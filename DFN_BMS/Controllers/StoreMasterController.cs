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
    public class StoreMasterController : ControllerBase
    {
        private readonly AppDbContext _context;

        public StoreMasterController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/StoreMaster/parts-list
        // Feeds the new Part Number dropdown on the Store Master form.
        [HttpGet("parts-list")]
        public async Task<IActionResult> GetPartsList()
        {
            var raw = await _context.ItemMasters
                .Select(x => new { x.Id, x.ItemNumber, x.ItemName })
                .ToListAsync();

            var data = raw
                .Select(x => new
                {
                    value = x.Id,
                    label = $"{x.ItemNumber} - {x.ItemName}"
                })
                .OrderBy(x => x.label)
                .ToList();

            return Ok(data);
        }

        // GET: api/StoreMaster/pallet-types
        [HttpGet("pallet-types")]
        public async Task<IActionResult> GetPalletTypes()
        {
            var data = await _context.PalletTypeMasters
                .Select(x => new
                {
                    value = x.Id,
                    label = x.PalletName
                })
                .OrderBy(x => x.label)
                .ToListAsync();

            return Ok(data);
        }

        // GET: api/StoreMaster
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _context.StoreMasters
                .Include(x => x.PalletType)
                .Include(x => x.PartNumber)
                .OrderByDescending(x => x.Id)
                .Select(x => new
                {
                    x.Id,
                    x.StoreLocation,
                    x.PalletTypeId,
                    PalletTypeName = x.PalletType.PalletName,
                    x.PalletNumber,
                    x.ColourCode,
                    x.PartNumberId,
                    PartNumberCode = x.PartNumber != null ? x.PartNumber.ItemNumber : null,
                    PartName = x.PartNumber != null ? x.PartNumber.ItemName : null
                })
                .ToListAsync();

            return Ok(list);
        }

        // GET: api/StoreMaster/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _context.StoreMasters
                .Include(x => x.PalletType)
                .Include(x => x.PartNumber)
                .Where(x => x.Id == id)
                .Select(x => new
                {
                    x.Id,
                    x.StoreLocation,
                    x.PalletTypeId,
                    PalletTypeName = x.PalletType.PalletName,
                    x.PalletNumber,
                    x.ColourCode,
                    x.PartNumberId,
                    PartNumberCode = x.PartNumber != null ? x.PartNumber.ItemNumber : null,
                    PartName = x.PartNumber != null ? x.PartNumber.ItemName : null
                })
                .FirstOrDefaultAsync();

            if (item == null)
                return NotFound(new { message = "Store record not found" });

            return Ok(item);
        }

        private async Task<string> GeneratePalletNumberAsync(int palletTypeId)
        {
            var palletType = await _context.PalletTypeMasters.FindAsync(palletTypeId);
            if (palletType == null)
                return null;

            var nextSeq = palletType.CurrentSequence + 1;

            palletType.CurrentSequence = nextSeq;
            await _context.SaveChangesAsync();

            var prefix = palletType.PalletName.Length >= 2
                ? palletType.PalletName.Substring(0, 2).ToUpper()
                : palletType.PalletName.ToUpper();

            return $"{prefix}-{nextSeq:D2}";
        }

        // POST: api/StoreMaster
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] StoreMaster model)
        {
            if (string.IsNullOrWhiteSpace(model.StoreLocation) ||
                model.PalletTypeId <= 0 ||
                string.IsNullOrWhiteSpace(model.ColourCode))
            {
                return BadRequest(new { message = "Store Location, Pallet Type and Colour are required" });
            }

            var typeExists = await _context.PalletTypeMasters.AnyAsync(x => x.Id == model.PalletTypeId);
            if (!typeExists)
                return BadRequest(new { message = "Selected Pallet Type does not exist" });

            if (model.PartNumberId.HasValue)
            {
                var partExists = await _context.ItemMasters.AnyAsync(x => x.Id == model.PartNumberId.Value);
                if (!partExists)
                    return BadRequest(new { message = "Selected Part Number does not exist" });
            }

            var palletNumber = await GeneratePalletNumberAsync(model.PalletTypeId);
            if (palletNumber == null)
                return BadRequest(new { message = "Failed to generate Pallet Number for the selected Pallet Type" });

            var entity = new StoreMaster
            {
                StoreLocation = model.StoreLocation.Trim(),
                PalletTypeId = model.PalletTypeId,
                PalletNumber = palletNumber,
                ColourCode = model.ColourCode.Trim(),
                PartNumberId = model.PartNumberId,
                CreatedDate = DateTime.Now
            };

            _context.StoreMasters.Add(entity);
            await _context.SaveChangesAsync();

            return Ok(entity);
        }

        // PUT: api/StoreMaster/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] StoreMaster model)
        {
            var entity = await _context.StoreMasters.FindAsync(id);

            if (entity == null)
                return NotFound(new { message = "Store record not found" });

            if (string.IsNullOrWhiteSpace(model.StoreLocation) ||
                model.PalletTypeId <= 0 ||
                string.IsNullOrWhiteSpace(model.ColourCode))
            {
                return BadRequest(new { message = "Store Location, Pallet Type and Colour are required" });
            }

            if (model.PartNumberId.HasValue)
            {
                var partExists = await _context.ItemMasters.AnyAsync(x => x.Id == model.PartNumberId.Value);
                if (!partExists)
                    return BadRequest(new { message = "Selected Part Number does not exist" });
            }

            entity.StoreLocation = model.StoreLocation.Trim();
            entity.PalletTypeId = model.PalletTypeId;
            entity.ColourCode = model.ColourCode.Trim();
            entity.PartNumberId = model.PartNumberId;
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