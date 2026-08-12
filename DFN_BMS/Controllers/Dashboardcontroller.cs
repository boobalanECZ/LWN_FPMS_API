using System;
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

        // GET: api/Dashboard/summary
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            try
            {
                var totalItems = await _context.ItemMasters.CountAsync();
                var totalSuppliers = await _context.SupplierMasters.CountAsync();
                var totalCustomers = await _context.CustomerMasters.CountAsync();
                var totalStores = await _context.StoreMasters.CountAsync();
                var totalGrns = await _context.GrnHeaders.CountAsync();
                var totalLines = await _context.GrnLines.CountAsync();
                var postedLines = await _context.GrnLines.CountAsync(l => l.IsPosted);
                var unpostedLines = totalLines - postedLines;
                var totalStockValue = await _context.GrnLines.SumAsync(l => (decimal?)l.TotalValue) ?? 0;
                var postedStockValue = await _context.GrnLines
                    .Where(l => l.IsPosted)
                    .SumAsync(l => (decimal?)l.TotalValue) ?? 0;

                var today = DateTime.Today;
                var last7Days = today.AddDays(-6);
                // GRN count per day for the last 7 days, for a simple trend chart.
                var recentHeaders = await _context.GrnHeaders
                    .Where(x => x.PoDate >= last7Days)
                    .Select(x => x.PoDate.Date)
                    .ToListAsync();

                var trend = Enumerable.Range(0, 7)
                    .Select(offset =>
                    {
                        var date = last7Days.AddDays(offset);
                        return new
                        {
                            Date = date.ToString("yyyy-MM-dd"),
                            Count = recentHeaders.Count(d => d == date)
                        };
                    })
                    .ToList();

                var recentGrns = await _context.GrnHeaders
                    .Include(x => x.Supplier)
                    .Include(x => x.Lines)
                    .OrderByDescending(x => x.Id)
                    .Take(6)
                    .Select(x => new
                    {
                        x.GrnNumber,
                        SupplierName = x.Supplier.SupplierName,
                        x.PoDate,
                        LineCount = x.Lines.Count,
                        TotalValue = x.Lines.Sum(l => l.TotalValue),
                        PostedLineCount = x.Lines.Count(l => l.IsPosted)
                    })
                    .ToListAsync();

                return Ok(new
                {
                    totalItems,
                    totalSuppliers,
                    totalCustomers,
                    totalStores,
                    totalGrns,
                    totalLines,
                    postedLines,
                    unpostedLines,
                    totalStockValue,
                    postedStockValue,
                    trend,
                    recentGrns
                });
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { message = $"Failed to load dashboard: {detail}" });
            }
        }
    }
}