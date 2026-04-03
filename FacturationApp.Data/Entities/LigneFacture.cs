namespace FacturationApp.Data.Entities
{
    public class LigneFacture : BaseEntity
    {
        public int FactureId { get; set; }
        public Facture Facture { get; set; } = null!;

        public int ProduitId { get; set; }
        public Produit Produit { get; set; } = null!;

        public decimal Quantite { get; set; }
        public decimal PrixUnitaireHT { get; set; }
        public decimal TauxTVA { get; set; }
        public decimal MontantHT { get; set; }
        public decimal MontantTVA { get; set; }
    }
}