using DFN_BMS.DB;
using DFN_BMS.Models;
using DFN_BMS.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Mail;

[Route("api/[controller]")]
[ApiController]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly EncryptionService _enc;
    private readonly IConfiguration _configuration;
    private readonly string _loginUrl;

    public UsersController(AppDbContext context, EncryptionService enc, IConfiguration configuration)
    {
        _context = context;
        _enc = enc;
        _configuration = configuration;
        _loginUrl = _configuration["AppSettings:FrontendUrl"];
    }



    [HttpGet("departments")]
    public async Task<IActionResult> GetDepartments()
    {
        var data = await _context.DepartmentMasters
            .Where(x => x.IsActive == true)
            .Select(x => new
            {
                value = x.Id,
                label = x.DepName
            })
            .OrderBy(x => x.label)
            .ToListAsync();

        return Ok(data);
    }


    // ✅ GET ALL

    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var users = await (
            from u in _context.UserMasters
            join d in _context.DepartmentMasters
                on u.DepartmentId equals d.Id
            where u.IsActive
            select new
            {
                id = u.Id,
                userId = u.UserCode,
                userName = u.UserName,
                employeeId = u.EmployeeId,
                departmentId = u.DepartmentId,
                departmentName = d.DepName
            }
        ).ToListAsync();

        return Ok(users);
    }
    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(int id)
    {
        var user = await _context.UserMasters
            .Where(x => x.Id == id && x.IsActive)
            .Select(x => new
            {
                x.Id,
                userId = x.UserCode,
                x.UserName,
                x.EmployeeId,
                x.DepartmentId,
                PasswordHash = x.PasswordHash
            })
            .FirstOrDefaultAsync();

        if (user == null)
            return NotFound("User not found");

        return Ok(new
        {
            user.Id,
            user.userId,
            user.UserName,
            user.EmployeeId,
            user.DepartmentId,
            Password = _enc.Decrypt(user.PasswordHash)
        });
    }

    // Shared helper: the frontend's Department dropdown (CreatableSelect)
    // always sends DepartmentName (uppercased), whether the person picked
    // an existing department or typed a brand new one. This resolves that
    // name to a DepartmentId, creating the department if it doesn't exist
    // yet — used by both Create and Update so "add new department" works
    // from the edit form too, not just the add form.
    private async Task<IActionResult?> ResolveDepartmentAsync(UserMaster user)
    {
        if (!string.IsNullOrWhiteSpace(user.DepartmentName))
        {
            var deptName = user.DepartmentName.Trim().ToUpper();

            var dept = await _context.DepartmentMasters
                .FirstOrDefaultAsync(x => x.DepName.ToUpper() == deptName);

            if (dept == null)
            {
                dept = new DepartmentMaster
                {
                    DepName = deptName,
                    IsActive = true
                };

                _context.DepartmentMasters.Add(dept);
                await _context.SaveChangesAsync();
            }

            user.DepartmentId = dept.Id;
            return null;
        }

        bool deptExists = await _context.DepartmentMasters
            .AnyAsync(x => x.Id == user.DepartmentId && x.IsActive);

        if (!deptExists)
            return BadRequest("Invalid Department");

        return null;
    }

    [HttpPost]
    public async Task<IActionResult> Create(UserMaster user)
    {
        try
        {
            // ================= VALIDATION =================

            if (string.IsNullOrWhiteSpace(user.UserCode))
                return BadRequest("User Code required");

            if (string.IsNullOrWhiteSpace(user.EmployeeId))
                return BadRequest("Employee ID required");

            if (string.IsNullOrWhiteSpace(user.UserName))
                return BadRequest("User Name required");

            if (string.IsNullOrWhiteSpace(user.PasswordHash))
                return BadRequest("Password required");

            // ================= DUPLICATE CHECK =================

            bool userExists = await _context.UserMasters
                .AnyAsync(x =>
                    x.UserCode.ToLower() == user.UserCode.ToLower()
                    && x.IsActive);

            if (userExists)
                return BadRequest("User Code already exists");

            bool employeeExists = await _context.UserMasters
                .AnyAsync(x =>
                    x.EmployeeId.ToLower() == user.EmployeeId.ToLower()
                    && x.IsActive);

            if (employeeExists)
                return BadRequest("Employee ID already exists");

            // ================= DEPARTMENT =================

            var deptResult = await ResolveDepartmentAsync(user);
            if (deptResult != null)
                return deptResult;

            // ================= PASSWORD =================

            user.PasswordHash = _enc.Encrypt(user.PasswordHash);

            // ================= DEFAULT VALUES =================

            user.IsActive = true;
            user.CreatedBy = 1;
            user.CreatedOn = DateTime.Now;

            // ================= SAVE USER =================

            _context.UserMasters.Add(user);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "User Saved Successfully",
                id = user.Id
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                message = ex.Message
            });
        }
    }
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id,UserMaster user)
    {
        try
        {
            var existing = await _context.UserMasters
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.IsActive);

            if (existing == null)
                return NotFound("User not found");

            bool duplicateUser = await _context.UserMasters
                .AnyAsync(x =>
                    x.UserCode == user.UserCode &&
                    x.Id != id &&
                    x.IsActive);

            if (duplicateUser)
                return BadRequest("User Code already exists");

            bool duplicateEmployee = await _context.UserMasters
                .AnyAsync(x =>
                    x.EmployeeId == user.EmployeeId &&
                    x.Id != id &&
                    x.IsActive);

            if (duplicateEmployee)
                return BadRequest("Employee ID already exists");

            // ================= DEPARTMENT =================
            // Same resolution as Create: the edit form's CreatableSelect
            // can also send a brand-new DepartmentName, not just an
            // existing DepartmentId.
            var deptResult = await ResolveDepartmentAsync(user);
            if (deptResult != null)
                return deptResult;

            existing.UserCode = user.UserCode;
            existing.UserName = user.UserName;
            existing.EmployeeId = user.EmployeeId;
            existing.DepartmentId = user.DepartmentId;

            if (string.IsNullOrWhiteSpace(user.PasswordHash))
                return BadRequest("Password is required");

            existing.PasswordHash = _enc.Encrypt(user.PasswordHash);

            existing.ModifiedOn = DateTime.Now;
            existing.ModifiedBy = 1;

            await _context.SaveChangesAsync();

            return Ok(existing);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
    // ✅ DELETE (SOFT DELETE)
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _context.UserMasters
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.IsActive);

        if (user == null)
            return NotFound();

        user.IsActive = false;
        user.ModifiedOn = DateTime.Now;
        user.ModifiedBy = 1;

        await _context.SaveChangesAsync();

        return Ok("Deleted Successfully");
    }

    [HttpGet("privileges/{userId}")]
    public async Task<IActionResult> GetPrivileges(int userId)
    {
        var data = await _context.UserPrivileges
            .Where(x => x.UserId == userId)
            .ToListAsync();

        return Ok(data);
    }

    [HttpPost("privileges")]
    public async Task<IActionResult> SavePrivileges(List<UserPrivilege> model)
    {
        if (model == null || model.Count == 0)
            return BadRequest("No data");

        var userId = model.First().UserId;

        // REMOVE OLD
        var old = _context.UserPrivileges.Where(x => x.UserId == userId);
        _context.UserPrivileges.RemoveRange(old);

        await _context.UserPrivileges.AddRangeAsync(model);
        await _context.SaveChangesAsync();

        return Ok("Saved");
    }

}
