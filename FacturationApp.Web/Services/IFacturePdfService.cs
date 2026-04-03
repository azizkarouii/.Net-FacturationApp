using FacturationApp.Data.Entities;

namespace FacturationApp.Web.Services
{
    public interface IFacturePdfService
    {
        byte[] Generate(Facture facture, Parametre? parametre = null);
    }
}