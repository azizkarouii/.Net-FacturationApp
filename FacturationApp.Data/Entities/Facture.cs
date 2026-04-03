using System.ComponentModel.DataAnnotations;

namespace FacturationApp.Data.Entities
{
    public enum StatutFacture { Brouillon, Emise, Payee, Annulee }

    public class Facture : BaseEntity
    {
        [Required]
        public string NumeroFacture { get; set; } = string.Empty;

        public DateTime DateFacture { get; set; } = DateTime.UtcNow;
        public DateTime? DateEcheance { get; set; }

        public int ClientId { get; set; }
        public Client Client { get; set; } = null!;

        public decimal MontantHT { get; set; }
        public decimal MontantTVA { get; set; }
        public decimal MontantTimbre { get; set; } = 1.000m;
        public decimal MontantTTC { get; set; }

        public StatutFacture Statut { get; set; } = StatutFacture.Brouillon;
        public string? Notes { get; set; }

        public ICollection<LigneFacture> Lignes { get; set; } = new List<LigneFacture>();
    }
}