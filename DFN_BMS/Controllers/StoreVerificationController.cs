using DFN_BMS.DB;
using DFN_BMS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DFN_BMS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StoreVerificationController : ControllerBase
    {
        private readonly AppDbContext _context;

        public StoreVerificationController(
            AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] StoreVerification model)
        {
            try
            {
                if (model == null)
                    return BadRequest(new { success = false, message = "Request body is required." });

                if (model.PalletId == null || model.PalletId <= 0)
                    return BadRequest(new { success = false, message = "Valid PalletId is required." });

                if (model.ItemId <= 0)
                    return BadRequest(new { success = false, message = "Valid ItemId is required." });

                if (string.IsNullOrWhiteSpace(model.PalletNo))
                    return BadRequest(new { success = false, message = "Pallet No is required." });

                if (model.Quantity <= 0)
                    return BadRequest(new { success = false, message = "Quantity must be greater than 0." });

                // NEW — the actual duplicate guard. Checked against the real
                // unique pallet identity, not the recyclable PalletNo label.
                var alreadyVerified = await _context.StoreVerification
                    .AnyAsync(x => x.PalletId == model.PalletId);

                if (alreadyVerified)
                {
                    return Conflict(new
                    {
                        success = false,
                        message = $"Pallet {model.PalletNo} (GRN {model.GrnNumber ?? "—"}) has already been verified. Duplicate rejected."
                    });
                }

                var entity = new StoreVerification
                {
                    PalletId = model.PalletId,
                    ItemId = model.ItemId,
                    GrnNumber = string.IsNullOrWhiteSpace(model.GrnNumber) ? null : model.GrnNumber.Trim(),
                    PalletNo = model.PalletNo.Trim(),
                    Quantity = model.Quantity,
                    StoreLocation = string.IsNullOrWhiteSpace(model.StoreLocation) ? null : model.StoreLocation.Trim(),
                    VerifiedAt = model.VerifiedAt == default ? DateTime.Now : model.VerifiedAt,
                    CreatedDate = DateTime.Now
                };

                _context.StoreVerification.Add(entity);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, id = entity.Id, message = "Store verification saved successfully." });
            }
            catch (DbUpdateException ex)
            {
                // A race that slips past the AnyAsync check still hits the
                // unique index and lands here instead of corrupting data.
                var innerMessage = ex.InnerException?.InnerException?.Message ?? ex.InnerException?.Message ?? ex.Message;
                if (innerMessage.Contains("UX_StoreVerification_PalletId"))
                {
                    return Conflict(new { success = false, message = "This pallet has already been verified (duplicate detected at save time)." });
                }
                return StatusCode(500, new { success = false, message = "Database save failed.", detail = innerMessage });
            }
            catch (Exception ex)
            {
                var innerMessage = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { success = false, message = "Store verification save failed.", detail = innerMessage });
            }
        }

        // GET: api/StoreVerification/verified-pallet-ids
        // Every PalletId already verified, server-confirmed. The mobile app
        // caches this on every Data Sync so its local duplicate guard survives
        // app restarts and stays correct even after pending records upload
        // and disappear from the local queue.
        [HttpGet("verified-pallet-ids")]
        public async Task<IActionResult> GetVerifiedPalletIds()
        {
            var ids = await _context.StoreVerification
                .Where(x => x.PalletId != null)
                .Select(x => x.PalletId)
                .Distinct()
                .ToListAsync();

            return Ok(ids);
        }
    }
}