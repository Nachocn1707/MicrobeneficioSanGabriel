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
        public DbSet<Finca> Fincas { get; set; }
        public DbSet<Producto> Productos { get; set; }
        public DbSet<MovimientoInventario> MovimientosInventario { get; set; }
        public DbSet<Lote> Lotes { get; set; }
        public DbSet<Produccion> Producciones { get; set; }
        public DbSet<Trazabilidad> Trazabilidades { get; set; }
        public DbSet<Pedido> Pedidos { get; set; }
        public DbSet<Factura> Facturas { get; set; }
        public DbSet<RegistroFinanciero> RegistrosFinancieros { get; set; }
        public DbSet<AuditoriaRegistro> Auditorias { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Productor>()
                .HasIndex(p => p.Cedula)
                .IsUnique();

            builder.Entity<Productor>()
                .HasIndex(p => p.Correo)
                .IsUnique()
                .HasFilter("[Correo] IS NOT NULL");

            builder.Entity<AuditoriaRegistro>()
                .HasIndex(a => a.Fecha);

            builder.Entity<AuditoriaRegistro>()
                .HasIndex(a => a.Modulo);

            builder.Entity<Finca>()
                .HasOne(f => f.Productor)
                .WithMany(p => p.Fincas)
                .HasForeignKey(f => f.ProductorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Lote>()
                .HasOne(l => l.Productor)
                .WithMany(p => p.Lotes)
                .HasForeignKey(l => l.ProductorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Lote>()
                .HasOne(l => l.Finca)
                .WithMany(f => f.Lotes)
                .HasForeignKey(l => l.FincaId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Lote>()
                .HasIndex(l => l.CodigoLote)
                .IsUnique();

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
