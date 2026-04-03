using FacturationApp.Data;
using FacturationApp.Data.Entities;
using FacturationApp.Services.Contracts;
using FacturationApp.Services.Models;
using Microsoft.EntityFrameworkCore;

namespace FacturationApp.Services.Implementations
{
    public sealed class FactureService : IFactureService
    {
        private readonly AppDbContext _db;

        public FactureService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<Facture>> GetAllAsync(string? search = null, StatutFacture? statut = null, DateTime? dateDebut = null, DateTime? dateFin = null, CancellationToken cancellationToken = default)
        {
            var utcDateDebut = ToUtcStart(dateDebut);
            var utcDateFin = ToUtcEnd(dateFin);

            var query = _db.Factures
                .AsNoTracking()
                .Include(facture => facture.Client)
                .Include(facture => facture.Lignes)
                    .ThenInclude(ligne => ligne.Produit)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(facture =>
                    facture.NumeroFacture.ToLower().Contains(term) ||
                    facture.Client.Nom.ToLower().Contains(term) ||
                    (facture.Notes != null && facture.Notes.ToLower().Contains(term)));
            }

            if (statut.HasValue)
            {
                query = query.Where(facture => facture.Statut == statut.Value);
            }

            if (utcDateDebut.HasValue)
            {
                query = query.Where(facture => facture.DateFacture >= utcDateDebut.Value);
            }

            if (utcDateFin.HasValue)
            {
                query = query.Where(facture => facture.DateFacture <= utcDateFin.Value);
            }

            return await query
                .OrderByDescending(facture => facture.DateFacture)
                .ToListAsync(cancellationToken);
        }

        public async Task<Facture?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _db.Factures
                .Include(facture => facture.Client)
                .Include(facture => facture.Lignes)
                    .ThenInclude(ligne => ligne.Produit)
                .FirstOrDefaultAsync(facture => facture.Id == id, cancellationToken);
        }

        public async Task<Facture> CreateAsync(FactureCreateRequest request, CancellationToken cancellationToken = default)
        {
            if (request.Lignes.Count == 0)
            {
                throw new InvalidOperationException("Une facture doit contenir au moins une ligne.");
            }

            var client = await _db.Clients.FirstOrDefaultAsync(item => item.Id == request.ClientId && item.EstActif, cancellationToken);
            if (client is null)
            {
                throw new InvalidOperationException("Le client indiqué est introuvable ou inactif.");
            }

            var produitIds = request.Lignes.Select(ligne => ligne.ProduitId).Distinct().ToList();
            var produits = await _db.Produits
                .Where(produit => produitIds.Contains(produit.Id) && produit.EstActif)
                .ToListAsync(cancellationToken);

            if (produits.Count != produitIds.Count)
            {
                throw new InvalidOperationException("Un ou plusieurs produits sont introuvables ou inactifs.");
            }

            var dateFacture = ToUtcStart(request.DateFacture ?? DateTime.UtcNow)!.Value;
            var dateEcheance = ToUtcStart(request.DateEcheance);
            var numeroFacture = await GenerateNumeroFactureAsync(dateFacture, cancellationToken);
            var montantTimbre = await GetMontantTimbreAsync(cancellationToken);

            var facture = new Facture
            {
                NumeroFacture = numeroFacture,
                DateFacture = dateFacture,
                DateEcheance = dateEcheance,
                ClientId = client.Id,
                Notes = request.Notes,
                MontantTimbre = montantTimbre,
                Statut = StatutFacture.Brouillon,
                DateCreation = DateTime.UtcNow
            };

            foreach (var ligneRequest in request.Lignes)
            {
                var produit = produits.First(item => item.Id == ligneRequest.ProduitId);
                var montantHt = decimal.Round(ligneRequest.Quantite * produit.PrixUnitaireHT, 3);
                var montantTva = decimal.Round(montantHt * produit.TauxTVA / 100m, 3);

                facture.Lignes.Add(new LigneFacture
                {
                    ProduitId = produit.Id,
                    Quantite = ligneRequest.Quantite,
                    PrixUnitaireHT = produit.PrixUnitaireHT,
                    TauxTVA = produit.TauxTVA,
                    MontantHT = montantHt,
                    MontantTVA = montantTva,
                    DateCreation = DateTime.UtcNow
                });

                facture.MontantHT += montantHt;
                facture.MontantTVA += montantTva;
            }

            facture.MontantHT = decimal.Round(facture.MontantHT, 3);
            facture.MontantTVA = decimal.Round(facture.MontantTVA, 3);
            facture.MontantTimbre = decimal.Round(facture.MontantTimbre, 3);
            facture.MontantTTC = decimal.Round(facture.MontantHT + facture.MontantTVA + facture.MontantTimbre, 3);

            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
            _db.Factures.Add(facture);
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return facture;
        }

        public async Task<Facture?> UpdateStatusAsync(int factureId, StatutFacture statut, CancellationToken cancellationToken = default)
        {
            var facture = await _db.Factures.FirstOrDefaultAsync(item => item.Id == factureId, cancellationToken);
            if (facture is null)
            {
                return null;
            }

            facture.Statut = statut;
            facture.DateModification = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return facture;
        }

        public async Task<string> GenerateNumeroFactureAsync(DateTime? dateFacture = null, CancellationToken cancellationToken = default)
        {
            var parametre = await _db.Parametres
                .AsNoTracking()
                .OrderByDescending(item => item.DateCreation)
                .FirstOrDefaultAsync(cancellationToken);

            var prefixe = string.IsNullOrWhiteSpace(parametre?.PrefixeFacture) ? "FAC-" : parametre.PrefixeFacture;
            var date = (dateFacture ?? DateTime.UtcNow).Date;
            var baseNumero = $"{prefixe}{date:yyyyMMdd}";

            var sequence = await _db.Factures.CountAsync(facture => facture.NumeroFacture.StartsWith(baseNumero), cancellationToken) + 1;
            var numero = $"{baseNumero}-{sequence:0000}";

            while (await _db.Factures.AnyAsync(facture => facture.NumeroFacture == numero, cancellationToken))
            {
                sequence++;
                numero = $"{baseNumero}-{sequence:0000}";
            }

            return numero;
        }

        private async Task<decimal> GetMontantTimbreAsync(CancellationToken cancellationToken)
        {
            var parametre = await _db.Parametres
                .AsNoTracking()
                .OrderByDescending(item => item.DateCreation)
                .FirstOrDefaultAsync(cancellationToken);

            return parametre?.MontantTimbre ?? 1.000m;
        }

        private static DateTime? ToUtcStart(DateTime? value)
        {
            if (!value.HasValue)
            {
                return null;
            }

            var date = value.Value;
            if (date.Kind == DateTimeKind.Utc)
            {
                return date;
            }

            return DateTime.SpecifyKind(date, DateTimeKind.Utc);
        }

        private static DateTime? ToUtcEnd(DateTime? value)
        {
            var utc = ToUtcStart(value);
            return utc?.Date.AddDays(1).AddTicks(-1);
        }
    }
}