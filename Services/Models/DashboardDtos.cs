namespace FacturationApp.Services.Models
{
    public sealed record TvaParTauxDto(decimal TauxTVA, decimal MontantTVA);

    public sealed record TvaParMoisDto(string Periode, decimal MontantTVA);

    public sealed record VenteParPeriodeDto(string Periode, decimal ChiffreAffairesHT, decimal ChiffreAffairesTTC);

    public sealed record VenteParClientDto(int ClientId, string ClientNom, decimal ChiffreAffairesHT, decimal ChiffreAffairesTTC);

    public sealed record VenteParProduitDto(int ProduitId, string Reference, string Designation, decimal ChiffreAffairesHT, decimal ChiffreAffairesTTC);

    public sealed class FiscalDashboardDto
    {
        public decimal TVACollecteeTotale { get; set; }
        public decimal MontantTimbreTotal { get; set; }
        public List<TvaParTauxDto> TVAParTaux { get; set; } = new();
        public List<TvaParMoisDto> EvolutionTVAParMois { get; set; } = new();
    }

    public sealed record StatutFactureDto(string Statut, int NombreFactures, decimal MontantTTC);

    public sealed class SalesDashboardDto
    {
        public decimal ChiffreAffairesTotalHT { get; set; }
        public decimal ChiffreAffairesTotalTTC { get; set; }
        public List<VenteParPeriodeDto> ChiffreAffairesParPeriode { get; set; } = new();
        public List<VenteParClientDto> ChiffreAffairesParClient { get; set; } = new();
        public List<VenteParProduitDto> ChiffreAffairesParProduit { get; set; } = new();
        public List<StatutFactureDto> RepartitionParStatut { get; set; } = new();
        public decimal TauxRecouvrement { get; set; }
    }
}