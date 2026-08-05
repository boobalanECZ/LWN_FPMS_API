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
    public class LocationRackController : ControllerBase
    {
        private readonly AppDbContext _context;

        public LocationRackController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/LocationRack/store/5
        // Full Rack -> Column -> Row tree for one store. Feeds both the
        // left-panel row table and the right-panel visualization.
        [HttpGet("store/{storeId}")]
        public async Task<IActionResult> GetRacksForStore(int storeId)
        {
            var racks = await _context.LocationRacks
                .Where(x => x.StoreId == storeId)
                .Include(x => x.Columns)
                    .ThenInclude(c => c.Rows)
                .OrderBy(x => x.RackNo)
                .Select(x => new
                {
                    x.Id,
                    x.RackNo,
                    Columns = x.Columns
                        .OrderBy(c => c.ColumnNo)
                        .Select(c => new
                        {
                            c.Id,
                            c.ColumnNo,
                            Rows = c.Rows
                                .OrderBy(r => r.RowNo)
                                .Select(r => new
                                {
                                    r.Id,
                                    r.RowNo,
                                    r.HasFront,
                                    r.HasRear,
                                    r.Fixture
                                })
                        })
                })
                .ToListAsync();

            return Ok(racks);
        }

        // POST: api/LocationRack/store/5/batch
        // Body: { rackNo: "A", rowCount: 4, fixture: 1 }
        // Finds-or-creates the Rack ("A") for this store, adds one new
        // Column under it with an auto-incrementing ColumnNo (e.g. "A1",
        // "A2", ...), then generates {rowCount} Rows (R1..Rn) under that
        // column, each with the given Fixture and Front+Rear checked.
        public class RackBatchRequest
        {
            public string RackNo { get; set; }
            public int RowCount { get; set; }
            public int Fixture { get; set; }
        }

        [HttpPost("store/{storeId}/batch")]
        public async Task<IActionResult> AddBatch(int storeId, [FromBody] RackBatchRequest req)
        {
            if (string.IsNullOrWhiteSpace(req?.RackNo))
                return BadRequest(new { message = "Rack No is required (e.g. A, B)" });

            var rackNo = req.RackNo.Trim().ToUpper();

            if (!System.Text.RegularExpressions.Regex.IsMatch(rackNo, @"^[A-Z]{1,3}$"))
                return BadRequest(new { message = "Rack No must be letters only (e.g. A, B, AB)" });

            if (req.RowCount <= 0)
                return BadRequest(new { message = "Row count must be greater than 0" });

            if (req.Fixture <= 0)
                return BadRequest(new { message = "Fixture must be greater than 0" });

            var storeExists = await _context.LocationMasters.AnyAsync(x => x.Id == storeId);
            if (!storeExists)
                return BadRequest(new { message = "Store not found" });

            var rack = await _context.LocationRacks
                .FirstOrDefaultAsync(x => x.StoreId == storeId && x.RackNo == rackNo);

            if (rack == null)
            {
                rack = new LocationRack { StoreId = storeId, RackNo = rackNo, CreatedDate = DateTime.Now };
                _context.LocationRacks.Add(rack);
                await _context.SaveChangesAsync();
            }

            var existingColumnCount = await _context.RackColumns
                .CountAsync(x => x.LocationRackId == rack.Id);

            var columnNo = $"{rackNo}{existingColumnCount + 1}";

            var column = new RackColumn
            {
                LocationRackId = rack.Id,
                ColumnNo = columnNo,
                CreatedDate = DateTime.Now
            };

            _context.RackColumns.Add(column);
            await _context.SaveChangesAsync();

            for (int i = 1; i <= req.RowCount; i++)
            {
                _context.RackRows.Add(new RackRow
                {
                    RackColumnId = column.Id,
                    RowNo = $"R{i}",
                    HasFront = true,
                    HasRear = true,
                    Fixture = req.Fixture,
                    CreatedDate = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();

            return Ok(new { rackId = rack.Id, columnId = column.Id, columnNo });
        }

        // PUT: api/LocationRack/rows/5
        [HttpPut("rows/{rowId}")]
        public async Task<IActionResult> UpdateRow(int rowId, [FromBody] RackRow model)
        {
            var entity = await _context.RackRows.FindAsync(rowId);

            if (entity == null)
                return NotFound(new { message = "Row not found" });

            if (!model.HasFront && !model.HasRear)
                return BadRequest(new { message = "Select at least Front or Rear" });

            if (model.Fixture <= 0)
                return BadRequest(new { message = "Fixture must be greater than 0" });

            entity.HasFront = model.HasFront;
            entity.HasRear = model.HasRear;
            entity.Fixture = model.Fixture;

            await _context.SaveChangesAsync();

            return Ok(entity);
        }

        // DELETE: api/LocationRack/rows/5
        [HttpDelete("rows/{rowId}")]
        public async Task<IActionResult> DeleteRow(int rowId)
        {
            var entity = await _context.RackRows.FindAsync(rowId);

            if (entity == null)
                return NotFound(new { message = "Row not found" });

            _context.RackRows.Remove(entity);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Deleted Successfully" });
        }

        // DELETE: api/LocationRack/5
        // Deletes an entire Rack (and its Columns/Rows via cascade).
        [HttpDelete("{rackId}")]
        public async Task<IActionResult> DeleteRack(int rackId)
        {
            var entity = await _context.LocationRacks.FindAsync(rackId);

            if (entity == null)
                return NotFound(new { message = "Rack not found" });

            _context.LocationRacks.Remove(entity);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Deleted Successfully" });
        }
    }
}