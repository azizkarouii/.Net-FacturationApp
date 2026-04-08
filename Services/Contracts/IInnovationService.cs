using FacturationApp.Services.Models;

namespace FacturationApp.Services.Contracts
{
    public interface IInnovationService
    {
        Task<List<FiscalAnomalyDto>> DetectFiscalAnomaliesAsync(DateTime? dateDebut = null, DateTime? dateFin = null, CancellationToken cancellationToken = default);
        Task<RevenueForecastDto> GetRevenueForecastAsync(int historyMonths = 6, CancellationToken cancellationToken = default);
    }
}