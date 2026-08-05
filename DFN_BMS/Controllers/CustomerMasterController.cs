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
    public class CustomerMasterController : ControllerBase
    {
        private readonly AppDbContext _context;

        // 2 digits (state code) + 5 letters (PAN) + 4 digits (PAN) + 1 letter (PAN)
        // + 1 alphanumeric (entity code) + literal 'Z' + 1 alphanumeric (checksum)
        private static readonly Regex GstRegex =
            new Regex(@"^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z]{1}[1-9A-Z]{1}Z[0-9A-Z]{1}$");

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
                string.IsNullOrWhiteSpace(model.EmailId) ||
                string.IsNullOrWhiteSpace(model.GstNo))
            {
                return BadRequest(new { message = "Please fill all required fields" });
            }

            var gstNo = model.GstNo.Trim().ToUpper();

            if (!GstRegex.IsMatch(gstNo))
                return BadRequest(new { message = "Enter a valid 15-character GSTIN (e.g. 33ABCDE1234F1Z5)" });

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

            var gstExists = await _context.CustomerMasters
                .AnyAsync(x => x.GstNo.ToLower() == gstNo.ToLower());

            if (gstExists)
                return BadRequest(new { message = "GST No already exists" });

            var entity = new CustomerMaster
            {
                CustomerCode = model.CustomerCode.Trim(),
                CustomerName = model.CustomerName.Trim(),
                CustomerDivision = model.CustomerDivision.Trim(),
                MobileNumber = model.MobileNumber.Trim(),
                EmailId = model.EmailId.Trim(),
                GstNo = gstNo,
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
                string.IsNullOrWhiteSpace(model.EmailId) ||
                string.IsNullOrWhiteSpace(model.GstNo))
            {
                return BadRequest(new { message = "Please fill all required fields" });
            }

            var gstNo = model.GstNo.Trim().ToUpper();

            if (!GstRegex.IsMatch(gstNo))
                return BadRequest(new { message = "Enter a valid 15-character GSTIN (e.g. 33ABCDE1234F1Z5)" });

            var nameExists = await _context.CustomerMasters
                .AnyAsync(x => x.CustomerName.ToLower() == model.CustomerName.Trim().ToLower() && x.Id != id);

            if (nameExists)
                return BadRequest(new { message = "Customer Name already exists" });

            var emailExists = await _context.CustomerMasters
                .AnyAsync(x => x.EmailId.ToLower() == model.EmailId.Trim().ToLower() && x.Id != id);

            if (emailExists)
                return BadRequest(new { message = "Email ID already exists" });

            var gstExists = await _context.CustomerMasters
                .AnyAsync(x => x.GstNo.ToLower() == gstNo.ToLower() && x.Id != id);

            if (gstExists)
                return BadRequest(new { message = "GST No already exists" });

            entity.CustomerName = model.CustomerName.Trim();
            entity.CustomerDivision = model.CustomerDivision.Trim();
            entity.MobileNumber = model.MobileNumber.Trim();
            entity.EmailId = model.EmailId.Trim();
            entity.GstNo = gstNo;
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