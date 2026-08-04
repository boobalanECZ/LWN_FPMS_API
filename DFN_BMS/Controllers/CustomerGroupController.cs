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
    public class CustomerGroupController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CustomerGroupController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/CustomerGroup
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _context.CustomerGroupMasters
                .OrderByDescending(x => x.Id)
                .ToListAsync();

            return Ok(list);
        }

        // GET: api/CustomerGroup/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _context.CustomerGroupMasters.FindAsync(id);

            if (item == null)
                return NotFound(new { message = "Customer Group not found" });

            return Ok(item);
        }

        // POST: api/CustomerGroup
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CustomerGroupMaster model)
        {
            if (string.IsNullOrWhiteSpace(model.CustomerGroupType))
                return BadRequest(new { message = "Customer Group Type is required" });

            var typeExists = await _context.CustomerGroupMasters
                .AnyAsync(x => x.CustomerGroupType.ToLower() == model.CustomerGroupType.Trim().ToLower());

            if (typeExists)
                return BadRequest(new { message = "Customer Group Type already exists" });

            var entity = new CustomerGroupMaster
            {
                CustomerGroupType = model.CustomerGroupType.Trim(),
                Description = model.Description?.Trim(),
                IsActive = true,
                CreatedDate = DateTime.Now
            };

            _context.CustomerGroupMasters.Add(entity);
            await _context.SaveChangesAsync();

            return Ok(entity);
        }

        // PUT: api/CustomerGroup/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CustomerGroupMaster model)
        {
            var entity = await _context.CustomerGroupMasters.FindAsync(id);

            if (entity == null)
                return NotFound(new { message = "Customer Group not found" });

            if (string.IsNullOrWhiteSpace(model.CustomerGroupType))
                return BadRequest(new { message = "Customer Group Type is required" });

            var typeExists = await _context.CustomerGroupMasters
                .AnyAsync(x => x.CustomerGroupType.ToLower() == model.CustomerGroupType.Trim().ToLower() && x.Id != id);

            if (typeExists)
                return BadRequest(new { message = "Customer Group Type already exists" });

            entity.CustomerGroupType = model.CustomerGroupType.Trim();
            entity.Description = model.Description?.Trim();
            entity.ModifiedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(entity);
        }

        // DELETE: api/CustomerGroup/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _context.CustomerGroupMasters.FindAsync(id);

            if (entity == null)
                return NotFound(new { message = "Customer Group not found" });

            _context.CustomerGroupMasters.Remove(entity);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Deleted Successfully" });
        }
    }
}