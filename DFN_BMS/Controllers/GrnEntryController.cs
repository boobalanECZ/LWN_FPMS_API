using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DFN_BMS.DB;
using DFN_BMS.Models;

namespace DFN_BMS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GrnEntryController : ControllerBase
    {
        private readonly AppDbContext _context;

        private static readonly string[] ValidGrnTypes = { "Regular", "Sample" };
        private static readonly System.Text.RegularExpressions.Regex InvoiceNumberRegex =
            new System.Text.RegularExpressions.Regex(@"^[0-9]+$");

        public GrnEntryController(AppDbContext context)
        {
            _context = context;
        }

        public class GrnLineRequest
        {
            public int ItemId { get; set; }
            public string Uom { get; set; }
            public decimal? PalletQuantity { get; set; }
            public decimal Rate { get; set; }
            public decimal Quantity { get; set; }
        }

        public class GrnCreateRequest
        {
            public int SupplierId { get; set; }
            public string PoNumber { get; set; }
            public DateTime PoDate { get; set; }
            public string GrnType { get; set; }
            public string SupplierInvoiceNumber { get; set; }
            public DateTime SupplierInvoiceDate { get; set; }
            public string? GrnNo { get; set; }   // only used the first time a FY's counter is seeded
            public List<GrnLineRequest> Lines { get; set; } = new List<GrnLineRequest>();
        }

        // GET: api/GrnEntry?posted=false
        // posted omitted -> everything. posted=false -> "Show All GRN Post"
        // tab (awaiting posting). posted=true -> "GRN Reprint" tab.
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool? posted)
        {
            var query = _context.GrnHeaders
                .Include(x => x.Supplier)
                .Include(x => x.Lines)
                .AsQueryable();

            if (posted.HasValue)
                query = query.Where(x => x.IsPosted == posted.Value);

            var list = await query
                .OrderByDescending(x => x.Id)
                .Select(x => new
                {
                    x.Id,
                    x.GrnNumber,
                    SupplierName = x.Supplier.SupplierName,
                    x.SupplierInvoiceNumber,
                    x.SupplierInvoiceDate,
                    x.PoNumber,
                    x.PoDate,
                    x.GrnType,
                    x.IsPosted,
                    x.PostedDate,
                    x.PalletNo,
                    x.FifoPalletNo,
                    LineCount = x.Lines.Count,
                    TotalQuantity = x.Lines.Sum(l => l.Quantity),
                    TotalValue = x.Lines.Sum(l => l.TotalValue)
                })
                .ToListAsync();

            return Ok(list);
        }

        // GET: api/GrnEntry/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var header = await _context.GrnHeaders
                .Include(x => x.Supplier)
                .Include(x => x.Lines)
                    .ThenInclude(l => l.Item)
                .Where(x => x.Id == id)
                .Select(x => new
                {
                    x.Id,
                    x.GrnNumber,
                    x.SupplierId,
                    SupplierName = x.Supplier.SupplierName,
                    x.PoNumber,
                    x.PoDate,
                    x.GrnType,
                    x.SupplierInvoiceNumber,
                    x.SupplierInvoiceDate,
                    x.IsPosted,
                    x.PostedDate,
                    x.PalletNo,
                    x.FifoPalletNo,
                    Lines = x.Lines.Select(l => new
                    {
                        l.Id,
                        l.ItemId,
                        PartNumber = l.Item.ItemNumber,
                        PartName = l.Item.ItemName,
                        l.Uom,
                        l.PalletQuantity,
                        l.Rate,
                        l.Quantity,
                        l.TotalValue
                    })
                })
                .FirstOrDefaultAsync();

            if (header == null)
                return NotFound(new { message = "GRN not found" });

            return Ok(header);
        }

        // GET: api/GrnEntry/next-number
        // Returns either the auto-continued next number (if a counter
        // already exists) or an empty string with isManual=true (meaning:
        // no GRN has ever been entered yet, so the frontend should let
        // the user type the starting number themselves — any format).
        [HttpGet("next-number")]
        public async Task<IActionResult> GetNextNumber()
        {
            var counter = await _context.GrnCounters.OrderByDescending(x => x.Id).FirstOrDefaultAsync();

            if (counter == null)
                return Ok(new { grnNumber = "", isManual = true });

            var nextSeq = counter.LastSequence + 1;
            var grnNumber = $"{counter.Prefix}{nextSeq.ToString().PadLeft(counter.PadWidth, '0')}";
            return Ok(new { grnNumber, isManual = false });
        }

        // Splits a manually-typed value like "260001" into a text
        // prefix ("26") and a trailing numeric part ("0001", width 4).
        // No format is enforced beyond "ends in at least one digit".
        private static (string prefix, int number, int padWidth) SplitPrefixAndNumber(string value)
        {
            var match = Regex.Match(value, @"^(.*?)(\d+)$");
            if (!match.Success)
                return (value, 0, 0);

            var digits = match.Groups[2].Value;
            return (match.Groups[1].Value, int.Parse(digits), digits.Length);
        }

        // Resolves the GRN number to actually use for this Create() call:
        //  - If a counter already exists, ignore whatever the client sent
        //    and auto-increment server-side (avoids races / accidental
        //    overrides once the sequence is live).
        //  - If no counter exists yet, this is the very first GRN ever —
        //    require the client's manually-typed GrnNo (any format, as
        //    long as it ends in digits) and seed the counter from it.
        private async Task<(string grnNumber, string error)> ResolveGrnNumberAsync(string clientGrnNo)
        {
            var counter = await _context.GrnCounters.OrderByDescending(x => x.Id).FirstOrDefaultAsync();

            if (counter != null)
            {
                var nextSeq = counter.LastSequence + 1;
                counter.LastSequence = nextSeq;
                await _context.SaveChangesAsync();
                return ($"{counter.Prefix}{nextSeq.ToString().PadLeft(counter.PadWidth, '0')}", null);
            }

            // First-ever GRN — must be typed manually.
            if (string.IsNullOrWhiteSpace(clientGrnNo))
                return (null, "Enter a starting GRN No (e.g. 260001) — this is the first GRN ever entered");

            var grnNo = clientGrnNo.Trim();
            var (prefix, number, padWidth) = SplitPrefixAndNumber(grnNo);

            if (padWidth == 0)
                return (null, "GRN No must end with at least one digit (e.g. 260001)");

            var newCounter = new GrnCounter { Prefix = prefix, PadWidth = padWidth, LastSequence = number };
            _context.GrnCounters.Add(newCounter);
            await _context.SaveChangesAsync();

            return (grnNo, null);
        }

        // POST: api/GrnEntry
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] GrnCreateRequest req)
        {
            try
            {
                if (req == null || req.SupplierId <= 0 ||
                    string.IsNullOrWhiteSpace(req.PoNumber) ||
                    req.PoDate == default ||
                    string.IsNullOrWhiteSpace(req.GrnType) ||
                    string.IsNullOrWhiteSpace(req.SupplierInvoiceNumber) ||
                    req.SupplierInvoiceDate == default)
                {
                    return BadRequest(new { message = "Please fill all required header fields" });
                }

                if (!ValidGrnTypes.Contains(req.GrnType))
                    return BadRequest(new { message = "GRN Type must be 'Regular' or 'Sample'" });

                if (!InvoiceNumberRegex.IsMatch(req.SupplierInvoiceNumber.Trim()))
                    return BadRequest(new { message = "Supplier Invoice Number must be numbers only" });

                var supplierExists = await _context.SupplierMasters.AnyAsync(x => x.Id == req.SupplierId);
                if (!supplierExists)
                    return BadRequest(new { message = "Selected Supplier does not exist" });

                if (req.Lines == null || req.Lines.Count == 0)
                    return BadRequest(new { message = "Add at least one part before saving" });

                foreach (var line in req.Lines)
                {
                    if (line.ItemId <= 0)
                        return BadRequest(new { message = "Each line needs a valid Part Number" });
                    if (line.Rate <= 0)
                        return BadRequest(new { message = "Rate must be greater than 0" });
                    if (line.Quantity <= 0)
                        return BadRequest(new { message = "Quantity must be greater than 0" });

                    var itemExists = await _context.ItemMasters.AnyAsync(x => x.Id == line.ItemId);
                    if (!itemExists)
                        return BadRequest(new { message = $"Part Number (Item Id {line.ItemId}) does not exist" });
                }

                var (grnNumber, grnNoError) = await ResolveGrnNumberAsync(req.GrnNo);
                if (grnNoError != null)
                    return BadRequest(new { message = grnNoError });

                var header = new GrnHeader
                {
                    GrnNumber = grnNumber,
                    SupplierId = req.SupplierId,
                    PoNumber = req.PoNumber.Trim(),
                    PoDate = req.PoDate,
                    GrnType = req.GrnType,
                    SupplierInvoiceNumber = req.SupplierInvoiceNumber.Trim(),
                    SupplierInvoiceDate = req.SupplierInvoiceDate,
                    CreatedDate = DateTime.Now
                };

                foreach (var line in req.Lines)
                {
                    header.Lines.Add(new GrnLine
                    {
                        ItemId = line.ItemId,
                        Uom = line.Uom?.Trim(),
                        PalletQuantity = line.PalletQuantity,
                        Rate = line.Rate,
                        Quantity = line.Quantity,
                        TotalValue = Math.Round(line.Rate * (line.PalletQuantity ?? 0), 2),
                        CreatedDate = DateTime.Now
                    });
                }

                _context.GrnHeaders.Add(header);
                await _context.SaveChangesAsync();

                return Ok(new { header.Id, header.GrnNumber });
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { message = $"Save failed: {detail}" });
            }
        }

        // PUT: api/GrnEntry/5
        // Replaces header fields + lines wholesale (delete-and-recreate the
        // lines, simplest correct way to handle add/remove/edit in one call).
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] GrnCreateRequest req)
        {
            var header = await _context.GrnHeaders
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (header == null)
                return NotFound(new { message = "GRN not found" });

            if (header.IsPosted)
                return BadRequest(new { message = "A posted GRN cannot be edited" });

            if (req == null || req.SupplierId <= 0 ||
                string.IsNullOrWhiteSpace(req.PoNumber) ||
                req.PoDate == default ||
                string.IsNullOrWhiteSpace(req.GrnType) ||
                string.IsNullOrWhiteSpace(req.SupplierInvoiceNumber) ||
                req.SupplierInvoiceDate == default)
            {
                return BadRequest(new { message = "Please fill all required header fields" });
            }

            if (!ValidGrnTypes.Contains(req.GrnType))
                return BadRequest(new { message = "GRN Type must be 'Regular' or 'Sample'" });

            if (!InvoiceNumberRegex.IsMatch(req.SupplierInvoiceNumber.Trim()))
                return BadRequest(new { message = "Supplier Invoice Number must be numbers only" });

            if (req.Lines == null || req.Lines.Count == 0)
                return BadRequest(new { message = "Add at least one part before saving" });

            header.SupplierId = req.SupplierId;
            header.PoNumber = req.PoNumber.Trim();
            header.PoDate = req.PoDate;
            header.GrnType = req.GrnType;
            header.SupplierInvoiceNumber = req.SupplierInvoiceNumber.Trim();
            header.SupplierInvoiceDate = req.SupplierInvoiceDate;

            _context.GrnLines.RemoveRange(header.Lines);

            foreach (var line in req.Lines)
            {
                header.Lines.Add(new GrnLine
                {
                    ItemId = line.ItemId,
                    Uom = line.Uom?.Trim(),
                    PalletQuantity = line.PalletQuantity,
                    Rate = line.Rate,
                    Quantity = line.Quantity,
                    TotalValue = Math.Round(line.Rate * (line.PalletQuantity ?? 0), 2),
                    CreatedDate = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();

            return Ok(new { header.Id, header.GrnNumber });
        }

        // PUT: api/GrnEntry/5/post
        // Marks the GRN as posted and assigns Pallet No / FIFO Pallet No.
        [HttpPut("{id}/post")]
        public async Task<IActionResult> Post(int id)
        {
            var header = await _context.GrnHeaders.FindAsync(id);

            if (header == null)
                return NotFound(new { message = "GRN not found" });

            if (header.IsPosted)
                return BadRequest(new { message = "GRN is already posted" });

            header.IsPosted = true;
            header.PostedDate = DateTime.Now;
            header.PalletNo = await GenerateNextCodeAsync(x => x.PalletNo, "EX-", 2);
            header.FifoPalletNo = await GenerateFifoPalletNoAsync();

            await _context.SaveChangesAsync();

            return Ok(new { header.Id, header.PalletNo, header.FifoPalletNo });
        }

        private async Task<string> GenerateNextCodeAsync(
            System.Linq.Expressions.Expression<Func<GrnHeader, string?>> selector,
            string prefix,
            int padWidth)
        {
            var compiled = selector.Compile();

            var last = await _context.GrnHeaders
                .Where(x => x.PalletNo != null && x.PalletNo.StartsWith(prefix))
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync();

            int nextSeq = 1;

            if (last != null)
            {
                var value = compiled(last);
                if (value != null && int.TryParse(value.Substring(prefix.Length), out int lastSeq))
                    nextSeq = lastSeq + 1;
            }

            return $"{prefix}{nextSeq.ToString().PadLeft(padWidth, '0')}";
        }

        private async Task<string> GenerateFifoPalletNoAsync()
        {
            // e.g. F25070001 -> F + YY + MM + 4-digit sequence for that month
            var now = DateTime.Now;
            var prefix = $"F{now:yyMM}";

            var last = await _context.GrnHeaders
                .Where(x => x.FifoPalletNo != null && x.FifoPalletNo.StartsWith(prefix))
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync();

            int nextSeq = 1;

            if (last?.FifoPalletNo != null)
            {
                if (int.TryParse(last.FifoPalletNo.Substring(prefix.Length), out int lastSeq))
                    nextSeq = lastSeq + 1;
            }

            return $"{prefix}{nextSeq:D4}";
        }

        // DELETE: api/GrnEntry/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _context.GrnHeaders.FindAsync(id);

            if (entity == null)
                return NotFound(new { message = "GRN not found" });

            if (entity.IsPosted)
                return BadRequest(new { message = "A posted GRN cannot be deleted" });

            _context.GrnHeaders.Remove(entity);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Deleted Successfully" });
        }
    }
}