using FacturationApp.Data;
using FacturationApp.Data.Entities;
using FacturationApp.Services.Contracts;
using FacturationApp.Services.Models;
using Microsoft.EntityFrameworkCore;

namespace FacturationApp.Services.Implementations
{
    public sealed class InnovationService : IInnovationService
    {
        private readonly AppDbContext _db;

        public InnovationService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<FiscalAnomalyDto>> DetectFiscalAnomaliesAsync(DateTime? dateDebut = null, DateTime? dateFin = null, CancellationToken cancellationToken = default)
        {
            var utcDateDebut = ToUtcStart(dateDebut);
            var utcDateFin = ToUtcEnd(dateFin);

            var invoices = await _db.Factures
                .AsNoTracking()
                .Include(facture => facture.Client)
                .Include(facture => facture.Lignes)
                    .ThenInclude(ligne => ligne.Produit)
                .Where(facture => !utcDateDebut.HasValue || facture.DateFacture >= utcDateDebut.Value)
                .Where(facture => !utcDateFin.HasValue || facture.DateFacture <= utcDateFin.Value)
                .ToListAsync(cancellationToken);

            var anomalies = new List<FiscalAnomalyDto>();

            foreach (var facture in invoices)
            {
                var totalHtLignes = facture.Lignes.Sum(ligne => ligne.MontantHT);
                var totalTvaLignes = facture.Lignes.Sum(ligne => ligne.MontantTVA);
                var montantTtcAttendu = decimal.Round(totalHtLignes + totalTvaLignes + facture.MontantTimbre, 3);

                if (Math.Abs(montantTtcAttendu - facture.MontantTTC) > 0.01m)
                {
                    anomalies.Add(new FiscalAnomalyDto(
                        "TTC_INCOHERENT",
                        "Critique",
                        $"Montant TTC incohérent pour la facture {facture.NumeroFacture}.",
                        facture.NumeroFacture,
                        facture.DateFacture));
                }

                if (facture.MontantTTC > 1000m && string.IsNullOrWhiteSpace(facture.Client.MatriculeFiscal))
                {
                    anomalies.Add(new FiscalAnomalyDto(
                        "B2B_SANS_MF",
                        "Moyen",
                        $"Le client {facture.Client.Nom} n'a pas de matricule fiscal alors que la facture {facture.NumeroFacture} dépasse 1000 TND.",
                        facture.NumeroFacture,
                        facture.DateFacture));
                }

                foreach (var ligne in facture.Lignes)
                {
                    var tvaAttendue = decimal.Round(ligne.MontantHT * (ligne.TauxTVA / 100m), 3);
                    if (Math.Abs(tvaAttendue - ligne.MontantTVA) > 0.01m)
                    {
                        anomalies.Add(new FiscalAnomalyDto(
                            "TVA_LINE_INCOHERENTE",
                            "Critique",
                            $"TVA mal calculée sur la ligne produit {ligne.Produit.Reference} de la facture {facture.NumeroFacture}.",
                            facture.NumeroFacture,
                            facture.DateFacture));
                    }
                }
            }

            var doublons = invoices
                .GroupBy(facture => new { facture.ClientId, facture.DateFacture.Date, facture.MontantTTC })
                .Where(group => group.Count() > 1)
                .ToList();

            foreach (var groupe in doublons)
            {
                anomalies.Add(new FiscalAnomalyDto(
                    "FACTURE_DOUBLON",
                    "Moyen",
                    $"{groupe.Count()} factures identiques détectées pour le client {groupe.First().Client.Nom} le {groupe.Key.Date:dd/MM/yyyy}.",
                    groupe.First().NumeroFacture,
                    groupe.Key.Date));
            }

            var sequencedGroups = invoices
                .Select(facture => new
                {
                    facture.NumeroFacture,
                    Prefix = GetPrefix(facture.NumeroFacture),
                    Sequence = GetSequence(facture.NumeroFacture)
                })
                .Where(item => item.Sequence.HasValue)
                .GroupBy(item => item.Prefix)
                .ToList();

            foreach (var group in sequencedGroups)
            {
                var existing = group.Select(item => item.Sequence!.Value).Distinct().OrderBy(value => value).ToList();
                if (existing.Count == 0)
                {
                    continue;
                }

                var min = existing.First();
                var max = existing.Last();
                for (var sequence = min; sequence <= max; sequence++)
                {
                    if (!existing.Contains(sequence))
                    {
                        anomalies.Add(new FiscalAnomalyDto(
                            "NUMEROTATION_MANQUANTE",
                            "Faible",
                            $"Numéro manquant détecté dans la séquence {group.Key}-{sequence:0000}.",
                            group.First().NumeroFacture,
                            null));
                    }
                }
            }

            return anomalies
                .GroupBy(item => new { item.Code, item.Message })
                .Select(group => group.First())
                .OrderByDescending(item => SeverityRank(item.Gravite))
                .ThenBy(item => item.NumeroFacture)
                .ToList();
        }

        public async Task<RevenueForecastDto> GetRevenueForecastAsync(int historyMonths = 6, CancellationToken cancellationToken = default)
        {
            if (historyMonths < 1)
            {
                historyMonths = 6;
            }

            var startDate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc)
                .AddMonths(-(historyMonths - 1));
            var monthlyData = await _db.Factures
                .AsNoTracking()
                .Where(facture => facture.Statut == StatutFacture.Payee)
                .Where(facture => facture.DateFacture >= startDate)
                .GroupBy(facture => new { facture.DateFacture.Year, facture.DateFacture.Month })
                .Select(group => new
                {
                    group.Key.Year,
                    group.Key.Month,
                    MontantHT = group.Sum(facture => facture.MontantHT)
                })
                .ToListAsync(cancellationToken);

            var historique = new List<RevenueForecastPointDto>();
            for (var index = 0; index < historyMonths; index++)
            {
                var month = startDate.AddMonths(index);
                var amount = monthlyData
                    .FirstOrDefault(item => item.Year == month.Year && item.Month == month.Month)
                    ?.MontantHT ?? 0m;

                historique.Add(new RevenueForecastPointDto($"{month:yyyy-MM}", amount));
            }

            var weightedValues = historique.Select((item, index) => (Value: item.MontantHT, Weight: index + 1)).ToList();
            var sumWeights = weightedValues.Sum(item => item.Weight);
            var forecast = sumWeights == 0 ? 0m : decimal.Round(weightedValues.Sum(item => item.Value * item.Weight) / sumWeights, 3);

            var lastMonth = historique.LastOrDefault()?.MontantHT ?? 0m;
            var evolution = lastMonth == 0m ? 0m : decimal.Round(((forecast - lastMonth) / lastMonth) * 100m, 2);

            return new RevenueForecastDto
            {
                Historique = historique,
                PrevisionMoisProchain = forecast,
                LabelMoisProchain = startDate.AddMonths(historyMonths).ToString("yyyy-MM"),
                DernierMois = lastMonth,
                TauxEvolution = evolution
            };
        }

        private static string GetPrefix(string numeroFacture)
        {
            var parts = numeroFacture.Split('-', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 3 ? $"{parts[0]}-{parts[1]}" : numeroFacture;
        }

        private static int? GetSequence(string numeroFacture)
        {
            var parts = numeroFacture.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 0 || !int.TryParse(parts[^1], out var sequence))
            {
                return null;
            }

            return sequence;
        }

        private static int SeverityRank(string gravite) => gravite switch
        {
            "Critique" => 3,
            "Moyen" => 2,
            "Faible" => 1,
            _ => 0
        };

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