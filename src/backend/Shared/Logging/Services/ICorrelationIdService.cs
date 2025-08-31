namespace Shared.Logging.Services;

public interface ICorrelationIdService
{
    string GetOrGenerateCorrelationId();
    string? GetCorrelationId();
    void SetCorrelationId(string correlationId);
}