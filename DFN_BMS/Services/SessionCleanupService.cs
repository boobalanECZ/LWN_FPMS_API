using DFN_BMS.DB;
using Microsoft.EntityFrameworkCore;

public class SessionCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public SessionCleanupService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            Console.WriteLine($"Loop started: {DateTime.Now}");

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var users = await db.UserMasters
                .Where(x =>
                    x.IsLoggedIn &&
                    x.LastActivity != null &&
                    x.LastActivity <= DateTime.Now.AddMinutes(-10))
                .ToListAsync();

            Console.WriteLine($"Users found: {users.Count}");

            foreach (var user in users)
            {
                Console.WriteLine($"Clearing user: {user.UserCode}, LastActivity: {user.LastActivity}");

                user.IsLoggedIn = false;
                user.SessionId = null;
                user.DeviceId = null;
                user.LoginTime = null;
                user.LastActivity = null;
            }

            if (users.Count > 0)
            {
                Console.WriteLine("Saving changes...");
                await db.SaveChangesAsync();
            }

            Console.WriteLine("Sleeping for 1 minute...");
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

}