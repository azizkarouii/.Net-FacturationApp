using FacturationApp.Data;
using FacturationApp.Data.Entities;
using FacturationApp.Services.Contracts;
using Microsoft.EntityFrameworkCore;

namespace FacturationApp.Services.Implementations
{
    public sealed class ProduitService : IProduitService
    {
        private readonly AppDbContext _db;

        public ProduitService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<Produit>> GetAllAsync(string? search = null, bool includeInactive = true, CancellationToken cancellationToken = default)
        {
            var query = _db.Produits.AsNoTracking().AsQueryable();

            if (!includeInactive)
            {
                query = query.Where(produit => produit.EstActif);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(produit =>
                    produit.Reference.ToLower().Contains(term) ||
                    produit.Designation.ToLower().Contains(term) ||
                    produit.Unite.ToLower().Contains(term));
            }

            return await query
                .OrderBy(produit => produit.Reference)
                .ToListAsync(cancellationToken);
        }

        public async Task<Produit?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _db.Produits
                .Include(produit => produit.Lignes)
                .FirstOrDefaultAsync(produit => produit.Id == id, cancellationToken);
        }

        public async Task<Produit> CreateAsync(Produit produit, CancellationToken cancellationToken = default)
        {
            produit.DateCreation = DateTime.UtcNow;
            produit.DateModification = null;

            _db.Produits.Add(produit);
            await _db.SaveChangesAsync(cancellationToken);
            return produit;
        }

        public async Task<Produit?> UpdateAsync(Produit produit, CancellationToken cancellationToken = default)
        {
            var existingProduit = await _db.Produits.FirstOrDefaultAsync(item => item.Id == produit.Id, cancellationToken);
            if (existingProduit is null)
            {
                return null;
            }

            existingProduit.Reference = produit.Reference;
            existingProduit.Designation = produit.Designation;
            existingProduit.PrixUnitaireHT = produit.PrixUnitaireHT;
            existingProduit.TauxTVA = produit.TauxTVA;
            existingProduit.Unite = produit.Unite;
            existingProduit.EstActif = produit.EstActif;
            existingProduit.DateModification = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);
            return existingProduit;
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var produit = await _db.Produits.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
            if (produit is null)
            {
                return false;
            }

            produit.EstActif = false;
            produit.DateModification = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}