using FacturationApp.Data;
using FacturationApp.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace FacturationApp.Web.Data;

public static class AppDataSeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.MigrateAsync(cancellationToken);
        var now = DateTime.UtcNow;

        if (!await db.Factures.AnyAsync(cancellationToken))
        {
            var parametre = await db.Parametres.FirstOrDefaultAsync(cancellationToken);
            if (parametre is null)
            {
                db.Parametres.Add(new Parametre
                {
                    NomSociete = "FacturationApp SARL",
                    Adresse = "Lac 2, Tunis, Tunisie",
                    Telephone = "+216 71 000 111",
                    Email = "contact@facturationapp.tn",
                    MatriculeFiscal = "1897654/A/M/000",
                    MontantTimbre = 1.000m,
                    PrefixeFacture = "FAC-2026",
                    MentionsLegales = "Facture conforme aux obligations fiscales tunisiennes."
                });
            }

            var clients = new List<Client>
            {
                new()
                {
                    Nom = "Société El Manar Distribution",
                    Adresse = "Rue de l'Industrie, Ariana",
                    Telephone = "+216 22 111 222",
                    Email = "achat@elmanar.tn",
                    MatriculeFiscal = "1267890/B/N/000",
                    EstActif = true
                },
                new()
                {
                    Nom = "Clinique Ennour",
                    Adresse = "Avenue Habib Bourguiba, Sfax",
                    Telephone = "+216 74 333 444",
                    Email = "finance@ennour.tn",
                    MatriculeFiscal = "2233445/C/P/000",
                    EstActif = true
                },
                new()
                {
                    Nom = "Cafeteria Jasmin",
                    Adresse = "Centre Ville, Nabeul",
                    Telephone = "+216 72 555 666",
                    Email = "gestion@jasmin.tn",
                    MatriculeFiscal = "3344556/D/S/000",
                    EstActif = true
                },
                new()
                {
                    Nom = "Client Particulier",
                    Adresse = "Tunis",
                    Telephone = "+216 98 777 888",
                    Email = "client.particulier@example.com",
                    MatriculeFiscal = null,
                    EstActif = true
                }
            };

            var produits = new List<Produit>
            {
                new()
                {
                    Reference = "PRD-BUR-001",
                    Designation = "Pack Fournitures Bureau",
                    PrixUnitaireHT = 120.000m,
                    TauxTVA = 19m,
                    Unite = "pack",
                    EstActif = true
                },
                new()
                {
                    Reference = "PRD-SRV-001",
                    Designation = "Service Maintenance Informatique",
                    PrixUnitaireHT = 250.000m,
                    TauxTVA = 19m,
                    Unite = "heure",
                    EstActif = true
                },
                new()
                {
                    Reference = "PRD-MED-001",
                    Designation = "Consommable Medical",
                    PrixUnitaireHT = 45.000m,
                    TauxTVA = 7m,
                    Unite = "boite",
                    EstActif = true
                },
                new()
                {
                    Reference = "PRD-ALM-001",
                    Designation = "Produits Alimentaires",
                    PrixUnitaireHT = 30.000m,
                    TauxTVA = 13m,
                    Unite = "carton",
                    EstActif = true
                },
                new()
                {
                    Reference = "PRD-CNS-001",
                    Designation = "Conseil Fiscal",
                    PrixUnitaireHT = 300.000m,
                    TauxTVA = 19m,
                    Unite = "session",
                    EstActif = true
                }
            };

            db.Clients.AddRange(clients);
            db.Produits.AddRange(produits);
            await db.SaveChangesAsync(cancellationToken);

            var timbre = 1.000m;

            var factures = new List<Facture>
            {
                CreateFacture(
                    numeroFacture: "FAC-2026-0001",
                    client: clients[0],
                    dateFactureUtc: StartOfMonth(now).AddDays(2),
                    statut: StatutFacture.Payee,
                    notes: "Paiement par virement",
                    timbre: timbre,
                    lignes:
                    [
                        CreateLigne(produits[0], 3m),
                        CreateLigne(produits[1], 2m)
                    ]),

                CreateFacture(
                    numeroFacture: "FAC-2026-0002",
                    client: clients[1],
                    dateFactureUtc: StartOfMonth(now).AddMonths(-1).AddDays(8),
                    statut: StatutFacture.Payee,
                    notes: "Règlement comptant",
                    timbre: timbre,
                    lignes:
                    [
                        CreateLigne(produits[2], 10m),
                        CreateLigne(produits[4], 1m)
                    ]),

                CreateFacture(
                    numeroFacture: "FAC-2026-0003",
                    client: clients[2],
                    dateFactureUtc: StartOfMonth(now).AddMonths(-2).AddDays(5),
                    statut: StatutFacture.Emise,
                    notes: "Paiement sous 30 jours",
                    timbre: timbre,
                    lignes:
                    [
                        CreateLigne(produits[3], 12m),
                        CreateLigne(produits[0], 1m)
                    ]),

                CreateFacture(
                    numeroFacture: "FAC-2026-0004",
                    client: clients[3],
                    dateFactureUtc: StartOfMonth(now).AddMonths(-3).AddDays(10),
                    statut: StatutFacture.Payee,
                    notes: "Vente au detail",
                    timbre: timbre,
                    lignes:
                    [
                        CreateLigne(produits[0], 1m),
                        CreateLigne(produits[3], 2m)
                    ]),

                CreateFacture(
                    numeroFacture: "FAC-2026-0005",
                    client: clients[0],
                    dateFactureUtc: StartOfMonth(now).AddMonths(-1).AddDays(19),
                    statut: StatutFacture.Annulee,
                    notes: "Facture annulee suite a retour",
                    timbre: timbre,
                    lignes:
                    [
                        CreateLigne(produits[1], 1m)
                    ])
            };

            db.Factures.AddRange(factures);
            await db.SaveChangesAsync(cancellationToken);
        }

        await EnsureDemoAnomalyAsync(db, now, cancellationToken);
    }

    private static async Task EnsureDemoAnomalyAsync(AppDbContext db, DateTime now, CancellationToken cancellationToken)
    {
        const string anomalyNumber = "FAC-2026-0999";
        if (await db.Factures.AnyAsync(item => item.NumeroFacture == anomalyNumber, cancellationToken))
        {
            return;
        }

        var clientSansMf = await db.Clients
            .FirstOrDefaultAsync(item => string.IsNullOrWhiteSpace(item.MatriculeFiscal), cancellationToken);

        if (clientSansMf is null)
        {
            clientSansMf = new Client
            {
                Nom = "Client Anomalie Démo",
                Adresse = "Tunis",
                Telephone = "+216 20 000 999",
                Email = "anomalie.demo@example.com",
                MatriculeFiscal = null,
                EstActif = true
            };

            db.Clients.Add(clientSansMf);
            await db.SaveChangesAsync(cancellationToken);
        }

        var produit = await db.Produits
            .OrderByDescending(item => item.PrixUnitaireHT)
            .FirstOrDefaultAsync(cancellationToken);

        if (produit is null)
        {
            produit = new Produit
            {
                Reference = "PRD-ANOM-001",
                Designation = "Service Démo Anomalie",
                PrixUnitaireHT = 350.000m,
                TauxTVA = 19m,
                Unite = "session",
                EstActif = true
            };

            db.Produits.Add(produit);
            await db.SaveChangesAsync(cancellationToken);
        }

        var ligneMontantHt = decimal.Round(produit.PrixUnitaireHT * 4m, 3);
        var ligneTvaTheorique = decimal.Round(ligneMontantHt * (produit.TauxTVA / 100m), 3);
        var ligneTvaErronee = decimal.Round(ligneTvaTheorique + 50m, 3);
        var timbre = 1.000m;

        var montantHt = ligneMontantHt;
        var montantTva = ligneTvaErronee;
        var montantTtcTheorique = decimal.Round(montantHt + montantTva + timbre, 3);
        var montantTtcErrone = decimal.Round(montantTtcTheorique - 200m, 3);
        var dateFacture = DateTime.SpecifyKind(now.Date.AddDays(-2), DateTimeKind.Utc);

        var factureAnormale = new Facture
        {
            NumeroFacture = anomalyNumber,
            DateFacture = dateFacture,
            DateEcheance = DateTime.SpecifyKind(dateFacture.AddDays(30), DateTimeKind.Utc),
            ClientId = clientSansMf.Id,
            MontantHT = montantHt,
            MontantTVA = montantTva,
            MontantTimbre = timbre,
            MontantTTC = montantTtcErrone,
            Statut = StatutFacture.Emise,
            Notes = "Facture de démonstration injectée pour anomalies fiscales dashboard.",
            Lignes =
            [
                new LigneFacture
                {
                    ProduitId = produit.Id,
                    Quantite = 4m,
                    PrixUnitaireHT = produit.PrixUnitaireHT,
                    TauxTVA = produit.TauxTVA,
                    MontantHT = ligneMontantHt,
                    MontantTVA = ligneTvaErronee
                }
            ]
        };

        db.Factures.Add(factureAnormale);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static DateTime StartOfMonth(DateTime utcNow)
        => new DateTime(utcNow.Year, utcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

    private static LigneFacture CreateLigne(Produit produit, decimal quantite)
    {
        var montantHt = decimal.Round(produit.PrixUnitaireHT * quantite, 3);
        var montantTva = decimal.Round(montantHt * (produit.TauxTVA / 100m), 3);

        return new LigneFacture
        {
            Produit = produit,
            Quantite = quantite,
            PrixUnitaireHT = produit.PrixUnitaireHT,
            TauxTVA = produit.TauxTVA,
            MontantHT = montantHt,
            MontantTVA = montantTva
        };
    }

    private static Facture CreateFacture(
        string numeroFacture,
        Client client,
        DateTime dateFactureUtc,
        StatutFacture statut,
        string? notes,
        decimal timbre,
        List<LigneFacture> lignes)
    {
        var montantHt = decimal.Round(lignes.Sum(item => item.MontantHT), 3);
        var montantTva = decimal.Round(lignes.Sum(item => item.MontantTVA), 3);
        var montantTtc = decimal.Round(montantHt + montantTva + timbre, 3);

        return new Facture
        {
            NumeroFacture = numeroFacture,
            Client = client,
            DateFacture = DateTime.SpecifyKind(dateFactureUtc, DateTimeKind.Utc),
            DateEcheance = DateTime.SpecifyKind(dateFactureUtc.AddDays(30), DateTimeKind.Utc),
            Statut = statut,
            Notes = notes,
            MontantTimbre = timbre,
            MontantHT = montantHt,
            MontantTVA = montantTva,
            MontantTTC = montantTtc,
            Lignes = lignes
        };
    }
}
