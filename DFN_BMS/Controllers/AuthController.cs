using DFN_BMS.DB;
using DFN_BMS.Models;
using DFN_BMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DFN_BMS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly EncryptionService _enc;

        public AuthController(AppDbContext context, EncryptionService enc)
        {
            _context = context;
            _enc = enc;
        }


        [HttpPost("heartbeat")]
        public async Task<IActionResult> Heartbeat()
        {
            var sessionId = Request.Headers["SessionId"].FirstOrDefault();

            if (string.IsNullOrEmpty(sessionId))
                return Unauthorized();

            var user = await _context.UserMasters.FirstOrDefaultAsync(x =>
                x.SessionId.ToString() == sessionId &&
                x.IsLoggedIn);

            if (user == null)
                return Unauthorized();

            user.LastActivity = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok();
        }


        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var sessionId = Request.Headers["SessionId"].FirstOrDefault();

            var user = await _context.UserMasters
                .FirstOrDefaultAsync(x => x.SessionId.ToString() == sessionId);

            if (user != null)
            {
                user.IsLoggedIn = false;
                user.SessionId = null;
                user.DeviceId = null;
                user.LastActivity = null;

                await _context.SaveChangesAsync();
            }

            return Ok();
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Username))
                {
                    return BadRequest(new
                    {
                        message = "Username is required"
                    });
                }

                if (string.IsNullOrWhiteSpace(request.Password))
                {
                    return BadRequest(new
                    {
                        message = "Password is required"
                    });
                }

                string loginText = request.Username.Trim().ToLower();

               
                var user = await _context.UserMasters
                    .FirstOrDefaultAsync(u =>
                        u.IsActive &&
                        (
                            u.UserCode.ToLower() == loginText ||
                            u.EmployeeId.ToLower() == loginText ||
                            u.UserName.ToLower() == loginText
                        ));

                if (user == null)
                {
                    return Unauthorized(new
                    {
                        message = "Invalid User ID / Employee ID / User Name"
                    });
                }

                string decryptPassword = _enc.Decrypt(user.PasswordHash);

                if (decryptPassword != request.Password)
                {
                    return Unauthorized(new
                    {
                        message = "Invalid Password"
                    });
                }

            
                var department = await _context.DepartmentMasters
                    .FirstOrDefaultAsync(x => x.Id == user.DepartmentId);

                if (!user.IsLoggedIn)
                {
                    user.SessionId = Guid.NewGuid();
                }
                else if (!string.Equals(user.DeviceId, request.DeviceId, StringComparison.OrdinalIgnoreCase))
                {
                    return Ok(new
                    {
                        alreadyLoggedIn = true,
                        message = $"User '{user.UserName}' is already logged in from another device."
                    });
                }

                // Same device -> keep existing SessionId
                user.DeviceId = request.DeviceId;
                user.IsLoggedIn = true;
                user.LoginTime = DateTime.Now;
                user.LastActivity = DateTime.Now;

                await _context.SaveChangesAsync();
              

                return Ok(new
                {
                    message = "Login Success",
                    sessionId = user.SessionId,
                    user = new
                    {
                        user.Id,
                        UserId = user.UserCode,
                        user.EmployeeId,
                        user.UserName,
                        user.DepartmentId,
                        DepartmentName = department?.DepName,
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = ex.InnerException?.Message ?? ex.Message
                });
            }
        }
    }
}