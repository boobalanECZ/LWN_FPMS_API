using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DFN_BMS.DB;

namespace DFN_BMS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        // GET:
        // api/Dashboard/summary?fromDate=2026-08-01&toDate=2026-08-19
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary(
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate)
        {
            try
            {
                // =========================================================
                // DATE FILTER
                // =========================================================

                var today = DateTime.Today;

                // Default = current week (Sunday -> Saturday)
                var defaultFromDate =
                    today.AddDays(-(int)today.DayOfWeek);

                var filterFrom =
                    fromDate?.Date ?? defaultFromDate;

                var filterTo =
                    toDate?.Date ?? today;

                // Validate date range
                if (filterFrom > filterTo)
                {
                    return BadRequest(new
                    {
                        message = "From Date cannot be greater than To Date."
                    });
                }

                // Include the complete To Date.
                //
                // Example:
                // From = 2026-08-01
                // To   = 2026-08-19
                //
                // Includes:
                // 01-Aug 00:00:00
                // through
                // 19-Aug 23:59:59
                var filterToExclusive = filterTo.AddDays(1);


                // =========================================================
                // DATE FILTERED PALLETS
                // =========================================================
                //
                // TOTAL PALLETS on dashboard is based on selected date range.
                //
                // Example:
                // Select 01-Jul to 17-Jul
                // A pallet created on 19-Aug will NOT be counted.
                // =========================================================

                var filteredPallets =
                    await _context.GrnPallets
                        .Where(p =>
                            p.CreatedDate >= filterFrom &&
                            p.CreatedDate < filterToExclusive)
                        .ToListAsync();

                var totalPalletsCount =
                    filteredPallets.Count;


                // =========================================================
                // MATERIAL ISSUES
                // =========================================================

                var filteredIssues =
                    await _context.MaterialIssues
                        .Where(i =>
                            i.IssueDate >= filterFrom &&
                            i.IssueDate < filterToExclusive)
                        .ToListAsync();


                // =========================================================
                // STORE MOVEMENTS
                // =========================================================

                var filteredMovements =
                    await _context.StoreMovements
                        .Where(m =>
                            m.MovementDate >= filterFrom &&
                            m.MovementDate < filterToExclusive)
                        .ToListAsync();


                // =========================================================
                // ISSUED PALLETS
                // =========================================================

                var issuedPalletNos =
                    filteredIssues
                        .Where(i => i.PalletNo != null)
                        .Select(i => i.PalletNo)
                        .Distinct()
                        .ToHashSet();


                var issuedPalletsCount =
                    filteredPallets.Count(
                        p =>
                            p.PalletNo != null &&
                            issuedPalletNos.Contains(p.PalletNo)
                    );


                // =========================================================
                // CURRENT/FILTERED PALLET STATUS
                // =========================================================

                var stuffedPalletIds =
                    filteredMovements
                        .Where(m => m.GrnPalletId != null)
                        .Select(m => m.GrnPalletId.Value)
                        .ToHashSet();


                var closedPalletsCount =
                    filteredPallets.Count(
                        p => !stuffedPalletIds.Contains(p.Id)
                    );


                var availablePalletsCount =
                    Math.Max(
                        0,
                        totalPalletsCount
                        - issuedPalletsCount
                        - closedPalletsCount
                    );


                // =========================================================
                // PALLETS BY LOCATION
                // =========================================================
                //
                // Only movements inside selected date range are considered.
                //
                // IMPORTANT:
                // We use filteredPallets for the dashboard count.
                // =========================================================

                var movementsWithLocation =
                    await _context.StoreMovements

                        .Include(m => m.StorePosition)
                            .ThenInclude(sp => sp.Store)

                        .Include(m => m.RackRow)
                            .ThenInclude(r => r.Column)
                                .ThenInclude(c => c.Rack)
                                    .ThenInclude(rk => rk.Store)
                                        .ThenInclude(s => s.StoreMaster)

                        .Where(m =>
                            m.GrnPalletId != null &&
                            m.MovementDate >= filterFrom &&
                            m.MovementDate < filterToExclusive)

                        .ToListAsync();


                // Dictionary only for pallets inside selected date range.
                var palletById =
                    filteredPallets.ToDictionary(
                        p => p.Id,
                        p => p
                    );


                // =========================================================
                // FIRST MOVEMENT PER PALLET
                // =========================================================

                var earliestMovementByPalletId =
                    movementsWithLocation

                        .GroupBy(m => m.GrnPalletId!.Value)

                        .ToDictionary(
                            g => g.Key,
                            g => g
                                .OrderBy(m => m.MovementDate)
                                .First()
                        );


                // =========================================================
                // LOCATION COUNTS
                // =========================================================

                var locationCounts =
                    new Dictionary<string, int>(
                        StringComparer.OrdinalIgnoreCase
                    );


                foreach (var kv in earliestMovementByPalletId)
                {
                    // Pallet must belong to selected date range.
                    if (!palletById.TryGetValue(
                            kv.Key,
                            out var pallet))
                    {
                        continue;
                    }


                    // Do not show issued pallets as available.
                    if (
                        pallet.PalletNo != null &&
                        issuedPalletNos.Contains(pallet.PalletNo)
                    )
                    {
                        continue;
                    }


                    var movement = kv.Value;


                    var location =
                        movement.StorePosition?.Store?.StoreLocation
                        ??
                        movement.RackRow?.Column?.Rack?.Store?
                            .StoreMaster?.StoreLocation
                        ??
                        "Unassigned";


                    if (locationCounts.ContainsKey(location))
                    {
                        locationCounts[location]++;
                    }
                    else
                    {
                        locationCounts[location] = 1;
                    }
                }


                var locations =
                    locationCounts

                        .Select(kv => new
                        {
                            StoreLocation = kv.Key,
                            AvailableCount = kv.Value
                        })

                        .OrderByDescending(
                            x => x.AvailableCount
                        )

                        .ToList();


                // =========================================================
                // DATE FILTERED GRN HEADERS
                // =========================================================

                var filteredGrnHeaders =
                    await _context.GrnHeaders

                        .Where(x =>
                            x.CreatedDate >= filterFrom &&
                            x.CreatedDate < filterToExclusive)

                        .Include(x => x.Lines)

                        .ToListAsync();


                // =========================================================
                // TRANSACTION SUMMARY
                // =========================================================

                var grnEntriesFiltered =
                    filteredGrnHeaders.Count;


                var palletsReceivedFiltered =
                    filteredPallets.Count;


                var materialIssuesFiltered =
                    filteredIssues.Count;


                var palletsIssuedFiltered =
                    filteredIssues.Count;


                var storeVerificationsFiltered =
                    filteredMovements.Count;


                // =========================================================
                // RECENT GRN ACTIVITIES
                // =========================================================

                var recentGrnActivities =
                    filteredGrnHeaders

                        .OrderByDescending(
                            x => x.CreatedDate
                        )

                        .Take(10)

                        .Select(x => new
                        {
                            Type = "GRN Entry",

                            Date = x.CreatedDate,

                            RefNo = x.GrnNumber,

                            PalletNo = (string)null,

                            Location = (string)null,

                            Quantity =
                                x.Lines
                                    .Sum(l =>
                                        (decimal?)l.Quantity)
                                ?? 0,

                            CreatedBy = x.CreatedBy
                        })

                        .ToList();


                // =========================================================
                // RECENT MATERIAL ISSUE ACTIVITIES
                // =========================================================

                var recentIssueActivities =
                    filteredIssues

                        .OrderByDescending(
                            i => i.IssueDate
                        )

                        .Take(10)

                        .Select(i => new
                        {
                            Type = "Material Issue",

                            Date = i.IssueDate,

                            RefNo = i.IssueNumber,

                            PalletNo = i.PalletNo,

                            Location = i.StoreLocation,

                            Quantity = i.Quantity,

                            CreatedBy = i.IssuedBy
                        })

                        .ToList();


                // =========================================================
                // RECENT STORE MOVEMENT ACTIVITIES
                // =========================================================

                var recentMovementActivities =
                    movementsWithLocation

                        .OrderByDescending(
                            m => m.MovementDate
                        )

                        .Take(10)

                        .Select(m =>
                        {
                            var location =
                                m.StorePosition?.Store?
                                    .StoreLocation
                                ??
                                m.RackRow?.Column?.Rack?
                                    .Store?
                                    .StoreMaster?
                                    .StoreLocation
                                ??
                                "Unassigned";


                            palletById.TryGetValue(
                                m.GrnPalletId!.Value,
                                out var pallet
                            );


                            return new
                            {
                                Type = "Store Movement",

                                Date = m.MovementDate,

                                RefNo = (string)null,

                                PalletNo =
                                    pallet?.PalletNo,

                                Location = location,

                                Quantity = m.Quantity,

                                CreatedBy = m.CreatedBy
                            };
                        })

                        .ToList();


                // =========================================================
                // COMBINE RECENT ACTIVITIES
                // =========================================================

                var recentActivities =
                    recentGrnActivities

                        .Concat(recentIssueActivities)

                        .Concat(recentMovementActivities)

                        .OrderByDescending(
                            a => a.Date
                        )

                        .Take(20)

                        .ToList();


                // =========================================================
                // RESPONSE
                // =========================================================

                return Ok(new
                {
                    // =====================================================
                    // DATE FILTER
                    // =====================================================

                    filter = new
                    {
                        fromDate =
                            filterFrom.ToString("yyyy-MM-dd"),

                        toDate =
                            filterTo.ToString("yyyy-MM-dd")
                    },


                    // =====================================================
                    // PALLET SUMMARY
                    // =====================================================

                    totalPallets =
                        totalPalletsCount,

                    availablePallets =
                        availablePalletsCount,

                    issuedPallets =
                        issuedPalletsCount,


                    // =====================================================
                    // TOTAL ISSUES
                    // =====================================================

                    totalIssues =
                        materialIssuesFiltered,


                    // =====================================================
                    // PALLET STATUS
                    // =====================================================

                    palletStatus = new
                    {
                        available =
                            availablePalletsCount,

                        issued =
                            issuedPalletsCount,

                        closed =
                            closedPalletsCount
                    },


                    // =====================================================
                    // LOCATIONS
                    // =====================================================

                    locations,


                    // =====================================================
                    // TRANSACTION SUMMARY
                    // =====================================================

                    transactionSummary = new
                    {
                        grnEntries =
                            grnEntriesFiltered,

                        palletsReceived =
                            palletsReceivedFiltered,

                        materialIssues =
                            materialIssuesFiltered,

                        palletsIssued =
                            palletsIssuedFiltered,

                        storeVerifications =
                            storeVerificationsFiltered
                    },


                    // =====================================================
                    // RECENT ACTIVITIES
                    // =====================================================

                    recentActivities
                });
            }
            catch (Exception ex)
            {
                var detail =
                    ex.InnerException?.Message
                    ?? ex.Message;

                return StatusCode(
                    500,
                    new
                    {
                        message =
                            $"Failed to load dashboard: {detail}"
                    });
            }
        }
    }
}