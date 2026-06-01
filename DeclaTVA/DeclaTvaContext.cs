using Microsoft.EntityFrameworkCore;


namespace DeclaTVA
{
    public class DeclaTvaContext : DbContext
    {
        
        public DeclaTvaContext(DbContextOptions<DeclaTvaContext> options) : base(options)
        {
        }
        public DbSet<Declaration> Declarations { get; set; }
        public DbSet<LigneTva> LignesTVA { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Table Decla
            modelBuilder.Entity<Declaration>(entity =>
            {
                entity.HasKey(d => d.Id);
                entity.Property(d => d.Societe).HasMaxLength(200);
                entity.Property(d => d.IdentifiantFiscal).HasMaxLength(200);
                entity.Property(d => d.ICE).HasMaxLength(200);
                entity.Property(d => d.Regime).HasMaxLength(200);
                entity.Property(d => d.Periode).HasMaxLength(50);
                entity.Property(d => d.Annee).HasMaxLength(10);
                entity.Property(d => d.DateImport).HasDefaultValueSql("GETDATE()");
            });

            // Table LignesTVA
            modelBuilder.Entity<LigneTva>(entity =>
            {
                entity.HasKey(l => l.Id);
                entity.Property(l => l.NumFacture).HasMaxLength(200);
                entity.Property(l => l.DateFacture).HasMaxLength(200);
                entity.Property(l => l.Fournisseur).HasMaxLength(200);
                entity.Property(l => l.Designation).HasMaxLength(500);

                // Montants en DECIMAL
                entity.Property(l => l.MontantHT).HasConversion(
                        v => decimal.Parse(v, System.Globalization.CultureInfo.InvariantCulture),
                        v => v.ToString(System.Globalization.CultureInfo.InvariantCulture)).HasColumnType("decimal(18,2)");

                entity.Property(l => l.MontantTVA).HasConversion(
                        v => decimal.Parse(v, System.Globalization.CultureInfo.InvariantCulture),
                        v => v.ToString(System.Globalization.CultureInfo.InvariantCulture)).HasColumnType("decimal(18,2)");

                entity.Property(l => l.MontantTTC).HasConversion(
                        v => decimal.Parse(v, System.Globalization.CultureInfo.InvariantCulture),
                        v => v.ToString(System.Globalization.CultureInfo.InvariantCulture)).HasColumnType("decimal(18,2)");

                // Relation 
                entity.HasOne(l => l.Declaration).WithMany(d => d.Lignes).HasForeignKey(l => l.DeclarationId);
            });
        }
    }
}