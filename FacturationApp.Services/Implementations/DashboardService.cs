using FacturationApp.Data;
using FacturationApp.Data.Entities;
using FacturationApp.Services.Contracts;
using FacturationApp.Services.Models;
using Microsoft.EntityFrameworkCore;

namespace FacturationApp.Services.Implementations
{
    public sealed class DashboardService : IDashboardService
    {
        private readonly AppDbContext _db;

        public DashboardService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<FiscalDashboardDto> GetFiscalDashboardAsync(DateTime? dateDebut = null, DateTime? dateFin = null, CancellationToken cancellationToken = default)
        {
            var utcDateDebut = ToUtcStart(dateDebut);
            var utcDateFin = ToUtcEnd(dateFin);

            var invoices = ApplyDateFilter(_db.Factures.AsNoTracking(), utcDateDebut, utcDateFin)
                .Where(facture => facture.Statut != StatutFacture.Annulee);

            var tvaCollecteeTotale = await invoices.SumAsync(facture => facture.MontantTVA, cancellationToken);
            var montantTimbreTotal = await invoices.SumAsync(facture => facture.MontantTimbre, cancellationToken);

            var tvaParTaux = await _db.LignesFacture
                .AsNoTracking()
                .Where(ligne => ligne.Facture.Statut != StatutFacture.Annulee)
                .Where(ligne => !utcDateDebut.HasValue || ligne.Facture.DateFacture >= utcDateDebut.Value)
                .Where(ligne => !utcDateFin.HasValue || ligne.Facture.DateFacture <= utcDateFin.Value)
                .GroupBy(ligne => ligne.TauxTVA)
                .Select(group => new { TauxTVA = group.Key, MontantTVA = group.Sum(ligne => ligne.MontantTVA) })
                .OrderByDescending(item => item.MontantTVA)
                .ToListAsync(cancellationToken);

            var evolutionTVAParMois = await invoices
                .GroupBy(facture => new { facture.DateFacture.Year, facture.DateFacture.Month })
                .Select(group => new
                {
                    group.Key.Year,
                    group.Key.Month,
                    MontantTVA = group.Sum(facture => facture.MontantTVA)
                })
                .OrderBy(item => item.Year)
                .ThenBy(item => item.Month)
                .ToListAsync(cancellationToken);

            return new FiscalDashboardDto
            {
                TVACollecteeTotale = tvaCollecteeTotale,
                MontantTimbreTotal = montantTimbreTotal,
                TVAParTaux = tvaParTaux
                    .Select(item => new TvaParTauxDto(item.TauxTVA, item.MontantTVA))
                    .ToList(),
                EvolutionTVAParMois = evolutionTVAParMois
                    .Select(item => new TvaParMoisDto($"{item.Year:D4}-{item.Month:D2}", item.MontantTVA))
                    .ToList()
            };
        }

