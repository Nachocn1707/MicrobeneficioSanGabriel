using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MicrobeneficioSanGabriel.Models;

namespace MicrobeneficioSanGabriel.Data
{
    public class ApplicationDbContext : IdentityDbContext
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
        }
    }

}
