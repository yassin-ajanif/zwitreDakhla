namespace GestionCommerciale.Shared.Services;

public interface IAppErrorLogger
{
    void Log(Exception exception, string? context = null);
}