        public async Task<SalesDashboardDto> GetSalesDashboardAsync(DateTime? dateDebut = null, DateTime? dateFin = null, CancellationToken cancellationToken = default)
        {
            var utcDateDebut = ToUtcStart(dateDebut);
            var utcDateFin = ToUtcEnd(dateFin);

            var invoices = ApplyDateFilter(_db.Factures.AsNoTracking(), utcDateDebut, utcDateFin)
                .Where(facture => facture.Statut != StatutFacture.Annulee);

            var totalHt = await invoices.SumAsync(facture => facture.MontantHT, cancellationToken);
            var totalTtc = await invoices.SumAsync(facture => facture.MontantTTC, cancellationToken);

            var parPeriode = await invoices
                .GroupBy(facture => new { facture.DateFacture.Year, facture.DateFacture.Month })
                .Select(group => new
                {
                    group.Key.Year,
                    group.Key.Month,
                    ChiffreAffairesHT = group.Sum(facture => facture.MontantHT),
                    ChiffreAffairesTTC = group.Sum(facture => facture.MontantTTC)
                })
                .OrderBy(item => item.Year)
                .ThenBy(item => item.Month)
                .ToListAsync(cancellationToken);

            var ventesLignes = _db.LignesFacture
                .AsNoTracking()
                .Include(ligne => ligne.Facture)
                .Include(ligne => ligne.Produit)
                .Where(ligne => ligne.Facture.Statut != StatutFacture.Annulee)
                .Where(ligne => !utcDateDebut.HasValue || ligne.Facture.DateFacture >= utcDateDebut.Value)
                .Where(ligne => !utcDateFin.HasValue || ligne.Facture.DateFacture <= utcDateFin.Value);

            var parClient = await ventesLignes
                .GroupBy(ligne => new { ligne.Facture.ClientId, ligne.Facture.Client.Nom })
                .Select(group => new
                {
                    group.Key.ClientId,
                    group.Key.Nom,
                    ChiffreAffairesHT = group.Sum(ligne => ligne.MontantHT),
                    ChiffreAffairesTTC = group.Sum(ligne => ligne.MontantHT + ligne.MontantTVA)
                })
                .OrderByDescending(item => item.ChiffreAffairesHT)
                .ToListAsync(cancellationToken);

            var parProduit = await ventesLignes
                .GroupBy(ligne => new { ligne.ProduitId, ligne.Produit.Reference, ligne.Produit.Designation })
                .Select(group => new
                {
                    group.Key.ProduitId,
                    group.Key.Reference,
                    group.Key.Designation,
                    ChiffreAffairesHT = group.Sum(ligne => ligne.MontantHT),
                    ChiffreAffairesTTC = group.Sum(ligne => ligne.MontantHT + ligne.MontantTVA)
                })
                .OrderByDescending(item => item.ChiffreAffairesHT)
                .ToListAsync(cancellationToken);

            var repartitionStatut = await invoices
                .GroupBy(facture => facture.Statut)
                .Select(group => new
                {
                    Statut = group.Key,
                    NombreFactures = group.Count(),
                    MontantTTC = group.Sum(facture => facture.MontantTTC)
                })
                .OrderByDescending(item => item.NombreFactures)
                .ToListAsync(cancellationToken);

            var montantFacturesEmises = await invoices
                .Where(facture => facture.Statut == StatutFacture.Emise || facture.Statut == StatutFacture.Payee)
                .SumAsync(facture => facture.MontantTTC, cancellationToken);

            var montantRecouvre = await invoices
                .Where(facture => facture.Statut == StatutFacture.Payee)
                .SumAsync(facture => facture.MontantTTC, cancellationToken);

            var tauxRecouvrement = montantFacturesEmises <= 0
                ? 0m
                : decimal.Round((montantRecouvre / montantFacturesEmises) * 100m, 2);

            return new SalesDashboardDto
            {
                ChiffreAffairesTotalHT = totalHt,
                ChiffreAffairesTotalTTC = totalTtc,
                ChiffreAffairesParPeriode = parPeriode
                    .Select(item => new VenteParPeriodeDto($"{item.Year:D4}-{item.Month:D2}", item.ChiffreAffairesHT, item.ChiffreAffairesTTC))
                    .ToList(),
                ChiffreAffairesParClient = parClient
                    .Select(item => new VenteParClientDto(item.ClientId, item.Nom, item.ChiffreAffairesHT, item.ChiffreAffairesTTC))
                    .ToList(),
                ChiffreAffairesParProduit = parProduit
                    .Select(item => new VenteParProduitDto(item.ProduitId, item.Reference, item.Designation, item.ChiffreAffairesHT, item.ChiffreAffairesTTC))
                    .ToList(),
                RepartitionParStatut = repartitionStatut
                    .Select(item => new StatutFactureDto(item.Statut.ToString(), item.NombreFactures, item.MontantTTC))
                    .ToList(),
                TauxRecouvrement = tauxRecouvrement
            };
        }

        private static IQueryable<Facture> ApplyDateFilter(IQueryable<Facture> query, DateTime? dateDebut, DateTime? dateFin)
        {
            if (dateDebut.HasValue)
            {
                query = query.Where(facture => facture.DateFacture >= dateDebut.Value);
            }

            if (dateFin.HasValue)
            {
                query = query.Where(facture => facture.DateFacture <= dateFin.Value);
            }

            return query;
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