using Microsoft.EntityFrameworkCore;
using FacturationApp.Data.Entities;

namespace FacturationApp.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Client> Clients => Set<Client>();
        public DbSet<Produit> Produits => Set<Produit>();
        public DbSet<Facture> Factures => Set<Facture>();
        public DbSet<LigneFacture> LignesFacture => Set<LigneFacture>();
        public DbSet<Parametre> Parametres => Set<Parametre>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Produit>()
                .Property(p => p.PrixUnitaireHT)
                .HasColumnType("numeric(18,3)");

            modelBuilder.Entity<Facture>()
                .Property(f => f.MontantHT)
                .HasColumnType("numeric(18,3)");

            modelBuilder.Entity<Facture>()
                .Property(f => f.MontantTVA)
                .HasColumnType("numeric(18,3)");

            modelBuilder.Entity<Facture>()
                .Property(f => f.MontantTimbre)
                .HasColumnType("numeric(18,3)");

            modelBuilder.Entity<Facture>()
                .Property(f => f.MontantTTC)
                .HasColumnType("numeric(18,3)");

            modelBuilder.Entity<LigneFacture>()
                .Property(l => l.MontantHT)
                .HasColumnType("numeric(18,3)");

            modelBuilder.Entity<LigneFacture>()
                .Property(l => l.MontantTVA)
                .HasColumnType("numeric(18,3)");

            modelBuilder.Entity<LigneFacture>()
                .Property(l => l.PrixUnitaireHT)
                .HasColumnType("numeric(18,3)");

            modelBuilder.Entity<Facture>()
                .HasIndex(f => f.NumeroFacture)
                .IsUnique();

            modelBuilder.Entity<Produit>()
                .HasIndex(p => p.Reference)
                .IsUnique();

            base.OnModelCreating(modelBuilder);
        }
    }
}