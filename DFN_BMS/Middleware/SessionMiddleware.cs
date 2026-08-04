using DFN_BMS.DB;
using Microsoft.EntityFrameworkCore;

namespace DFN_BMS.Middleware
{
    public class SessionMiddleware
    {
        private readonly RequestDelegate _next;

        public SessionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, AppDbContext db)
        {
            var path = context.Request.Path.Value?.ToLower() ?? "";

            if (context.Request.Method == HttpMethods.Options ||
             path.StartsWith("/api/auth/login") ||
             path.StartsWith("/api/users") ||
              path.StartsWith("/api/customer") ||
             path.StartsWith("/swagger") ||
             path.StartsWith("/favicon"))
            {
                await _next(context);
                return;
            }

            var sessionId = context.Request.Headers["SessionId"].FirstOrDefault();

            Console.WriteLine($"Path : {path}");
            Console.WriteLine($"Session : {sessionId}");

            if (string.IsNullOrWhiteSpace(sessionId))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized");
                return;
            }

            var user = await db.UserMasters.FirstOrDefaultAsync(x =>
                x.SessionId != null &&
                x.SessionId.ToString() == sessionId &&
                x.IsLoggedIn);

            if (user == null)
            {
                Console.WriteLine("401 - User not found");

                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Session Expired");
                return;
            }

            Console.WriteLine($"Minutes : {DateTime.Now.Subtract(user.LastActivity.Value).TotalMinutes}");

            if (user.LastActivity.HasValue)
            {
                var elapsed = DateTime.Now.Subtract(user.LastActivity.Value).TotalMinutes;

                if (elapsed >= 10)
                {
                    user.IsLoggedIn = false;
                    user.SessionId = null;
                    user.DeviceId = null;
                    user.LoginTime = null;
                    user.LastActivity = null;
                    await db.SaveChangesAsync();
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Session Expired");
                    return;
                }
            }
            await _next(context);
        }
    }
}