namespace FacturationApp.Data.Entities
{
    public class Parametre : BaseEntity
    {
        public string NomSociete { get; set; } = string.Empty;
        public string? Adresse { get; set; }
        public string? Telephone { get; set; }
        public string? Email { get; set; }
        public string? MatriculeFiscal { get; set; }
        public decimal MontantTimbre { get; set; } = 1.000m;
        public string PrefixeFacture { get; set; } = "FAC-";
        public string? MentionsLegales { get; set; }
    }
}