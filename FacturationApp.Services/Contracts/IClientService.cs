using FacturationApp.Data.Entities;

namespace FacturationApp.Services.Contracts
{
    public interface IClientService
    {
        Task<List<Client>> GetAllAsync(string? search = null, bool includeInactive = true, CancellationToken cancellationToken = default);
        Task<Client?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<Client> CreateAsync(Client client, CancellationToken cancellationToken = default);
        Task<Client?> UpdateAsync(Client client, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    }
}