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
    public class ItemMasterController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ItemMasterController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/ItemMaster/item-types
        // Feeds the frontend's Item Type CreatableSelect — same shape as
        // UsersController's GET /users/departments.
        [HttpGet("item-types")]
        public async Task<IActionResult> GetItemTypes()
        {
            var data = await _context.ItemTypeMasters
                .Where(x => x.IsActive)
                .Select(x => new
                {
                    value = x.Id,
                    label = x.TypeName
                })
                .OrderBy(x => x.label)
                .ToListAsync();

            return Ok(data);
        }

        // GET: api/ItemMaster/uom-list
        // Feeds the frontend's UOM CreatableSelect — same shape/pattern as
        // item-types above. Note ItemMaster.Uom itself stays a plain
        // string column (no FK), this table just powers the dropdown and
        // keeps values consistent.
        [HttpGet("uom-list")]
        public async Task<IActionResult> GetUomList()
        {
            var data = await _context.UomMasters
                .Where(x => x.IsActive)
                .Select(x => new
                {
                    value = x.UomName,
                    label = x.UomName
                })
                .OrderBy(x => x.label)
                .ToListAsync();

            return Ok(data);
        }

        // GET: api/ItemMaster
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _context.ItemMasters
                .Include(x => x.ItemGroup)
                .Include(x => x.ItemType)
                .OrderByDescending(x => x.Id)
                .Select(x => new
                {
                    x.Id,
                    x.ItemNumber,
                    x.ItemName,
                    x.ItemTypeId,
                    ItemTypeName = x.ItemType.TypeName,
                    x.ItemGroupId,
                    ItemGroupName = x.ItemGroup.GroupName,
                    x.HsnCode,
                    x.UnitPrice,
                    x.Uom,
                    x.WeightPerUnit,
                    x.StuffQuantity,
                    x.ItemModel,
                    x.Usage,
                    x.Length,
                    x.Width,
                    x.Height,
                    x.Description,
                    x.SafetyLevel,
                    x.ReorderLevel,
                    x.DangerLevel
                })
                .ToListAsync();

            return Ok(list);
        }

        // GET: api/ItemMaster/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _context.ItemMasters
                .Include(x => x.ItemType)
                .Where(x => x.Id == id)
                .Select(x => new
                {
                    x.Id,
                    x.ItemNumber,
                    x.ItemName,
                    x.ItemTypeId,
                    ItemTypeName = x.ItemType.TypeName,
                    x.ItemGroupId,
                    x.HsnCode,
                    x.UnitPrice,
                    x.Uom,
                    x.WeightPerUnit,
                    x.StuffQuantity,
                    x.ItemModel,
                    x.Usage,
                    x.Length,
                    x.Width,
                    x.Height,
                    x.Description,
                    x.SafetyLevel,
                    x.ReorderLevel,
                    x.DangerLevel
                })
                .FirstOrDefaultAsync();

            if (item == null)
                return NotFound(new { message = "Item not found" });

            return Ok(item);
        }

        // Shared helper: the frontend's Item Type dropdown (CreatableSelect)
        // always sends ItemTypeName (uppercased), whether the person picked
        // an existing type or typed a brand new one. This resolves that
        // name to an ItemTypeId, creating the type if it doesn't exist yet —
        // used by both Create and Update, same pattern as
        // UsersController.ResolveDepartmentAsync.
        private async Task<IActionResult> ResolveItemTypeAsync(ItemMaster item)
        {
            if (!string.IsNullOrWhiteSpace(item.ItemTypeName))
            {
                var typeName = item.ItemTypeName.Trim().ToUpper();

                var type = await _context.ItemTypeMasters
                    .FirstOrDefaultAsync(x => x.TypeName.ToUpper() == typeName);

                if (type == null)
                {
                    type = new ItemTypeMaster
                    {
                        TypeName = typeName,
                        IsActive = true
                    };

                    _context.ItemTypeMasters.Add(type);
                    await _context.SaveChangesAsync();
                }

                item.ItemTypeId = type.Id;
                return null;
            }

            bool typeExists = await _context.ItemTypeMasters
                .AnyAsync(x => x.Id == item.ItemTypeId && x.IsActive);

            if (!typeExists)
                return BadRequest(new { message = "Invalid Item Type" });

            return null;
        }

        // Companion helper for Uom — since ItemMaster.Uom is a plain
        // string column (not a FK), this just makes sure whatever the
        // person picked/typed in the CreatableSelect exists in
        // UOM_MASTER for future dropdowns, then normalizes the value
        // that actually gets saved on the item (uppercased, trimmed).
        private async Task RegisterUomIfNewAsync(ItemMaster item)
        {
            if (string.IsNullOrWhiteSpace(item.Uom))
                return;

            var uomName = item.Uom.Trim().ToUpper();

            var exists = await _context.UomMasters
                .AnyAsync(x => x.UomName.ToUpper() == uomName);

            if (!exists)
            {
                _context.UomMasters.Add(new UomMaster
                {
                    UomName = uomName,
                    IsActive = true
                });

                await _context.SaveChangesAsync();
            }

            item.Uom = uomName;
        }

        // POST: api/ItemMaster
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ItemMaster model)
        {
            if (string.IsNullOrWhiteSpace(model.ItemNumber) ||
                string.IsNullOrWhiteSpace(model.ItemName) ||
                string.IsNullOrWhiteSpace(model.Uom) ||
                model.ItemGroupId <= 0)
            {
                return BadRequest(new { message = "Item Number, Item Name, Item Group and UOM are required" });
            }

            var groupExists = await _context.ItemGroupMasters.AnyAsync(g => g.Id == model.ItemGroupId);
            if (!groupExists)
                return BadRequest(new { message = "Selected Item Group does not exist" });

            var numberExists = await _context.ItemMasters
                .AnyAsync(x => x.ItemNumber.ToLower() == model.ItemNumber.Trim().ToLower());

            if (numberExists)
                return BadRequest(new { message = "Item Number already exists" });

            var nameExists = await _context.ItemMasters
                .AnyAsync(x => x.ItemName.ToLower() == model.ItemName.Trim().ToLower());

            if (nameExists)
                return BadRequest(new { message = "Item Name already exists" });

            var typeResult = await ResolveItemTypeAsync(model);
            if (typeResult != null)
                return typeResult;

            await RegisterUomIfNewAsync(model);

            var entity = new ItemMaster
            {
                ItemNumber = model.ItemNumber.Trim(),
                ItemName = model.ItemName.Trim(),
                ItemTypeId = model.ItemTypeId,
                ItemGroupId = model.ItemGroupId,
                HsnCode = model.HsnCode?.Trim(),
                UnitPrice = model.UnitPrice,
                Uom = model.Uom.Trim(),
                WeightPerUnit = model.WeightPerUnit,
                StuffQuantity = model.StuffQuantity,
                ItemModel = model.ItemModel?.Trim(),
                Usage = model.Usage?.Trim(),
                Length = model.Length,
                Width = model.Width,
                Height = model.Height,
                Description = model.Description?.Trim(),
                SafetyLevel = model.SafetyLevel,
                ReorderLevel = model.ReorderLevel,
                DangerLevel = model.DangerLevel?.Trim(),
                CreatedDate = DateTime.Now
            };

            _context.ItemMasters.Add(entity);
            await _context.SaveChangesAsync();

            return Ok(entity);
        }

        // PUT: api/ItemMaster/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ItemMaster model)
        {
            var entity = await _context.ItemMasters.FindAsync(id);

            if (entity == null)
                return NotFound(new { message = "Item not found" });

            if (string.IsNullOrWhiteSpace(model.ItemName) ||
                string.IsNullOrWhiteSpace(model.Uom) ||
                model.ItemGroupId <= 0)
            {
                return BadRequest(new { message = "Item Name, Item Group and UOM are required" });
            }

            var groupExists = await _context.ItemGroupMasters.AnyAsync(g => g.Id == model.ItemGroupId);
            if (!groupExists)
                return BadRequest(new { message = "Selected Item Group does not exist" });

            var nameExists = await _context.ItemMasters
                .AnyAsync(x => x.ItemName.ToLower() == model.ItemName.Trim().ToLower() && x.Id != id);

            if (nameExists)
                return BadRequest(new { message = "Item Name already exists" });

            var typeResult = await ResolveItemTypeAsync(model);
            if (typeResult != null)
                return typeResult;

            await RegisterUomIfNewAsync(model);

            entity.ItemName = model.ItemName.Trim();
            entity.ItemTypeId = model.ItemTypeId;
            entity.ItemGroupId = model.ItemGroupId;
            entity.HsnCode = model.HsnCode?.Trim();
            entity.UnitPrice = model.UnitPrice;
            entity.Uom = model.Uom.Trim();
            entity.WeightPerUnit = model.WeightPerUnit;
            entity.StuffQuantity = model.StuffQuantity;
            entity.ItemModel = model.ItemModel?.Trim();
            entity.Usage = model.Usage?.Trim();
            entity.Length = model.Length;
            entity.Width = model.Width;
            entity.Height = model.Height;
            entity.Description = model.Description?.Trim();
            entity.SafetyLevel = model.SafetyLevel;
            entity.ReorderLevel = model.ReorderLevel;
            entity.DangerLevel = model.DangerLevel?.Trim();
            entity.ModifiedDate = DateTime.Now;
            // Note: ItemNumber is intentionally never changed on update.

            await _context.SaveChangesAsync();

            return Ok(entity);
        }

        // DELETE: api/ItemMaster/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _context.ItemMasters.FindAsync(id);

            if (entity == null)
                return NotFound(new { message = "Item not found" });

            _context.ItemMasters.Remove(entity);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Deleted Successfully" });
        }
    }
}