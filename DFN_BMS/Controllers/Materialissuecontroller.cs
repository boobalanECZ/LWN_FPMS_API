using System;
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
    public class MaterialIssueController : ControllerBase
    {
        private readonly AppDbContext _context;

        public MaterialIssueController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/MaterialIssue
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _context.MaterialIssues
                .Include(x => x.Item)
                .OrderByDescending(x => x.Id)
                .Select(x => new
                {
                    x.Id,
                    x.IssueNumber,
                    x.ItemId,
                    PartNumber = x.Item.ItemNumber,
                    PartName = x.Item.ItemName,
                    x.Quantity,
                    x.IssuedTo,
                    x.IssuedBy,
                    x.StoreLocation,
                    x.PalletNo,
                    x.GrnNumber,
                    x.Remarks,
                    x.IssueDate
                })
                .ToListAsync();

            return Ok(list);
        }

        private async Task<string> GenerateIssueNumberAsync()
        {
            var year = DateTime.Now.Year;
            var prefix = $"MI-{year}-";

            var last = await _context.MaterialIssues
                .Where(x => x.IssueNumber.StartsWith(prefix))
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync();

            int nextSeq = 1;

            if (last != null)
            {
                var numericPart = last.IssueNumber.Substring(prefix.Length);
                if (int.TryParse(numericPart, out int lastSeq))
                    nextSeq = lastSeq + 1;
            }

            return $"{prefix}{nextSeq:D4}";
        }

        // POST: api/MaterialIssue
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] MaterialIssue model)
        {
            try
            {
                if (model.ItemId <= 0 ||
                    model.Quantity <= 0 ||
                    string.IsNullOrWhiteSpace(model.IssuedTo) ||
                    string.IsNullOrWhiteSpace(model.IssuedBy))
                {
                    return BadRequest(new { message = "Part Number, Quantity, Issued To and Issued By are required" });
                }

                var itemExists = await _context.ItemMasters.AnyAsync(x => x.Id == model.ItemId);
                if (!itemExists)
                    return BadRequest(new { message = "Selected Part Number does not exist" });

                // SERVER-SIDE DUPLICATE GUARD: if this exact pallet has
                // already been issued, reject it — this is the real,
                // authoritative check (client-side checks can be bypassed
                // by a stale cache, another device, a page refresh before
                // sync, etc).
                if (!string.IsNullOrWhiteSpace(model.PalletNo))
                {
                    var alreadyIssued = await _context.MaterialIssues
                        .AnyAsync(x => x.PalletNo == model.PalletNo);

                    if (alreadyIssued)
                        return BadRequest(new { message = $"Pallet {model.PalletNo} has already been issued." });
                }

                var entity = new MaterialIssue
                {
                    IssueNumber = await GenerateIssueNumberAsync(),
                    ItemId = model.ItemId,
                    Quantity = model.Quantity,
                    IssuedTo = model.IssuedTo.Trim(),
                    IssuedBy = model.IssuedBy.Trim(),
                    StoreLocation = model.StoreLocation?.Trim(),
                    // FIX: these two were being silently dropped before —
                    // the entity never picked up the pallet/GRN the
                    // frontend actually sent, so every saved row ended up
                    // with PalletNo = NULL, GrnNumber = NULL regardless of
                    // what was scanned.
                    PalletNo = model.PalletNo?.Trim(),
                    GrnNumber = model.GrnNumber?.Trim(),
                    Remarks = model.Remarks?.Trim(),
                    IssueDate = DateTime.Now,
                    CreatedDate = DateTime.Now
                };

                _context.MaterialIssues.Add(entity);
                await _context.SaveChangesAsync();

                return Ok(new { entity.Id, entity.IssueNumber });
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { message = $"Save failed: {detail}" });
            }
        }

        // DELETE: api/MaterialIssue/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _context.MaterialIssues.FindAsync(id);

            if (entity == null)
                return NotFound(new { message = "Material Issue record not found" });

            _context.MaterialIssues.Remove(entity);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Deleted Successfully" });
        }
    }
}