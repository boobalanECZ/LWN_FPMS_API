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

        // GET: api/Reports/full?fromDate=2026-01-01&toDate=2026-01-31
        // ★ NEW: single merged report — no tabs. One row per GRN line,
        // enriched left-to-right across the whole pallet lifecycle:
        //   GRN Entry/Post  ->  Store Movement (where it was stuffed)
        //   ->  Material Issue (who/when it was issued out, if at all)
        //
        // Filters by PO Date (same open-ended-range behaviour as before).
        // Note: GrnLine.PalletNo/FifoPalletNo (the "EX-xx" / FIFO label
        // pallet) is a different identifier from GrnPallet.PalletNo (the
        // "Pxxx" pallet Store Movement actually stuffs and Material Issue
        // matches against) — both are surfaced as separate columns so
        // nothing is silently conflated.
        [HttpGet("full")]
        public async Task<IActionResult> GetFullReport([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
        {
            try
            {
                var headerQuery = _context.GrnHeaders
                    .Include(x => x.Supplier)
                    .Include(x => x.Lines)
                        .ThenInclude(l => l.Item)
                    .AsQueryable();

                if (fromDate.HasValue)
                    headerQuery = headerQuery.Where(x => x.PoDate >= fromDate.Value.Date);
                if (toDate.HasValue)
                    headerQuery = headerQuery.Where(x => x.PoDate <= toDate.Value.Date.AddDays(1).AddTicks(-1));

                var headers = await headerQuery.OrderBy(x => x.PoDate).ToListAsync();
                var lineIds = headers.SelectMany(h => h.Lines).Select(l => l.Id).ToList();

                // GrnLineId -> GrnPallet (the Store Movement pallet record,
                // separate from GrnLine.PalletNo/FifoPalletNo above).
                var pallets = await _context.GrnPallets
                    .Where(p => lineIds.Contains(p.GrnLineId))
                    .ToListAsync();
                var palletsByLineId = pallets.ToDictionary(p => p.GrnLineId, p => p);
                var palletIds = pallets.Select(p => p.Id).ToList();

                // Earliest Store Movement per pallet = where/when it was
                // first stuffed into a store position or rack slot.
                var movements = await _context.StoreMovements
                    .Include(m => m.StorePosition)
                        .ThenInclude(sp => sp.Store)
                    .Include(m => m.RackRow)
                        .ThenInclude(r => r.Column)
                            .ThenInclude(c => c.Rack)
                                .ThenInclude(rk => rk.Store)
                                    .ThenInclude(lm => lm.StoreMaster)
                    .Where(m => m.GrnPalletId != null && palletIds.Contains(m.GrnPalletId.Value))
                    .ToListAsync();
                var earliestMovementByPalletId = movements
                    .GroupBy(m => m.GrnPalletId.Value)
                    .ToDictionary(g => g.Key, g => g.OrderBy(m => m.MovementDate).First());

                // Material Issue is matched by GrnPallet.PalletNo ("Pxxx").
                var storePalletNos = pallets.Select(p => p.PalletNo).Where(p => p != null).ToList();
                var issues = await _context.MaterialIssues
                    .Where(mi => mi.PalletNo != null && storePalletNos.Contains(mi.PalletNo))
                    .ToListAsync();
                var latestIssueByPalletNo = issues
                    .GroupBy(i => i.PalletNo)
                    .ToDictionary(g => g.Key, g => g.OrderByDescending(i => i.IssueDate).First());

                var rows = new List<object>();

                foreach (var h in headers)
                {
                    var lines = h.Lines.Any() ? h.Lines.Cast<GrnLine>().ToList() : new List<GrnLine> { null };

                    foreach (var l in lines)
                    {
                        var pallet = l != null && palletsByLineId.ContainsKey(l.Id) ? palletsByLineId[l.Id] : null;
                        var movement = pallet != null && earliestMovementByPalletId.ContainsKey(pallet.Id)
                            ? earliestMovementByPalletId[pallet.Id]
                            : null;
                        var issue = pallet?.PalletNo != null && latestIssueByPalletNo.ContainsKey(pallet.PalletNo)
                            ? latestIssueByPalletNo[pallet.PalletNo]
                            : null;

                        var storeLocation = movement?.StorePosition?.Store?.StoreLocation
                            ?? movement?.RackRow?.Column?.Rack?.Store?.StoreMaster?.StoreLocation;

                        var status = issue != null ? "Issued"
                            : movement != null ? "In Store"
                            : (l != null && l.IsPosted) ? "Posted"
                            : "Not Posted";

                        rows.Add(new
                        {
                            h.GrnNumber,
                            SupplierName = h.Supplier != null ? h.Supplier.SupplierName : null,
                            h.PoNumber,
                            h.PoDate,
                            h.GrnType,
                            h.SupplierInvoiceNumber,
                            h.SupplierInvoiceDate,
                            PartNumber = l?.Item?.ItemNumber,
                            PartName = l?.Item?.ItemName,
                            Quantity = l?.Quantity,
                            PalletQuantity = l?.PalletQuantity,
                            Rate = l?.Rate,
                            TotalValue = l?.TotalValue,
                            LabelPalletNo = l?.PalletNo,
                            FifoPalletNo = l?.FifoPalletNo,
                            StorePalletNo = pallet?.PalletNo,
                            StoreLocation = storeLocation,
                            MovementDate = movement?.MovementDate,
                            IssuedTo = issue?.IssuedTo,
                            IssuedBy = issue?.IssuedBy,
                            IssueDate = issue?.IssueDate,
                            Status = status
                        });
                    }
                }

                return Ok(rows);
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { message = $"Failed to load report: {detail}" });
            }
        }

        // GET: api/Reports/grn?fromDate=2026-01-01&toDate=2026-01-31
        // Filters by PO Date. Both dates optional — omit either (or both)
        // to get an open-ended range / everything. Kept alongside /full
        // in case anything else still links directly to a GRN-only export.
        [HttpGet("grn")]
        public async Task<IActionResult> GetGrnReport([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
        {
            try
            {
                var query = _context.GrnHeaders
                    .Include(x => x.Supplier)
                    .Include(x => x.Lines)
                        .ThenInclude(l => l.Item)
                    .AsQueryable();

                if (fromDate.HasValue)
                    query = query.Where(x => x.PoDate >= fromDate.Value.Date);
                if (toDate.HasValue)
                    query = query.Where(x => x.PoDate <= toDate.Value.Date.AddDays(1).AddTicks(-1));

                var headers = await query
                    .OrderBy(x => x.PoDate)
                    .ToListAsync();

                var rows = headers
                    .SelectMany(h => h.Lines.DefaultIfEmpty(), (h, l) => new
                    {
                        h.GrnNumber,
                        SupplierName = h.Supplier != null ? h.Supplier.SupplierName : null,
                        h.PoNumber,
                        h.PoDate,
                        h.GrnType,
                        h.SupplierInvoiceNumber,
                        h.SupplierInvoiceDate,
                        PartNumber = l != null ? l.Item.ItemNumber : null,
                        PartName = l != null ? l.Item.ItemName : null,
                        Quantity = l != null ? l.Quantity : (decimal?)null,
                        PalletQuantity = l != null ? l.PalletQuantity : null,
                        Rate = l != null ? l.Rate : (decimal?)null,
                        TotalValue = l != null ? l.TotalValue : (decimal?)null,
                        IsPosted = l != null && l.IsPosted,
                        PalletNo = l != null ? l.PalletNo : null,
                        FifoPalletNo = l != null ? l.FifoPalletNo : null
                    })
                    .ToList();

                return Ok(rows);
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { message = $"Failed to load report: {detail}" });
            }
        }

        // GET: api/Reports/material-issue?fromDate=2026-01-01&toDate=2026-01-31
        // Filters by IssueDate. Kept alongside /full for the same reason
        // as /grn above.
        [HttpGet("material-issue")]
        public async Task<IActionResult> GetMaterialIssueReport([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
        {
            try
            {
                var query = _context.MaterialIssues
                    .Include(x => x.Item)
                    .AsQueryable();

                if (fromDate.HasValue)
                    query = query.Where(x => x.IssueDate >= fromDate.Value.Date);
                if (toDate.HasValue)
                    query = query.Where(x => x.IssueDate <= toDate.Value.Date.AddDays(1).AddTicks(-1));

                var rows = await query
                    .OrderBy(x => x.IssueDate)
                    .Select(x => new
                    {
                        x.IssueNumber,
                        PartNumber = x.Item != null ? x.Item.ItemNumber : null,
                        PartName = x.Item != null ? x.Item.ItemName : null,
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
                var detail = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { message = $"Failed to load material issue report: {detail}" });
            }
        }
    }
}