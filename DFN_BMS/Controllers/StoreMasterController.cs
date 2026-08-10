using System;
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
    public class StoreMasterController : ControllerBase
    {
        private readonly AppDbContext _context;

        private static readonly Regex ColourRegex = new Regex(@"^#[0-9A-Fa-f]{6}$");

        public StoreMasterController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/StoreMaster
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _context.StoreMasters
                .Include(x => x.PalletType)
                .OrderByDescending(x => x.Id)
                .Select(x => new
                {
                    x.Id,
                    x.StoreLocation,
                    x.PalletTypeId,
                    PalletTypeName = x.PalletType.PalletName,
                    x.PalletNumber,
                    x.ColourCode
                })
                .ToListAsync();

            return Ok(list);
        }

        // GET: api/StoreMaster/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _context.StoreMasters.FindAsync(id);

            if (item == null)
                return NotFound(new { message = "Store record not found" });

            return Ok(item);
        }

        // Generates the next pallet number for a series, wrapping back to
        // RangeFrom once RangeTo is passed — e.g. IN-01 .. IN-30, then
        // IN-01 again. Updates CurrentSequence on the type so the next
        // call continues from here.
        private async Task<string> GenerateNextPalletNumberAsync(PalletTypeMaster type)
        {
            int next = type.CurrentSequence + 1;
            if (next > type.RangeTo)
                next = type.RangeFrom;

            type.CurrentSequence = next;
            await _context.SaveChangesAsync();

            return $"{type.PalletName}-{next:D2}";
        }

        // POST: api/StoreMaster
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] StoreMaster model)
        {
            if (string.IsNullOrWhiteSpace(model.StoreLocation) ||
                model.PalletTypeId <= 0 ||
                string.IsNullOrWhiteSpace(model.ColourCode))
            {
                return BadRequest(new { message = "Please fill all required fields" });
            }

            var colourCode = model.ColourCode.Trim();

            if (!ColourRegex.IsMatch(colourCode))
                return BadRequest(new { message = "Colour must be a valid hex code (e.g. #1E88E5)" });

            var palletType = await _context.PalletTypeMasters.FindAsync(model.PalletTypeId);
            if (palletType == null)
                return BadRequest(new { message = "Selected Pallet Type does not exist" });

            var entity = new StoreMaster
            {
                StoreLocation = model.StoreLocation.Trim(),
                PalletTypeId = model.PalletTypeId,
                PalletNumber = await GenerateNextPalletNumberAsync(palletType),
                ColourCode = colourCode.ToUpper(),
                CreatedDate = DateTime.Now
            };

            _context.StoreMasters.Add(entity);
            await _context.SaveChangesAsync();

            return Ok(entity);
        }

        // PUT: api/StoreMaster/5
        // Note: changing the Pallet Type on an existing row does NOT
        // regenerate PalletNumber — that's assigned once, at creation.
        // Editing here only updates Store Location and Colour.
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] StoreMaster model)
        {
            var entity = await _context.StoreMasters.FindAsync(id);

            if (entity == null)
                return NotFound(new { message = "Store record not found" });

            if (string.IsNullOrWhiteSpace(model.StoreLocation) ||
                string.IsNullOrWhiteSpace(model.ColourCode))
            {
                return BadRequest(new { message = "Please fill all required fields" });
            }

            var colourCode = model.ColourCode.Trim();

            if (!ColourRegex.IsMatch(colourCode))
                return BadRequest(new { message = "Colour must be a valid hex code (e.g. #1E88E5)" });

            entity.StoreLocation = model.StoreLocation.Trim();
            entity.ColourCode = colourCode.ToUpper();
            entity.ModifiedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(entity);
        }

        // DELETE: api/StoreMaster/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _context.StoreMasters.FindAsync(id);

            if (entity == null)
                return NotFound(new { message = "Store record not found" });

            _context.StoreMasters.Remove(entity);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Deleted Successfully" });
        }
    }
}