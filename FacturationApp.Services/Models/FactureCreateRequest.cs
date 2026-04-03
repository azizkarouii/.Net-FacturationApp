namespace FacturationApp.Services.Models
{
    public sealed class FactureCreateRequest
    {
        public int ClientId { get; set; }
        public DateTime? DateFacture { get; set; }
        public DateTime? DateEcheance { get; set; }
        public string? Notes { get; set; }
        public List<FactureLineRequest> Lignes { get; set; } = new();
    }

    public sealed class FactureLineRequest
    {
        public int ProduitId { get; set; }
        public decimal Quantite { get; set; }
    }
}