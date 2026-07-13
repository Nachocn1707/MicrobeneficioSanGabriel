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
        public DbSet<NotificacionUsuario> NotificacionesUsuarios { get; set; }

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

            builder.Entity<NotificacionUsuario>()
                .HasIndex(n => new { n.UsuarioId, n.Clave })
                .IsUnique();

            builder.Entity<NotificacionUsuario>()
                .HasOne(n => n.Usuario)
                .WithMany()
                .HasForeignKey(n => n.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);


            builder.Entity<Finca>()
                .HasIndex(f => new { f.ProductorId, f.Nombre })
                .IsUnique();

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


            builder.Entity<Pedido>()
                .HasOne(p => p.Cliente)
                .WithMany()
                .HasForeignKey(p => p.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Factura>()
                .HasIndex(f => f.PedidoId)
                .IsUnique()
                .HasFilter("[EstadoPago] <> N'Anulada'");

            builder.Entity<Producto>()
                .Property(p => p.Precio)
                .HasPrecision(18, 2);

            builder.Entity<RegistroFinanciero>()
                .Property(r => r.Monto)
                .HasPrecision(18, 2);

            builder.Entity<Producto>()
                .Property(p => p.Stock)
                .HasPrecision(18, 2);

            builder.Entity<Producto>()
                .Property(p => p.StockMinimo)
                .HasPrecision(18, 2);

            builder.Entity<Pedido>()
                .Property(p => p.Cantidad)
                .HasPrecision(18, 2);

            builder.Entity<MovimientoInventario>()
                .Property(m => m.Cantidad)
                .HasPrecision(18, 2);

            builder.Entity<Lote>()
                .Property(l => l.PesoKg)
                .HasPrecision(18, 2);

            builder.Entity<Produccion>()
                .Property(p => p.CantidadProcesadaKg)
                .HasPrecision(18, 2);

            builder.Entity<Produccion>()
                .Property(p => p.CantidadResultanteKg)
                .HasPrecision(18, 2);

            builder.Entity<MovimientoInventario>()
                .HasIndex(m => new { m.OrigenTipo, m.OrigenId });

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
