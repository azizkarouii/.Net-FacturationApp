using FacturationApp.Data.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FacturationApp.Web.Services
{
    public sealed class FacturePdfService : IFacturePdfService
    {
        public byte[] Generate(Facture facture, Parametre? parametre = null)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(28);
                    page.DefaultTextStyle(textStyle => textStyle.FontSize(10));

                    page.Header().Column(header =>
                    {
                        header.Item().Text(parametre?.NomSociete ?? "FacturationApp").FontSize(18).SemiBold();
                        header.Item().Text(parametre?.Adresse ?? string.Empty);
                        header.Item().Text($"Tél: {parametre?.Telephone ?? "-"} | Email: {parametre?.Email ?? "-"}");
                        header.Item().PaddingTop(8).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
                    });

                    page.Content().PaddingVertical(12).Column(content =>
                    {
                        content.Spacing(12);

                        content.Item().Row(row =>
                        {
                            row.RelativeItem().Column(left =>
                            {
                                left.Item().Text("Facture").FontSize(14).SemiBold();
                                left.Item().Text($"Numéro: {facture.NumeroFacture}");
                                left.Item().Text($"Date: {facture.DateFacture:dd/MM/yyyy}");
                                left.Item().Text($"Échéance: {(facture.DateEcheance.HasValue ? facture.DateEcheance.Value.ToString("dd/MM/yyyy") : "N/A")}");
                                left.Item().Text($"Statut: {facture.Statut}");
                            });

                            row.RelativeItem().AlignRight().Column(right =>
                            {
                                right.Item().Text("Client").FontSize(14).SemiBold();
                                right.Item().Text(facture.Client.Nom);
                                right.Item().Text(facture.Client.Adresse ?? string.Empty);
                                right.Item().Text(facture.Client.Telephone ?? string.Empty);
                                right.Item().Text(facture.Client.Email ?? string.Empty);
                                right.Item().Text(facture.Client.MatriculeFiscal ?? string.Empty);
                            });
                        });

                        content.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(30);
                                columns.RelativeColumn(3);
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("#");
                                header.Cell().Element(CellStyle).Text("Produit");
                                header.Cell().Element(CellStyle).AlignRight().Text("Qté");
                                header.Cell().Element(CellStyle).AlignRight().Text("PU HT");
                                header.Cell().Element(CellStyle).AlignRight().Text("TVA");
                                header.Cell().Element(CellStyle).AlignRight().Text("Total HT");

                                static IContainer CellStyle(IContainer container)
                                {
                                    return container.DefaultTextStyle(x => x.SemiBold()).Padding(6).Background(Colors.Grey.Lighten3);
                                }
                            });

                            var index = 1;
                            foreach (var ligne in facture.Lignes)
                            {
                                table.Cell().Element(CellStyle).Text(index++.ToString());
                                table.Cell().Element(CellStyle).Text(ligne.Produit.Designation);
                                table.Cell().Element(CellStyle).AlignRight().Text(ligne.Quantite.ToString("N3"));
                                table.Cell().Element(CellStyle).AlignRight().Text(ligne.PrixUnitaireHT.ToString("N3"));
                                table.Cell().Element(CellStyle).AlignRight().Text($"{ligne.TauxTVA:N0}%");
                                table.Cell().Element(CellStyle).AlignRight().Text(ligne.MontantHT.ToString("N3"));

                                static IContainer CellStyle(IContainer container)
                                {
                                    return container.Padding(6).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
                                }
                            }
                        });

                        content.Item().AlignRight().Column(totals =>
                        {
                            totals.Item().Text($"Total HT: {facture.MontantHT:N3}");
                            totals.Item().Text($"Total TVA: {facture.MontantTVA:N3}");
                            totals.Item().Text($"Timbre fiscal: {facture.MontantTimbre:N3}");
                            totals.Item().Text($"Total TTC: {facture.MontantTTC:N3}").FontSize(12).SemiBold();
                        });

                        if (!string.IsNullOrWhiteSpace(facture.Notes))
                        {
                            content.Item().PaddingTop(8).Text($"Notes: {facture.Notes}");
                        }
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("Document généré automatiquement - ");
                        text.Span(DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm"));
                    });
                });
            });

            return document.GeneratePdf();
        }
    }
}