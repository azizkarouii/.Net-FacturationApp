using System.ComponentModel.DataAnnotations;

namespace FacturationApp.Data.Entities
{
    public class Client : BaseEntity
    {
        [Required, MaxLength(100)]
        public string Nom { get; set; } = string.Empty;

        public string? Adresse { get; set; }

        [MaxLength(20)]
        public string? Telephone { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        public string? MatriculeFiscal { get; set; }
        public bool EstActif { get; set; } = true;

        public ICollection<Facture> Factures { get; set; } = new List<Facture>();
    }
}