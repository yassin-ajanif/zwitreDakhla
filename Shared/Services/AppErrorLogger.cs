using System.Text;
using GestionCommerciale.Shared.Database;

namespace GestionCommerciale.Shared.Services;

public sealed class AppErrorLogger : IAppErrorLogger
{
    public const string LogFileName = "logError.txt";
    private const long MaxBytes = 5 * 1024 * 1024;

    private static readonly object Lock = new();

    public void Log(Exception exception, string? context = null)
    {
        if (exception is null)
            return;

        try
        {
            var entry = FormatEntry(exception, context);
            var path = Path.Combine(DatabasePath.GetDirectory(), LogFileName);

            lock (Lock)
            {
                if (File.Exists(path) && new FileInfo(path).Length >= MaxBytes)
                    File.Delete(path);

                File.AppendAllText(path, entry + Environment.NewLine + Environment.NewLine, Encoding.UTF8);
            }
        }
        catch
        {
            // Never crash the app because logging failed.
        }
    }

    private static string FormatEntry(Exception exception, string? context)
    {
        var sb = new StringBuilder();
        sb.Append('[').Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")).Append("] ");
        sb.Append(exception.GetType().Name).Append(" | ");

        if (!string.IsNullOrWhiteSpace(context))
            sb.Append('[').Append(context).Append("] ");

        sb.AppendLine(exception.Message);

        if (!string.IsNullOrWhiteSpace(exception.StackTrace))
            sb.AppendLine(exception.StackTrace);

        if (exception.InnerException is { } inner)
        {
            sb.AppendLine("--- Inner exception ---");
            sb.Append(inner.GetType().Name).Append(" | ").AppendLine(inner.Message);
            if (!string.IsNullOrWhiteSpace(inner.StackTrace))
                sb.AppendLine(inner.StackTrace);
        }

        return sb.ToString().TrimEnd();
    }
}
