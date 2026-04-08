using FacturationApp.Data;
using FacturationApp.Data.Entities;
using FacturationApp.Services.Contracts;
using Microsoft.EntityFrameworkCore;

namespace FacturationApp.Services.Implementations
{
    public sealed class ClientService : IClientService
    {
        private readonly AppDbContext _db;

        public ClientService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<Client>> GetAllAsync(string? search = null, bool includeInactive = true, CancellationToken cancellationToken = default)
        {
            var query = _db.Clients.AsNoTracking().AsQueryable();

            if (!includeInactive)
            {
                query = query.Where(client => client.EstActif);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(client =>
                    client.Nom.ToLower().Contains(term) ||
                    (client.Email != null && client.Email.ToLower().Contains(term)) ||
                    (client.Telephone != null && client.Telephone.ToLower().Contains(term)) ||
                    (client.MatriculeFiscal != null && client.MatriculeFiscal.ToLower().Contains(term)));
            }

            return await query
                .OrderBy(client => client.Nom)
                .ToListAsync(cancellationToken);
        }

        public async Task<Client?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _db.Clients
                .Include(client => client.Factures)
                .FirstOrDefaultAsync(client => client.Id == id, cancellationToken);
        }

        public async Task<Client> CreateAsync(Client client, CancellationToken cancellationToken = default)
        {
            client.DateCreation = DateTime.UtcNow;
            client.DateModification = null;

            _db.Clients.Add(client);
            await _db.SaveChangesAsync(cancellationToken);
            return client;
        }

        public async Task<Client?> UpdateAsync(Client client, CancellationToken cancellationToken = default)
        {
            var existingClient = await _db.Clients.FirstOrDefaultAsync(item => item.Id == client.Id, cancellationToken);
            if (existingClient is null)
            {
                return null;
            }

            existingClient.Nom = client.Nom;
            existingClient.Adresse = client.Adresse;
            existingClient.Telephone = client.Telephone;
            existingClient.Email = client.Email;
            existingClient.MatriculeFiscal = client.MatriculeFiscal;
            existingClient.EstActif = client.EstActif;
            existingClient.DateModification = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);
            return existingClient;
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var client = await _db.Clients.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
            if (client is null)
            {
                return false;
            }

            client.EstActif = false;
            client.DateModification = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}