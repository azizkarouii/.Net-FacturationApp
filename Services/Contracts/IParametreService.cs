using FacturationApp.Data.Entities;

namespace FacturationApp.Services.Contracts
{
    public interface IParametreService
    {
        Task<Parametre?> GetCurrentAsync(CancellationToken cancellationToken = default);
        Task<Parametre> SaveAsync(Parametre parametre, CancellationToken cancellationToken = default);
    }
}