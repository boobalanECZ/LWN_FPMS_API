using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DFN_BMS.DB;
using DFN_BMS.Models;

namespace DFN_BMS.Controllers
{
    // Mostly a read-only reference table (seeded via SQL script), exposed
    // so the Store Master frontend can populate its Pallet Type dropdown.
    [ApiController]
    [Route("api/[controller]")]
    public class PalletTypeController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PalletTypeController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/PalletType
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _context.PalletTypeMasters
                .OrderBy(x => x.PalletName)
                .Select(x => new
                {
                    x.Id,
                    x.PalletName,
                    x.RangeFrom,
                    x.RangeTo,
                    x.CurrentSequence
                })
                .ToListAsync();

            return Ok(list);
        }
    }
}