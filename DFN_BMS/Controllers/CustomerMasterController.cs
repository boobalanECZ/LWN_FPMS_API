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
    public class CustomerMasterController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CustomerMasterController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/CustomerMaster
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _context.CustomerMasters
                .OrderByDescending(x => x.Id)
                .ToListAsync();

            return Ok(list);
        }

        // GET: api/CustomerMaster/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _context.CustomerMasters.FindAsync(id);

            if (item == null)
                return NotFound(new { message = "Customer not found" });

            return Ok(item);
        }

        // POST: api/CustomerMaster
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CustomerMaster model)
        {
            if (string.IsNullOrWhiteSpace(model.CustomerCode) ||
                string.IsNullOrWhiteSpace(model.CustomerName) ||
                string.IsNullOrWhiteSpace(model.CustomerDivision) ||
                string.IsNullOrWhiteSpace(model.MobileNumber) ||
                string.IsNullOrWhiteSpace(model.EmailId))
            {
                return BadRequest(new { message = "Please fill all required fields" });
            }

            var codeExists = await _context.CustomerMasters
                .AnyAsync(x => x.CustomerCode.ToLower() == model.CustomerCode.Trim().ToLower());

            if (codeExists)
                return BadRequest(new { message = "Customer ID already exists" });

            var nameExists = await _context.CustomerMasters
                .AnyAsync(x => x.CustomerName.ToLower() == model.CustomerName.Trim().ToLower());

            if (nameExists)
                return BadRequest(new { message = "Customer Name already exists" });

            var emailExists = await _context.CustomerMasters
                .AnyAsync(x => x.EmailId.ToLower() == model.EmailId.Trim().ToLower());

            if (emailExists)
                return BadRequest(new { message = "Email ID already exists" });

            var entity = new CustomerMaster
            {
                CustomerCode = model.CustomerCode.Trim(),
                CustomerName = model.CustomerName.Trim(),
                CustomerDivision = model.CustomerDivision.Trim(),
                MobileNumber = model.MobileNumber.Trim(),
                EmailId = model.EmailId.Trim(),
                CreatedDate = DateTime.Now
            };

            _context.CustomerMasters.Add(entity);
            await _context.SaveChangesAsync();

            return Ok(entity);
        }

        // PUT: api/CustomerMaster/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CustomerMaster model)
        {
            var entity = await _context.CustomerMasters.FindAsync(id);

            if (entity == null)
                return NotFound(new { message = "Customer not found" });

            if (string.IsNullOrWhiteSpace(model.CustomerName) ||
                string.IsNullOrWhiteSpace(model.CustomerDivision) ||
                string.IsNullOrWhiteSpace(model.MobileNumber) ||
                string.IsNullOrWhiteSpace(model.EmailId))
            {
                return BadRequest(new { message = "Please fill all required fields" });
            }

            var nameExists = await _context.CustomerMasters
                .AnyAsync(x => x.CustomerName.ToLower() == model.CustomerName.Trim().ToLower() && x.Id != id);

            if (nameExists)
                return BadRequest(new { message = "Customer Name already exists" });

            var emailExists = await _context.CustomerMasters
                .AnyAsync(x => x.EmailId.ToLower() == model.EmailId.Trim().ToLower() && x.Id != id);

            if (emailExists)
                return BadRequest(new { message = "Email ID already exists" });

            entity.CustomerName = model.CustomerName.Trim();
            entity.CustomerDivision = model.CustomerDivision.Trim();
            entity.MobileNumber = model.MobileNumber.Trim();
            entity.EmailId = model.EmailId.Trim();
            entity.ModifiedDate = DateTime.Now;
            // Note: CustomerCode is intentionally never changed on update.

            await _context.SaveChangesAsync();

            return Ok(entity);
        }

        // DELETE: api/CustomerMaster/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _context.CustomerMasters.FindAsync(id);

            if (entity == null)
                return NotFound(new { message = "Customer not found" });

            _context.CustomerMasters.Remove(entity);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Deleted Successfully" });
        }
    }
}