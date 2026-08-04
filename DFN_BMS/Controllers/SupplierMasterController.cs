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
    public class SupplierMasterController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SupplierMasterController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/SupplierMaster
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _context.SupplierMasters
                .Include(x => x.SupplierGroup)
                .OrderByDescending(x => x.Id)
                .Select(x => new
                {
                    x.Id,
                    x.SupplierCode,
                    x.SupplierName,
                    x.VendorCode,
                    x.SupplierGroupId,
                    SupplierGroupName = x.SupplierGroup.SupplierGroupType,
                    x.Email,
                    x.ContactNumber,
                    x.PersonToContact,
                    x.GstNo,
                    x.PanNo,
                    x.BillingAddressLine1,
                    x.BillingAddressLine2,
                    x.BillingState,
                    x.BillingStateCode,
                    x.BillingPinCode,
                    x.ShippingAddressLine1,
                    x.ShippingAddressLine2,
                    x.ShippingState,
                    x.ShippingStateCode,
                    x.ShippingPinCode
                })
                .ToListAsync();

            return Ok(list);
        }

        // GET: api/SupplierMaster/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _context.SupplierMasters.FindAsync(id);

            if (item == null)
                return NotFound(new { message = "Supplier not found" });

            return Ok(item);
        }

        // GET: api/SupplierMaster/next-number
        [HttpGet("next-number")]
        public async Task<IActionResult> GetNextNumber()
        {
            var number = await GenerateSupplierCodeAsync();
            return Ok(new { supplierCode = number });
        }

        private async Task<string> GenerateSupplierCodeAsync()
        {
            var lastSupplier = await _context.SupplierMasters
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync();

            int nextSeq = 1;

            if (lastSupplier != null && lastSupplier.SupplierCode.StartsWith("SUP-"))
            {
                var numericPart = lastSupplier.SupplierCode.Substring(4);
                if (int.TryParse(numericPart, out int lastSeq))
                {
                    nextSeq = lastSeq + 1;
                }
            }

            return $"SUP-{nextSeq:D4}";
        }

        // POST: api/SupplierMaster
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SupplierMaster model)
        {
            if (string.IsNullOrWhiteSpace(model.SupplierName) ||
                string.IsNullOrWhiteSpace(model.VendorCode) ||
                model.SupplierGroupId <= 0 ||
                string.IsNullOrWhiteSpace(model.Email) ||
                string.IsNullOrWhiteSpace(model.ContactNumber) ||
                string.IsNullOrWhiteSpace(model.PersonToContact) ||
                string.IsNullOrWhiteSpace(model.GstNo) ||
                string.IsNullOrWhiteSpace(model.PanNo) ||
                string.IsNullOrWhiteSpace(model.BillingAddressLine1) ||
                string.IsNullOrWhiteSpace(model.BillingState) ||
                string.IsNullOrWhiteSpace(model.BillingStateCode) ||
                string.IsNullOrWhiteSpace(model.BillingPinCode) ||
                string.IsNullOrWhiteSpace(model.ShippingAddressLine1) ||
                string.IsNullOrWhiteSpace(model.ShippingState) ||
                string.IsNullOrWhiteSpace(model.ShippingStateCode) ||
                string.IsNullOrWhiteSpace(model.ShippingPinCode))
            {
                return BadRequest(new { message = "Please fill all required fields" });
            }

            var groupExists = await _context.SupplierGroupMasters.AnyAsync(g => g.Id == model.SupplierGroupId);
            if (!groupExists)
                return BadRequest(new { message = "Selected Supplier Group does not exist" });

            var nameExists = await _context.SupplierMasters
                .AnyAsync(x => x.SupplierName.ToLower() == model.SupplierName.Trim().ToLower());

            if (nameExists)
                return BadRequest(new { message = "Supplier Name already exists" });

            var gstExists = await _context.SupplierMasters
                .AnyAsync(x => x.GstNo.ToLower() == model.GstNo.Trim().ToLower());

            if (gstExists)
                return BadRequest(new { message = "GST No already exists" });

            var entity = new SupplierMaster
            {
                SupplierCode = await GenerateSupplierCodeAsync(),
                SupplierName = model.SupplierName.Trim(),
                VendorCode = model.VendorCode.Trim(),
                SupplierGroupId = model.SupplierGroupId,
                Email = model.Email.Trim(),
                ContactNumber = model.ContactNumber.Trim(),
                PersonToContact = model.PersonToContact.Trim(),
                GstNo = model.GstNo.Trim().ToUpper(),
                PanNo = model.PanNo.Trim().ToUpper(),
                BillingAddressLine1 = model.BillingAddressLine1.Trim(),
                BillingAddressLine2 = model.BillingAddressLine2?.Trim(),
                BillingState = model.BillingState.Trim(),
                BillingStateCode = model.BillingStateCode.Trim(),
                BillingPinCode = model.BillingPinCode.Trim(),
                ShippingAddressLine1 = model.ShippingAddressLine1.Trim(),
                ShippingAddressLine2 = model.ShippingAddressLine2?.Trim(),
                ShippingState = model.ShippingState.Trim(),
                ShippingStateCode = model.ShippingStateCode.Trim(),
                ShippingPinCode = model.ShippingPinCode.Trim(),
                CreatedDate = DateTime.Now
            };

            _context.SupplierMasters.Add(entity);
            await _context.SaveChangesAsync();

            return Ok(entity);
        }

        // PUT: api/SupplierMaster/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] SupplierMaster model)
        {
            var entity = await _context.SupplierMasters.FindAsync(id);

            if (entity == null)
                return NotFound(new { message = "Supplier not found" });

            if (string.IsNullOrWhiteSpace(model.SupplierName) ||
                string.IsNullOrWhiteSpace(model.VendorCode) ||
                model.SupplierGroupId <= 0 ||
                string.IsNullOrWhiteSpace(model.Email) ||
                string.IsNullOrWhiteSpace(model.ContactNumber) ||
                string.IsNullOrWhiteSpace(model.PersonToContact) ||
                string.IsNullOrWhiteSpace(model.GstNo) ||
                string.IsNullOrWhiteSpace(model.PanNo) ||
                string.IsNullOrWhiteSpace(model.BillingAddressLine1) ||
                string.IsNullOrWhiteSpace(model.BillingState) ||
                string.IsNullOrWhiteSpace(model.BillingStateCode) ||
                string.IsNullOrWhiteSpace(model.BillingPinCode) ||
                string.IsNullOrWhiteSpace(model.ShippingAddressLine1) ||
                string.IsNullOrWhiteSpace(model.ShippingState) ||
                string.IsNullOrWhiteSpace(model.ShippingStateCode) ||
                string.IsNullOrWhiteSpace(model.ShippingPinCode))
            {
                return BadRequest(new { message = "Please fill all required fields" });
            }

            var groupExists = await _context.SupplierGroupMasters.AnyAsync(g => g.Id == model.SupplierGroupId);
            if (!groupExists)
                return BadRequest(new { message = "Selected Supplier Group does not exist" });

            var nameExists = await _context.SupplierMasters
                .AnyAsync(x => x.SupplierName.ToLower() == model.SupplierName.Trim().ToLower() && x.Id != id);

            if (nameExists)
                return BadRequest(new { message = "Supplier Name already exists" });

            var gstExists = await _context.SupplierMasters
                .AnyAsync(x => x.GstNo.ToLower() == model.GstNo.Trim().ToLower() && x.Id != id);

            if (gstExists)
                return BadRequest(new { message = "GST No already exists" });

            entity.SupplierName = model.SupplierName.Trim();
            entity.VendorCode = model.VendorCode.Trim();
            entity.SupplierGroupId = model.SupplierGroupId;
            entity.Email = model.Email.Trim();
            entity.ContactNumber = model.ContactNumber.Trim();
            entity.PersonToContact = model.PersonToContact.Trim();
            entity.GstNo = model.GstNo.Trim().ToUpper();
            entity.PanNo = model.PanNo.Trim().ToUpper();
            entity.BillingAddressLine1 = model.BillingAddressLine1.Trim();
            entity.BillingAddressLine2 = model.BillingAddressLine2?.Trim();
            entity.BillingState = model.BillingState.Trim();
            entity.BillingStateCode = model.BillingStateCode.Trim();
            entity.BillingPinCode = model.BillingPinCode.Trim();
            entity.ShippingAddressLine1 = model.ShippingAddressLine1.Trim();
            entity.ShippingAddressLine2 = model.ShippingAddressLine2?.Trim();
            entity.ShippingState = model.ShippingState.Trim();
            entity.ShippingStateCode = model.ShippingStateCode.Trim();
            entity.ShippingPinCode = model.ShippingPinCode.Trim();
            entity.ModifiedDate = DateTime.Now;
            // Note: SupplierCode is intentionally never changed on update.

            await _context.SaveChangesAsync();

            return Ok(entity);
        }

        // DELETE: api/SupplierMaster/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _context.SupplierMasters.FindAsync(id);

            if (entity == null)
                return NotFound(new { message = "Supplier not found" });

            _context.SupplierMasters.Remove(entity);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Deleted Successfully" });
        }
    }
}