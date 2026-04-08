using FacturationApp.Data;
using FacturationApp.Data.Entities;
using FacturationApp.Services.Contracts;
using Microsoft.EntityFrameworkCore;

namespace FacturationApp.Services.Implementations
{
    public sealed class ParametreService : IParametreService
    {
        private readonly AppDbContext _db;

        public ParametreService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<Parametre?> GetCurrentAsync(CancellationToken cancellationToken = default)
        {
            return await _db.Parametres
                .AsNoTracking()
                .OrderByDescending(item => item.DateCreation)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<Parametre> SaveAsync(Parametre parametre, CancellationToken cancellationToken = default)
        {
            var existing = await _db.Parametres
                .OrderByDescending(item => item.DateCreation)
                .FirstOrDefaultAsync(cancellationToken);

            if (existing is null)
            {
                parametre.DateCreation = DateTime.UtcNow;
                _db.Parametres.Add(parametre);
                await _db.SaveChangesAsync(cancellationToken);
                return parametre;
            }

            existing.NomSociete = parametre.NomSociete;
            existing.Adresse = parametre.Adresse;
            existing.Telephone = parametre.Telephone;
            existing.Email = parametre.Email;
            existing.MatriculeFiscal = parametre.MatriculeFiscal;
            existing.MontantTimbre = parametre.MontantTimbre;
            existing.PrefixeFacture = parametre.PrefixeFacture;
            existing.MentionsLegales = parametre.MentionsLegales;
            existing.DateModification = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);
            return existing;
        }
    }
}