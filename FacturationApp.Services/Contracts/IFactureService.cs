using FacturationApp.Data.Entities;
using FacturationApp.Services.Models;

namespace FacturationApp.Services.Contracts
{
    public interface IFactureService
    {
        Task<List<Facture>> GetAllAsync(string? search = null, StatutFacture? statut = null, DateTime? dateDebut = null, DateTime? dateFin = null, CancellationToken cancellationToken = default);
        Task<Facture?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<Facture> CreateAsync(FactureCreateRequest request, CancellationToken cancellationToken = default);
        Task<Facture?> UpdateStatusAsync(int factureId, StatutFacture statut, CancellationToken cancellationToken = default);
        Task<string> GenerateNumeroFactureAsync(DateTime? dateFacture = null, CancellationToken cancellationToken = default);
    }
}