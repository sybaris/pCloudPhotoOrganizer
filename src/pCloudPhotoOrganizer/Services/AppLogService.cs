using System.Text;
using System.Threading;
using Microsoft.Maui.Storage;

namespace pCloudPhotoOrganizer.Services;

public class AppLogService
{
    private const string LogFolderName = "Logs";
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private static string LogDirectory => Path.Combine(FileSystem.AppDataDirectory, LogFolderName);

    private static string BuildLogFilePath(DateTimeOffset date)
        => Path.Combine(LogDirectory, $"{date:yyyyMMdd}.log");

    private string CurrentLogFilePath => BuildLogFilePath(DateTimeOffset.Now);

    public Task LogInfo(string message) => WriteAsync("INFO", message);

    public Task LogOperation(string message) => WriteAsync("OP", message);

    public Task LogError(Exception exception, string? context = null)
    {
        var builder = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(context))
        {
            builder.AppendLine(context);
        }

        builder.AppendLine(exception.GetType().Name + ": " + exception.Message);
        if (!string.IsNullOrWhiteSpace(exception.StackTrace))
        {
            builder.AppendLine(exception.StackTrace);
        }

        if (exception.InnerException is not null)
        {
            builder.AppendLine("Inner: " + exception.InnerException);
        }

        return WriteAsync("ERROR", builder.ToString());
    }

    public async Task<string> GetCurrentLogContentAsync()
    {
        try
        {
            var path = CurrentLogFilePath;
            if (!File.Exists(path))
            {
                return "Aucun log pour aujourd'hui.";
            }

            using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync().ConfigureAwait(false);
        }
        catch
        {
            return "Impossible de lire le journal.";
        }
    }

    public async Task ClearCurrentLogAsync()
    {
        var path = CurrentLogFilePath;
        var entered = false;
        try
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);
            entered = true;
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Ignored
        }
        finally
        {
            if (entered)
            {
                _semaphore.Release();
            }
        }
    }

    private async Task WriteAsync(string level, string message)
    {
        var timestamp = DateTimeOffset.Now.ToString("O");
        var entry = $"{timestamp} [{level}] {message}" + Environment.NewLine;

        var entered = false;
        try
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);
            entered = true;
            Directory.CreateDirectory(LogDirectory);
            await File.AppendAllTextAsync(CurrentLogFilePath, entry).ConfigureAwait(false);
        }
        catch
        {
            // Logging must never crash the app.
        }
        finally
        {
            if (entered)
            {
                _semaphore.Release();
            }
        }
    }
}
