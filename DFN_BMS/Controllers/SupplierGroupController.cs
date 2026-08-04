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
    public class SupplierGroupController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SupplierGroupController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/SupplierGroup
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _context.SupplierGroupMasters
                .OrderByDescending(x => x.Id)
                .ToListAsync();

            return Ok(list);
        }

        // GET: api/SupplierGroup/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _context.SupplierGroupMasters.FindAsync(id);

            if (item == null)
                return NotFound(new { message = "Supplier Group not found" });

            return Ok(item);
        }

        // POST: api/SupplierGroup
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SupplierGroupMaster model)
        {
            if (string.IsNullOrWhiteSpace(model.SupplierGroupType))
                return BadRequest(new { message = "Supplier Group Type is required" });

            var typeExists = await _context.SupplierGroupMasters
                .AnyAsync(x => x.SupplierGroupType.ToLower() == model.SupplierGroupType.Trim().ToLower());

            if (typeExists)
                return BadRequest(new { message = "Supplier Group Type already exists" });

            var entity = new SupplierGroupMaster
            {
                SupplierGroupType = model.SupplierGroupType.Trim(),
                Description = model.Description?.Trim(),
                IsActive = true,
                CreatedDate = DateTime.Now
            };

            _context.SupplierGroupMasters.Add(entity);
            await _context.SaveChangesAsync();

            return Ok(entity);
        }

        // PUT: api/SupplierGroup/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] SupplierGroupMaster model)
        {
            var entity = await _context.SupplierGroupMasters.FindAsync(id);

            if (entity == null)
                return NotFound(new { message = "Supplier Group not found" });

            if (string.IsNullOrWhiteSpace(model.SupplierGroupType))
                return BadRequest(new { message = "Supplier Group Type is required" });

            var typeExists = await _context.SupplierGroupMasters
                .AnyAsync(x => x.SupplierGroupType.ToLower() == model.SupplierGroupType.Trim().ToLower() && x.Id != id);

            if (typeExists)
                return BadRequest(new { message = "Supplier Group Type already exists" });

            entity.SupplierGroupType = model.SupplierGroupType.Trim();
            entity.Description = model.Description?.Trim();
            entity.ModifiedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(entity);
        }

        // DELETE: api/SupplierGroup/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _context.SupplierGroupMasters.FindAsync(id);

            if (entity == null)
                return NotFound(new { message = "Supplier Group not found" });

            _context.SupplierGroupMasters.Remove(entity);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Deleted Successfully" });
        }
    }
}