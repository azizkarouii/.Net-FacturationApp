using FacturationApp.Data.Entities;

namespace FacturationApp.Services.Contracts
{
    public interface IProduitService
    {
        Task<List<Produit>> GetAllAsync(string? search = null, bool includeInactive = true, CancellationToken cancellationToken = default);
        Task<Produit?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<Produit> CreateAsync(Produit produit, CancellationToken cancellationToken = default);
        Task<Produit?> UpdateAsync(Produit produit, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    }
}