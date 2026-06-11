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
                entity.Property(d => d.DateImport).HasDefaultValueSql("GETDATE()");
            });

            // Table LignesTVA
            modelBuilder.Entity<LigneTva>(entity =>
            {
                entity.HasKey(l => l.Id);
                entity.Property(l => l.NumFacture).HasMaxLength(200);
                entity.Property(l => l.Fournisseur).HasMaxLength(200);
                entity.Property(l => l.Designation).HasMaxLength(500);
                entity.Property(l => l.IFFournisseur).HasMaxLength(200);
                entity.Property(l => l.ICEFournisseur).HasMaxLength(200);
                entity.Property(l => l.MontantHT).HasColumnType("decimal(18,2)");
                entity.Property(l => l.MontantTVA).HasColumnType("decimal(18,2)");
                entity.Property(l => l.MontantTTC).HasColumnType("decimal(18,2)");
                entity.Property(l => l.TauxTVA).HasColumnType("decimal(5,2)");
                entity.Property(l => l.Prorata).HasColumnType("decimal(5,2)");


                // Relation 
                entity.HasOne(l => l.Declaration).WithMany(d => d.Lignes).HasForeignKey(l => l.DeclarationId);
            });
        }
    }
}