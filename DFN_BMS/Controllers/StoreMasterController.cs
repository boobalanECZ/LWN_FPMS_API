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
                .OrderByDescending(x => x.Id)
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

        // POST: api/StoreMaster
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] StoreMaster model)
        {
            if (string.IsNullOrWhiteSpace(model.StoreLocation) ||
                string.IsNullOrWhiteSpace(model.PalletNumber) ||
                string.IsNullOrWhiteSpace(model.ColourCode))
            {
                return BadRequest(new { message = "Please fill all required fields" });
            }

            var colourCode = model.ColourCode.Trim();

            if (!ColourRegex.IsMatch(colourCode))
                return BadRequest(new { message = "Colour must be a valid hex code (e.g. #1E88E5)" });

            var palletExists = await _context.StoreMasters
                .AnyAsync(x => x.PalletNumber.ToLower() == model.PalletNumber.Trim().ToLower());

            if (palletExists)
                return BadRequest(new { message = "Pallet Number already exists" });

            var entity = new StoreMaster
            {
                StoreLocation = model.StoreLocation.Trim(),
                PalletNumber = model.PalletNumber.Trim(),
                ColourCode = colourCode.ToUpper(),
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
                string.IsNullOrWhiteSpace(model.PalletNumber) ||
                string.IsNullOrWhiteSpace(model.ColourCode))
            {
                return BadRequest(new { message = "Please fill all required fields" });
            }

            var colourCode = model.ColourCode.Trim();

            if (!ColourRegex.IsMatch(colourCode))
                return BadRequest(new { message = "Colour must be a valid hex code (e.g. #1E88E5)" });

            var palletExists = await _context.StoreMasters
                .AnyAsync(x => x.PalletNumber.ToLower() == model.PalletNumber.Trim().ToLower() && x.Id != id);

            if (palletExists)
                return BadRequest(new { message = "Pallet Number already exists" });

            entity.StoreLocation = model.StoreLocation.Trim();
            entity.PalletNumber = model.PalletNumber.Trim();
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