using FacturationApp.Services.Models;

namespace FacturationApp.Services.Contracts
{
    public interface IDashboardService
    {
        Task<FiscalDashboardDto> GetFiscalDashboardAsync(DateTime? dateDebut = null, DateTime? dateFin = null, CancellationToken cancellationToken = default);
        Task<SalesDashboardDto> GetSalesDashboardAsync(DateTime? dateDebut = null, DateTime? dateFin = null, CancellationToken cancellationToken = default);
    }
}