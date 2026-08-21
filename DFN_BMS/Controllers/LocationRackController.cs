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

        // POST: api/LocationRack/store/5/save-grid
        // Body: { rackNo: "A", rows: [{ columnNo, rowNo, hasFront, hasRear, fixture }, ...] }
        // Persists the ENTIRE client-built grid in one call. If a Rack
        // with this RackNo already exists for the store, its Columns/Rows
        // are replaced wholesale (this is how "edit an existing rack"
        // works — regenerate the grid, tweak it, Save again).
        public class SaveGridRowRequest
        {
            public string ColumnNo { get; set; }
            public string RowNo { get; set; }
            public bool HasFront { get; set; } = true;
            public bool HasRear { get; set; } = true;
            public int Fixture { get; set; } = 6;
        }

        public class SaveGridRequest
        {
            public string RackNo { get; set; }
            public System.Collections.Generic.List<SaveGridRowRequest> Rows { get; set; }
                = new System.Collections.Generic.List<SaveGridRowRequest>();
        }

        [HttpPost("store/{storeId}/save-grid")]
        public async Task<IActionResult> SaveGrid(int storeId, [FromBody] SaveGridRequest req)
        {
            try
            {
                if (req == null || string.IsNullOrWhiteSpace(req.RackNo))
                    return BadRequest(new { message = "Rack No is required" });

                var rackNo = req.RackNo.Trim().ToUpper();

                if (!System.Text.RegularExpressions.Regex.IsMatch(rackNo, @"^[A-Z]{1,5}$"))
                    return BadRequest(new { message = "Rack No must be letters only (e.g. A, B, AB)" });

                if (req.Rows == null || req.Rows.Count == 0)
                    return BadRequest(new { message = "Generate the grid first" });

                var storeExists = await _context.LocationMasters.AnyAsync(x => x.Id == storeId);
                if (!storeExists)
                    return BadRequest(new { message = "Store not found" });

                var rack = await _context.LocationRacks
                    .Include(x => x.Columns)
                        .ThenInclude(c => c.Rows)
                    .FirstOrDefaultAsync(x => x.StoreId == storeId && x.RackNo == rackNo);

                if (rack == null)
                {
                    rack = new LocationRack { StoreId = storeId, RackNo = rackNo, CreatedDate = DateTime.Now };
                    _context.LocationRacks.Add(rack);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    // Replace existing structure wholesale.
                    foreach (var col in rack.Columns.ToList())
                    {
                        _context.RackRows.RemoveRange(col.Rows);
                        _context.RackColumns.Remove(col);
                    }
                    await _context.SaveChangesAsync();
                }

                // Group incoming rows by ColumnNo so each distinct column is
                // created once with all its rows underneath.
                var columnGroups = req.Rows.GroupBy(r => r.ColumnNo);

                foreach (var group in columnGroups)
                {
                    var column = new RackColumn
                    {
                        LocationRackId = rack.Id,
                        ColumnNo = group.Key,
                        CreatedDate = DateTime.Now
                    };

                    _context.RackColumns.Add(column);
                    await _context.SaveChangesAsync();

                    foreach (var r in group)
                    {
                        _context.RackRows.Add(new RackRow
                        {
                            RackColumnId = column.Id,
                            RowNo = r.RowNo,
                            HasFront = r.HasFront,
                            HasRear = r.HasRear,
                            Fixture = r.Fixture,
                            CreatedDate = DateTime.Now
                        });
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new { rack.Id, rack.RackNo, message = "Rack Saved" });
            }
            catch (Exception ex)
            {
                // Surface the real error instead of a bare 500 with no body,
                // so the frontend toast shows something actionable.
                var detail = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { message = $"Save failed: {detail}" });
            }
        }

        // GET: api/LocationRack/store/5/occupancy
        // Returns every currently-occupied slot in this store, as a flat
        // list of { rackRowId, slotNumber, side, quantity, note }. The
        // frontend turns this into a lookup set to decide slot colour —
        // "available" is the default; a slot only turns
        // occupied-front/occupied-rear once a real StoreMovement exists
        // for it.
        [HttpGet("store/{storeId}/occupancy")]
        public async Task<IActionResult> GetOccupancy(int storeId)
        {
            var occupied = await _context.StoreMovements
                .Where(m => m.RackRowId != null &&
                            m.RackRow.Column.Rack.StoreId == storeId)
                .Select(m => new
                {
                    m.Id,
                    m.RackRowId,
                    m.SlotNumber,
                    m.Side,
                    m.Quantity,
                    m.Note,
                    PalletNo = m.GrnPallet != null ? m.GrnPallet.PalletNo : null   // NEW
                })
                .ToListAsync();

            return Ok(occupied);
        }

        // POST: api/LocationRack/slots/occupy
        // Quick manual occupy of one specific slot (no GRN/pallet tie —
        // used by clicking an "Available" slot in the rack preview).
        public class OccupySlotRequest
        {
            public int RackRowId { get; set; }
            public int SlotNumber { get; set; }
            public string Side { get; set; } = "Front";
            public decimal Quantity { get; set; }
            public string? Note { get; set; }
        }

        [HttpPost("slots/occupy")]
        public async Task<IActionResult> OccupySlot([FromBody] OccupySlotRequest req)
        {
            if (req == null || req.RackRowId <= 0 || req.SlotNumber <= 0)
                return BadRequest(new { message = "Rack Row and Slot Number are required" });

            if (req.Side != "Front" && req.Side != "Rear")
                return BadRequest(new { message = "Side must be 'Front' or 'Rear'" });

            if (req.Quantity <= 0)
                return BadRequest(new { message = "Quantity must be greater than 0" });

            var row = await _context.RackRows.FindAsync(req.RackRowId);
            if (row == null)
                return BadRequest(new { message = "Rack Row not found" });

            var alreadyOccupied = await _context.StoreMovements
                .AnyAsync(m => m.RackRowId == req.RackRowId && m.SlotNumber == req.SlotNumber && m.Side == req.Side);

            if (alreadyOccupied)
                return BadRequest(new { message = "That slot is already occupied" });

            var movement = new StoreMovement
            {
                RackRowId = req.RackRowId,
                SlotNumber = req.SlotNumber,
                Side = req.Side,
                Quantity = req.Quantity,
                Note = req.Note?.Trim(),
                MovementDate = DateTime.Now
            };

            _context.StoreMovements.Add(movement);
            await _context.SaveChangesAsync();

            return Ok(new { movement.Id, message = "Slot Occupied" });
        }

        // DELETE: api/LocationRack/slots/occupy/5
        // Vacates a slot (deletes the movement), turning it back to
        // "Available".
        [HttpDelete("slots/occupy/{movementId}")]
        public async Task<IActionResult> VacateSlot(int movementId)
        {
            var movement = await _context.StoreMovements.FindAsync(movementId);

            if (movement == null)
                return NotFound(new { message = "Movement not found" });

            _context.StoreMovements.Remove(movement);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Slot Vacated" });
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