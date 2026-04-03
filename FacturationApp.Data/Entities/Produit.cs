using System.ComponentModel.DataAnnotations;

namespace FacturationApp.Data.Entities
{
    public class Produit : BaseEntity
    {
        [Required, MaxLength(50)]
        public string Reference { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string Designation { get; set; } = string.Empty;

        [Range(0, double.MaxValue)]
        public decimal PrixUnitaireHT { get; set; }

        public decimal TauxTVA { get; set; } = 19;
        public string Unite { get; set; } = "pièce";
        public bool EstActif { get; set; } = true;

        public ICollection<LigneFacture> Lignes { get; set; } = new List<LigneFacture>();
    }
}