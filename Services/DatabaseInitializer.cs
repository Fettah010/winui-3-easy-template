using System;
using System.Threading.Tasks;

namespace DevTemWinUi3.Services;

/// <summary>
/// Deferred database init (best-effort, idempotent). Split from
/// <see cref="BackgroundUpdateService"/> so the template engine can
/// exclude each feature surface per flag.
/// </summary>
public static class DatabaseInitializer
{
    public static async Task InitializeAsync()
    {
        try
        {
            var db = ServiceLocator.GetRequiredService<DatabaseService>();
            await db.InitializeAsync();
        }
        catch (Exception ex)
        {
            // DatabaseService.InitializeAsync is idempotent; a page that needs
            // the DB earlier triggers init itself, so this is best-effort.
            LoggingService.Log.Error(ex, "Deferred database init failed");
        }
    }
}
