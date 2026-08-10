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
    public class StoreMovementController : ControllerBase
    {
        private readonly AppDbContext _context;

        public StoreMovementController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/StoreMovement/grn/5/pallets
        [HttpGet("grn/{grnId}/pallets")]
        public async Task<IActionResult> GetPalletsForGrn(int grnId)
        {
            try
            {
                var lines = await _context.GrnLines
                    .Include(x => x.Item)
                    .Where(x => x.GrnHeaderId == grnId)
                    .ToListAsync();

                if (lines.Count == 0)
                    return NotFound(new { message = "GRN not found or has no lines" });

                foreach (var line in lines)
                {
                    var exists = await _context.GrnPallets.AnyAsync(p => p.GrnLineId == line.Id);
                    if (!exists)
                    {
                        var palletNo = await GenerateNextPalletNoAsync();
                        _context.GrnPallets.Add(new GrnPallet
                        {
                            GrnLineId = line.Id,
                            PalletNo = palletNo,
                            Quantity = line.Quantity,
                            Rate = line.Rate,
                            CreatedDate = DateTime.Now
                        });
                        await _context.SaveChangesAsync();
                    }
                }

                var pallets = await _context.GrnPallets
                    .Include(x => x.GrnLine)
                        .ThenInclude(l => l.Item)
                    .Where(x => x.GrnLine.GrnHeaderId == grnId)
                    .OrderBy(x => x.PalletNo)
                    .Select(x => new
                    {
                        x.Id,
                        x.PalletNo,
                        x.Quantity,
                        x.Rate,
                        PartNumber = x.GrnLine.Item.ItemNumber,
                        PartName = x.GrnLine.Item.ItemName,
                        StuffedQty = _context.StoreMovements
                            .Where(m => m.GrnPalletId == x.Id)
                            .Sum(m => (decimal?)m.Quantity) ?? 0,
                        Assignments = _context.StoreMovements
                            .Where(m => m.GrnPalletId == x.Id && m.StorePositionId != null)
                            .Select(m => new
                            {
                                m.Id,
                                StoreLocation = m.StorePosition.Store.StoreLocation,
                                PositionCode = m.StorePosition.PositionCode,
                                m.Side,
                                m.Quantity
                            })
                            .ToList()
                    })
                    .ToListAsync();

                return Ok(pallets);
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { message = $"Failed to load pallets: {detail}" });
            }
        }

        private async Task<string> GenerateNextPalletNoAsync()
        {
            var last = await _context.GrnPallets
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync();

            int nextSeq = 1;

            if (last != null && last.PalletNo.StartsWith("P"))
            {
                if (int.TryParse(last.PalletNo.Substring(1), out int lastSeq))
                    nextSeq = lastSeq + 1;
            }

            return $"P{nextSeq:D3}";
        }

        // GET: api/StoreMovement/positions
        [HttpGet("positions")]
        public async Task<IActionResult> GetPositions()
        {
            try
            {
                var stores = await _context.StoreMasters.ToListAsync();

                foreach (var store in stores)
                {
                    var hasPositions = await _context.StorePositions.AnyAsync(p => p.StoreMasterId == store.Id);
                    if (!hasPositions)
                    {
                        _context.StorePositions.Add(new StorePosition { StoreMasterId = store.Id, PositionCode = "P1", Capacity = 5000 });
                        _context.StorePositions.Add(new StorePosition { StoreMasterId = store.Id, PositionCode = "P2", Capacity = 5000 });
                        await _context.SaveChangesAsync();
                    }
                }

                var result = await _context.StoreMasters
                    .Select(s => new
                    {
                        s.Id,
                        s.StoreLocation,
                        Positions = _context.StorePositions
                            .Where(p => p.StoreMasterId == s.Id)
                            .OrderBy(p => p.PositionCode)
                            .Select(p => new
                            {
                                p.Id,
                                p.PositionCode,
                                p.Capacity,
                                Stuffed = _context.StoreMovements
                                    .Where(m => m.StorePositionId == p.Id)
                                    .Sum(m => (decimal?)m.Quantity) ?? 0
                            })
                            .ToList()
                    })
                    .ToListAsync();

                return Ok(result);
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { message = $"Failed to load store positions: {detail}" });
            }
        }

        // POST: api/StoreMovement/stuff
        public class StuffRequest
        {
            public int GrnPalletId { get; set; }
            public int StorePositionId { get; set; }
            public string Side { get; set; } = "Front";
            public decimal Quantity { get; set; }
        }

        [HttpPost("stuff")]
        public async Task<IActionResult> Stuff([FromBody] StuffRequest req)
        {
            try
            {
                if (req == null || req.GrnPalletId <= 0 || req.StorePositionId <= 0)
                    return BadRequest(new { message = "Pallet and Store Position are required" });

                if (req.Side != "Front" && req.Side != "Rear")
                    return BadRequest(new { message = "Side must be 'Front' or 'Rear'" });

                if (req.Quantity <= 0)
                    return BadRequest(new { message = "Quantity must be greater than 0" });

                var pallet = await _context.GrnPallets.FindAsync(req.GrnPalletId);
                if (pallet == null)
                    return BadRequest(new { message = "Pallet not found" });

                var position = await _context.StorePositions.FindAsync(req.StorePositionId);
                if (position == null)
                    return BadRequest(new { message = "Store Position not found" });

                var alreadyStuffed = await _context.StoreMovements
                    .Where(m => m.GrnPalletId == req.GrnPalletId)
                    .SumAsync(m => (decimal?)m.Quantity) ?? 0;

                var remaining = pallet.Quantity - alreadyStuffed;

                if (req.Quantity > remaining)
                    return BadRequest(new { message = $"Only {remaining} remaining on this pallet" });

                var positionStuffed = await _context.StoreMovements
                    .Where(m => m.StorePositionId == req.StorePositionId)
                    .SumAsync(m => (decimal?)m.Quantity) ?? 0;

                var available = position.Capacity - positionStuffed;

                if (req.Quantity > available)
                    return BadRequest(new { message = $"Only {available} available space in {position.PositionCode}" });

                var movement = new StoreMovement
                {
                    GrnPalletId = req.GrnPalletId,
                    StorePositionId = req.StorePositionId,
                    Side = req.Side,
                    Quantity = req.Quantity,
                    MovementDate = DateTime.Now
                };

                _context.StoreMovements.Add(movement);
                await _context.SaveChangesAsync();

                return Ok(new { movement.Id, message = "Stuffed Successfully" });
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { message = $"Stuff failed: {detail}" });
            }
        }

        // DELETE: api/StoreMovement/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Undo(int id)
        {
            try
            {
                var movement = await _context.StoreMovements.FindAsync(id);

                if (movement == null)
                    return NotFound(new { message = "Movement not found" });

                _context.StoreMovements.Remove(movement);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Movement Undone" });
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { message = $"Undo failed: {detail}" });
            }
        }
    }
}