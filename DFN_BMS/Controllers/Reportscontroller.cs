using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DFN_BMS.DB;
using DFN_BMS.Models;

namespace DFN_BMS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ReportsController(AppDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // FULL REPORT
        //
        // GET:
        // /api/Reports/full
        //
        // Optional filters:
        // fromDate
        // toDate
        // itemId
        // itemGroupId
        // supplierId
        //
        // Example:
        // /api/Reports/full?fromDate=2026-08-01&toDate=2026-08-18
        //
        // /api/Reports/full?itemId=1
        //
        // /api/Reports/full?itemGroupId=2
        //
        // /api/Reports/full?supplierId=3
        //
        // Multiple:
        // /api/Reports/full?fromDate=2026-08-01
        // &toDate=2026-08-18
        // &itemId=1
        // &itemGroupId=2
        // &supplierId=3
        // ============================================================

        [HttpGet("full")]
        public async Task<IActionResult> GetFullReport(
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] int? itemId,
            [FromQuery] int? itemGroupId,
            [FromQuery] int? supplierId)
        {
            try
            {
                // ====================================================
                // VALIDATE DATE
                // ====================================================

                if (fromDate.HasValue &&
                    toDate.HasValue &&
                    fromDate.Value.Date > toDate.Value.Date)
                {
                    return BadRequest(new
                    {
                        message = "From Date cannot be after To Date"
                    });
                }


                // ====================================================
                // BASE QUERY
                // ====================================================

                var headerQuery = _context.GrnHeaders
                    .Include(x => x.Supplier)
                    .Include(x => x.Lines)
                        .ThenInclude(l => l.Item)
                    .AsQueryable();


                // ====================================================
                // DATE FILTER
                // ====================================================

                if (fromDate.HasValue)
                {
                    headerQuery = headerQuery.Where(
                        x => x.PoDate >= fromDate.Value.Date
                    );
                }

                if (toDate.HasValue)
                {
                    headerQuery = headerQuery.Where(
                        x => x.PoDate <=
                             toDate.Value.Date
                                 .AddDays(1)
                                 .AddTicks(-1)
                    );
                }


                // ====================================================
                // ITEM FILTER
                // ====================================================

                if (itemId.HasValue)
                {
                    headerQuery = headerQuery.Where(
                        x => x.Lines.Any(
                            l => l.Item != null &&
                                 l.Item.Id == itemId.Value
                        )
                    );
                }


                // ====================================================
                // ITEM GROUP FILTER
                // ====================================================
                //
                // Assumes:
                // Item.ItemGroupId
                //
                // If your Item model uses another property name,
                // change l.Item.ItemGroupId here.
                // ====================================================

                if (itemGroupId.HasValue)
                {
                    headerQuery = headerQuery.Where(
                        x => x.Lines.Any(
                            l => l.Item != null &&
                                 l.Item.ItemGroupId == itemGroupId.Value
                        )
                    );
                }


                // ====================================================
                // SUPPLIER FILTER
                // ====================================================

                if (supplierId.HasValue)
                {
                    headerQuery = headerQuery.Where(
                        x => x.Supplier != null &&
                             x.Supplier.Id == supplierId.Value
                    );
                }


                // ====================================================
                // GET GRN HEADERS
                // ====================================================

                var headers = await headerQuery
                    .OrderBy(x => x.PoDate)
                    .ToListAsync();


                // ====================================================
                // GET GRN LINE IDS
                // ====================================================

                var lineIds = headers
                    .SelectMany(h => h.Lines)
                    .Select(l => l.Id)
                    .ToList();


                // ====================================================
                // GET STORE MOVEMENT / GRN PALLETS
                // ====================================================

                var pallets = await _context.GrnPallets
                    .Where(p => lineIds.Contains(p.GrnLineId))
                    .ToListAsync();


                var palletsByLineId = pallets
                    .GroupBy(p => p.GrnLineId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.First()
                    );


                var palletIds = pallets
                    .Select(p => p.Id)
                    .ToList();


                // ====================================================
                // GET STORE MOVEMENTS
                // ====================================================

                var movements = await _context.StoreMovements

                    .Include(m => m.StorePosition)
                        .ThenInclude(sp => sp.Store)

                    .Include(m => m.RackRow)
                        .ThenInclude(r => r.Column)
                            .ThenInclude(c => c.Rack)
                                .ThenInclude(rk => rk.Store)
                                    .ThenInclude(lm => lm.StoreMaster)

                    .Where(
                        m =>
                            m.GrnPalletId != null &&
                            palletIds.Contains(m.GrnPalletId.Value)
                    )

                    .ToListAsync();


                // ====================================================
                // EARLIEST STORE MOVEMENT FOR EACH PALLET
                // ====================================================

                var earliestMovementByPalletId = movements
                    .GroupBy(m => m.GrnPalletId!.Value)
                    .ToDictionary(
                        g => g.Key,
                        g => g.OrderBy(m => m.MovementDate).First()
                    );


                // ====================================================
                // GET MATERIAL ISSUES
                // ====================================================

                var storePalletNos = pallets
                    .Select(p => p.PalletNo)
                    .Where(p => !string.IsNullOrEmpty(p))
                    .ToList();


                var issues = await _context.MaterialIssues
                    .Where(
                        mi =>
                            mi.PalletNo != null &&
                            storePalletNos.Contains(mi.PalletNo)
                    )
                    .ToListAsync();


                // ====================================================
                // LATEST MATERIAL ISSUE FOR EACH PALLET
                // ====================================================

                var latestIssueByPalletNo = issues
                    .GroupBy(i => i.PalletNo)
                    .ToDictionary(
                        g => g.Key,
                        g => g.OrderByDescending(i => i.IssueDate).First()
                    );


                // ====================================================
                // BUILD FINAL REPORT
                // ====================================================

                var rows = new List<object>();


                foreach (var h in headers)
                {
                    var lines = h.Lines.Any()
                        ? h.Lines.Cast<GrnLine>().ToList()
                        : new List<GrnLine> { null };


                    foreach (var l in lines)
                    {
                        // ============================================
                        // GET PALLET
                        // ============================================

                        var pallet =
                            l != null &&
                            palletsByLineId.ContainsKey(l.Id)
                                ? palletsByLineId[l.Id]
                                : null;


                        // ============================================
                        // GET STORE MOVEMENT
                        // ============================================

                        var movement =
                            pallet != null &&
                            earliestMovementByPalletId.ContainsKey(pallet.Id)
                                ? earliestMovementByPalletId[pallet.Id]
                                : null;


                        // ============================================
                        // GET MATERIAL ISSUE
                        // ============================================

                        var issue =
                            pallet?.PalletNo != null &&
                            latestIssueByPalletNo.ContainsKey(pallet.PalletNo)
                                ? latestIssueByPalletNo[pallet.PalletNo]
                                : null;


                        // ============================================
                        // STORE LOCATION
                        // ============================================

                        var storeLocation =
                            movement?.StorePosition?.Store?.StoreLocation
                            ??
                            movement?.RackRow?.Column?.Rack?.Store?
                                .StoreMaster?.StoreLocation;


                        // ============================================
                        // STATUS
                        // ============================================

                        var status =
                            issue != null
                                ? "Issued"
                                : movement != null
                                    ? "In Store"
                                    : (l != null && l.IsPosted)
                                        ? "Posted"
                                        : "Not Posted";


                        // ============================================
                        // ADD REPORT ROW
                        // ============================================

                        rows.Add(new
                        {
                            h.GrnNumber,

                            SupplierName =
                                h.Supplier != null
                                    ? h.Supplier.SupplierName
                                    : null,

                            h.PoNumber,

                            h.PoDate,

                            h.GrnType,

                            h.SupplierInvoiceNumber,

                            h.SupplierInvoiceDate,

                            PartNumber =
                                l?.Item?.ItemNumber,

                            PartName =
                                l?.Item?.ItemName,

                            Quantity =
                                l?.Quantity,

                            PalletQuantity =
                                l?.PalletQuantity,

                            Rate =
                                l?.Rate,

                            TotalValue =
                                l?.TotalValue,

                            LabelPalletNo =
                                l?.PalletNo,

                            FifoPalletNo =
                                l?.FifoPalletNo,

                            StorePalletNo =
                                pallet?.PalletNo,

                            StoreLocation =
                                storeLocation,

                            MovementDate =
                                movement?.MovementDate,

                            IssuedTo =
                                issue?.IssuedTo,

                            IssuedBy =
                                issue?.IssuedBy,

                            IssueDate =
                                issue?.IssueDate,

                            Status =
                                status
                        });
                    }
                }


                // ====================================================
                // RETURN RESULT
                // ====================================================

                return Ok(rows);
            }
            catch (Exception ex)
            {
                var detail =
                    ex.InnerException?.Message ??
                    ex.Message;

                return StatusCode(
                    500,
                    new
                    {
                        message =
                            $"Failed to load report: {detail}"
                    }
                );
            }
        }


        // ============================================================
        // GRN REPORT
        // ============================================================

        [HttpGet("grn")]
        public async Task<IActionResult> GetGrnReport(
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate)
        {
            try
            {
                if (fromDate.HasValue &&
                    toDate.HasValue &&
                    fromDate.Value.Date > toDate.Value.Date)
                {
                    return BadRequest(new
                    {
                        message = "From Date cannot be after To Date"
                    });
                }


                var query = _context.GrnHeaders

                    .Include(x => x.Supplier)

                    .Include(x => x.Lines)
                        .ThenInclude(l => l.Item)

                    .AsQueryable();


                if (fromDate.HasValue)
                {
                    query = query.Where(
                        x => x.PoDate >= fromDate.Value.Date
                    );
                }


                if (toDate.HasValue)
                {
                    query = query.Where(
                        x =>
                            x.PoDate <=
                            toDate.Value.Date
                                .AddDays(1)
                                .AddTicks(-1)
                    );
                }


                var headers = await query
                    .OrderBy(x => x.PoDate)
                    .ToListAsync();


                var rows = headers

                    .SelectMany(
                        h => h.Lines.DefaultIfEmpty(),
                        (h, l) => new
                        {
                            h.GrnNumber,

                            SupplierName =
                                h.Supplier != null
                                    ? h.Supplier.SupplierName
                                    : null,

                            h.PoNumber,

                            h.PoDate,

                            h.GrnType,

                            h.SupplierInvoiceNumber,

                            h.SupplierInvoiceDate,

                            PartNumber =
                                l != null
                                    ? l.Item.ItemNumber
                                    : null,

                            PartName =
                                l != null
                                    ? l.Item.ItemName
                                    : null,

                            Quantity =
                                l != null
                                    ? l.Quantity
                                    : (decimal?)null,

                            PalletQuantity =
                                l != null
                                    ? l.PalletQuantity
                                    : null,

                            Rate =
                                l != null
                                    ? l.Rate
                                    : (decimal?)null,

                            TotalValue =
                                l != null
                                    ? l.TotalValue
                                    : (decimal?)null,

                            IsPosted =
                                l != null &&
                                l.IsPosted,

                            PalletNo =
                                l != null
                                    ? l.PalletNo
                                    : null,

                            FifoPalletNo =
                                l != null
                                    ? l.FifoPalletNo
                                    : null
                        }
                    )
                    .ToList();


                return Ok(rows);
            }
            catch (Exception ex)
            {
                var detail =
                    ex.InnerException?.Message ??
                    ex.Message;

                return StatusCode(
                    500,
                    new
                    {
                        message =
                            $"Failed to load GRN report: {detail}"
                    }
                );
            }
        }


        // ============================================================
        // MATERIAL ISSUE REPORT
        // ============================================================

        [HttpGet("material-issue")]
        public async Task<IActionResult> GetMaterialIssueReport(
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate)
        {
            try
            {
                if (fromDate.HasValue &&
                    toDate.HasValue &&
                    fromDate.Value.Date > toDate.Value.Date)
                {
                    return BadRequest(new
                    {
                        message = "From Date cannot be after To Date"
                    });
                }


                var query = _context.MaterialIssues

                    .Include(x => x.Item)

                    .AsQueryable();


                if (fromDate.HasValue)
                {
                    query = query.Where(
                        x => x.IssueDate >= fromDate.Value.Date
                    );
                }


                if (toDate.HasValue)
                {
                    query = query.Where(
                        x =>
                            x.IssueDate <=
                            toDate.Value.Date
                                .AddDays(1)
                                .AddTicks(-1)
                    );
                }


                var rows = await query

                    .OrderBy(x => x.IssueDate)

                    .Select(x => new
                    {
                        x.IssueNumber,

                        PartNumber =
                            x.Item != null
                                ? x.Item.ItemNumber
                                : null,

                        PartName =
                            x.Item != null
                                ? x.Item.ItemName
                                : null,

                        x.Quantity,

                        x.IssuedTo,

                        x.IssuedBy,

                        x.StoreLocation,

                        x.PalletNo,

                        x.GrnNumber,

                        x.Remarks,

                        x.IssueDate,

                        x.CreatedDate
                    })

                    .ToListAsync();


                return Ok(rows);
            }
            catch (Exception ex)
            {
                var detail =
                    ex.InnerException?.Message ??
                    ex.Message;

                return StatusCode(
                    500,
                    new
                    {
                        message =
                            $"Failed to load material issue report: {detail}"
                    }
                );
            }
        }
    }
}