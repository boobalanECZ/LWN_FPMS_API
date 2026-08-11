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
    public class ReportsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ReportsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Reports/grn?fromDate=2026-01-01&toDate=2026-01-31
        // Filters by PO Date. Both dates optional — omit either (or both)
        // to get an open-ended range / everything.
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

                // Flatten to one row per GRN line — the natural shape for
                // an Excel export (Part, Qty, Rate, etc. per row).
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
    }
}