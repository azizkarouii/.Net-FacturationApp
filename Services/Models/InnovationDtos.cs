namespace FacturationApp.Services.Models
{
    public sealed record FiscalAnomalyDto(
        string Code,
        string Gravite,
        string Message,
        string? NumeroFacture,
        DateTime? DateFacture);

    public sealed record RevenueForecastPointDto(string Periode, decimal MontantHT);

    public sealed class RevenueForecastDto
    {
        public List<RevenueForecastPointDto> Historique { get; set; } = new();
        public decimal PrevisionMoisProchain { get; set; }
        public string LabelMoisProchain { get; set; } = string.Empty;
        public decimal DernierMois { get; set; }
        public decimal TauxEvolution { get; set; }
    }
}