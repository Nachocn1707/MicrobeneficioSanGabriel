using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MicrobeneficioSanGabriel.Models;

namespace MicrobeneficioSanGabriel.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Productor> Productores { get; set; }
        public DbSet<Producto> Productos { get; set; }
        public DbSet<MovimientoInventario> MovimientosInventario { get; set; }
        public DbSet<Lote> Lotes { get; set; }
        public DbSet<Produccion> Producciones { get; set; }
        public DbSet<Trazabilidad> Trazabilidades { get; set; }
        public DbSet<Pedido> Pedidos { get; set; }
        public DbSet<Factura> Facturas { get; set; }
        public DbSet<RegistroFinanciero> RegistrosFinancieros { get; set; }
        public DbSet<MetodoPago> MetodosPago { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Trazabilidad>()
                .HasOne(t => t.Lote)
                .WithMany()
                .HasForeignKey(t => t.LoteId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Trazabilidad>()
                .HasOne(t => t.Produccion)
                .WithMany()
                .HasForeignKey(t => t.ProduccionId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.Entity<Factura>()
                .Property(f => f.Subtotal)
                .HasPrecision(18, 2);

            builder.Entity<Factura>()
                .Property(f => f.IVA)
                .HasPrecision(18, 2);

            builder.Entity<Factura>()
                .Property(f => f.Total)
                .HasPrecision(18, 2);
        }
    }

}
