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
    public class PriceMasterController : ControllerBase
    {
        private readonly AppDbContext _context;

        private static readonly string[] ValidTypes = { "Customer", "Supplier" };

        public PriceMasterController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/PriceMaster
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _context.PriceMasters
                .Include(x => x.PartNumber)
                .OrderByDescending(x => x.Id)
                .Select(x => new
                {
                    x.Id,
                    x.PartNumberId,
                    PartNumberText = x.PartNumber.ItemNumber,
                    ItemName = x.PartNumber.ItemName,
                    x.GroupCode,
                    x.CustomerOrSupplier,
                    x.Rate,
                    x.EffectiveDate
                })
                .ToListAsync();

            return Ok(list);
        }

        // GET: api/PriceMaster/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _context.PriceMasters.FindAsync(id);

            if (item == null)
                return NotFound(new { message = "Price record not found" });

            return Ok(item);
        }

        // POST: api/PriceMaster
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PriceMaster model)
        {
            if (model.PartNumberId <= 0 ||
                string.IsNullOrWhiteSpace(model.GroupCode) ||
                string.IsNullOrWhiteSpace(model.CustomerOrSupplier) ||
                model.EffectiveDate == default)
            {
                return BadRequest(new { message = "Please fill all required fields" });
            }

            if (!ValidTypes.Contains(model.CustomerOrSupplier))
                return BadRequest(new { message = "Customer/Supplier must be 'Customer' or 'Supplier'" });

            var partExists = await _context.ItemMasters.AnyAsync(x => x.Id == model.PartNumberId);
            if (!partExists)
                return BadRequest(new { message = "Selected Part Number does not exist" });

            var entity = new PriceMaster
            {
                PartNumberId = model.PartNumberId,
                GroupCode = model.GroupCode.Trim(),
                CustomerOrSupplier = model.CustomerOrSupplier,
                Rate = model.Rate,
                EffectiveDate = model.EffectiveDate,
                CreatedDate = DateTime.Now
            };

            _context.PriceMasters.Add(entity);
            await _context.SaveChangesAsync();

            return Ok(entity);
        }

        // PUT: api/PriceMaster/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] PriceMaster model)
        {
            var entity = await _context.PriceMasters.FindAsync(id);

            if (entity == null)
                return NotFound(new { message = "Price record not found" });

            if (model.PartNumberId <= 0 ||
                string.IsNullOrWhiteSpace(model.GroupCode) ||
                string.IsNullOrWhiteSpace(model.CustomerOrSupplier) ||
                model.EffectiveDate == default)
            {
                return BadRequest(new { message = "Please fill all required fields" });
            }

            if (!ValidTypes.Contains(model.CustomerOrSupplier))
                return BadRequest(new { message = "Customer/Supplier must be 'Customer' or 'Supplier'" });

            var partExists = await _context.ItemMasters.AnyAsync(x => x.Id == model.PartNumberId);
            if (!partExists)
                return BadRequest(new { message = "Selected Part Number does not exist" });

            entity.PartNumberId = model.PartNumberId;
            entity.GroupCode = model.GroupCode.Trim();
            entity.CustomerOrSupplier = model.CustomerOrSupplier;
            entity.Rate = model.Rate;
            entity.EffectiveDate = model.EffectiveDate;
            entity.ModifiedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(entity);
        }

        // DELETE: api/PriceMaster/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _context.PriceMasters.FindAsync(id);

            if (entity == null)
                return NotFound(new { message = "Price record not found" });

            _context.PriceMasters.Remove(entity);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Deleted Successfully" });
        }
    }
}