using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MicrobeneficioSanGabriel.Constants;
using MicrobeneficioSanGabriel.Data;
using MicrobeneficioSanGabriel.Models;
using MicrobeneficioSanGabriel.Services;
using QuestPDF.Fluent;
using System.IO;

namespace MicrobeneficioSanGabriel.Controllers
{
    [Authorize(Roles = "Administrador,Vendedor")]
    public class FacturasController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly decimal _porcentajeIva;

        public FacturasController(ApplicationDbContext context, IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _porcentajeIva = configuration.GetValue<decimal?>("Facturacion:PorcentajeIVA") ?? 13m;
        }

        public async Task<IActionResult> Index()
        {
            var facturas = _context.Facturas
                .Include(f => f.Pedido)
                .ThenInclude(p => p!.Producto)
                .OrderByDescending(f => f.FechaFactura);

            return View(await facturas.ToListAsync());
        }

        [Authorize(Roles = "Administrador")]
        public IActionResult Create()
        {
            CargarPedidos();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Create(Factura factura)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.Producto)
                .FirstOrDefaultAsync(p => p.Id == factura.PedidoId);

            // Los montos de la factura se recalculan desde el pedido.
            // Se eliminan del ModelState para evitar errores cuando el navegador envía decimales
            // con coma o punto. En pantalla se muestran como enteros.
            ModelState.Remove(nameof(Factura.Subtotal));
            ModelState.Remove(nameof(Factura.IVA));
            ModelState.Remove(nameof(Factura.Total));

            if (pedido == null)
            {
                ModelState.AddModelError("", "El pedido seleccionado no existe.");
            }
            else
            {
                if (pedido.Estado == EstadosPedido.Cancelado)
                {
                    ModelState.AddModelError("", "No se puede facturar un pedido cancelado.");
                }

                if (pedido.PrecioUnitarioMostrar <= 0)
                {
                    ModelState.AddModelError("", "El pedido no tiene un precio histórico válido para facturar.");
                }

                if (await _context.Facturas.AnyAsync(f => f.PedidoId == factura.PedidoId && f.EstadoPago != EstadosPago.Anulada))
                {
                    ModelState.AddModelError(nameof(Factura.PedidoId), "El pedido ya tiene una factura asociada.");
                }
            }

            if (ModelState.IsValid)
            {
                factura.FechaFactura = DateTime.Now;

                factura.EstadoPago = string.IsNullOrWhiteSpace(pedido!.EstadoPago)
                    ? EstadosPago.Pendiente
                    : pedido.EstadoPago;
                factura.Subtotal = Math.Round(pedido.Cantidad * pedido.PrecioUnitarioMostrar, 2, MidpointRounding.AwayFromZero);
                factura.IVA = Math.Round(factura.Subtotal * (_porcentajeIva / 100m), 2, MidpointRounding.AwayFromZero);
                factura.Total = Math.Round(factura.Subtotal + factura.IVA, 2, MidpointRounding.AwayFromZero);

                _context.Facturas.Add(factura);
                await _context.SaveChangesAsync();
                await AuditoriaHelper.RegistrarAsync(_context, User, "Facturas", "Crear", factura.Id,
                    $"Se creó la factura #{factura.Id} para el pedido #{factura.PedidoId}.");
                TempData["Success"] = "Factura creada correctamente.";

                return RedirectToAction(nameof(Index));
            }

            CargarPedidos(factura.PedidoId);
            return View(factura);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var factura = await _context.Facturas
                .Include(f => f.Pedido)
                .ThenInclude(p => p!.Producto)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (factura == null) return NotFound();

            return View(factura);
        }

        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var factura = await _context.Facturas.FindAsync(id);

            if (factura == null) return NotFound();

            if (factura.EstadoPago == EstadosPago.Anulada)
            {
                TempData["Warning"] = "Una factura anulada se conserva como histórico y no puede editarse.";
                return RedirectToAction(nameof(Details), new { id = factura.Id });
            }

            CargarPedidos(factura.PedidoId);
            return View(factura);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Edit(int id, Factura factura)
        {
            if (id != factura.Id) return NotFound();

            var facturaExistente = await _context.Facturas
                .FirstOrDefaultAsync(f => f.Id == id);

            if (facturaExistente == null) return NotFound();

            if (facturaExistente.EstadoPago == EstadosPago.Anulada)
            {
                TempData["Warning"] = "Una factura anulada se conserva como histórico y no puede editarse.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var pedido = await _context.Pedidos
                .Include(p => p.Producto)
                .FirstOrDefaultAsync(p => p.Id == factura.PedidoId);

            // Los montos siempre se recalculan con los datos vigentes del pedido.
            ModelState.Remove(nameof(Factura.Subtotal));
            ModelState.Remove(nameof(Factura.IVA));
            ModelState.Remove(nameof(Factura.Total));

            var estadosPermitidos = new[]
            {
                EstadosPago.Pendiente,
                EstadosPago.PendientePago,
                EstadosPago.PagoCompletado,
                EstadosPago.Anulada
            };

            if (!estadosPermitidos.Contains(factura.EstadoPago, StringComparer.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(Factura.EstadoPago), "El estado de pago seleccionado no es válido.");
            }

            if (pedido == null)
            {
                ModelState.AddModelError("", "El pedido seleccionado no existe.");
            }
            else
            {
                if (pedido.Estado == EstadosPedido.Cancelado)
                {
                    ModelState.AddModelError("", "No se puede asociar una factura a un pedido cancelado.");
                }

                if (pedido.PrecioUnitarioMostrar <= 0)
                {
                    ModelState.AddModelError("", "El pedido no tiene un precio histórico válido para facturar.");
                }

                if (await _context.Facturas.AnyAsync(f =>
                        f.PedidoId == factura.PedidoId &&
                        f.Id != factura.Id &&
                        f.EstadoPago != EstadosPago.Anulada))
                {
                    ModelState.AddModelError(nameof(Factura.PedidoId), "El pedido ya tiene otra factura activa asociada.");
                }
            }

            if (ModelState.IsValid)
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    facturaExistente.PedidoId = factura.PedidoId;
                    facturaExistente.FechaFactura = factura.FechaFactura;
                    facturaExistente.Observacion = factura.Observacion;
                    facturaExistente.EstadoPago = estadosPermitidos.First(e =>
                        string.Equals(e, factura.EstadoPago, StringComparison.OrdinalIgnoreCase));
                    facturaExistente.Subtotal = Math.Round(pedido!.Cantidad * pedido.PrecioUnitarioMostrar, 2, MidpointRounding.AwayFromZero);
                    facturaExistente.IVA = Math.Round(facturaExistente.Subtotal * (_porcentajeIva / 100m), 2, MidpointRounding.AwayFromZero);
                    facturaExistente.Total = Math.Round(facturaExistente.Subtotal + facturaExistente.IVA, 2, MidpointRounding.AwayFromZero);

                    if (facturaExistente.EstadoPago != EstadosPago.Anulada)
                    {
                        pedido.EstadoPago = facturaExistente.EstadoPago;
                    }

                    await _context.SaveChangesAsync();
                    await AuditoriaHelper.RegistrarAsync(_context, User, "Facturas", "Editar", facturaExistente.Id,
                        $"Se actualizó la factura #{facturaExistente.Id}.");
                    await transaction.CommitAsync();
                    TempData["Success"] = "Factura actualizada correctamente.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    await transaction.RollbackAsync();
                    if (!FacturaExists(factura.Id)) return NotFound();
                    ModelState.AddModelError("", "La factura fue modificada por otro usuario. Recargá la página e intentá nuevamente.");
                }
            }

            if (!ModelState.IsValid)
            {
                CargarPedidos(factura.PedidoId);
                return View(factura);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Anular(int id)
        {
            var factura = await _context.Facturas.FindAsync(id);

            if (factura == null)
            {
                return NotFound();
            }

            factura.EstadoPago = EstadosPago.Anulada;

            _context.Update(factura);
            await _context.SaveChangesAsync();
            await AuditoriaHelper.RegistrarAsync(_context, User, "Facturas", "Anular", factura.Id,
                $"Se anuló la factura #{factura.Id}.");
            TempData["Success"] = "Factura anulada correctamente.";

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var factura = await _context.Facturas
                .Include(f => f.Pedido)
                .ThenInclude(p => p!.Producto)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (factura == null) return NotFound();

            TempData["Warning"] = "Las facturas no se eliminan; se anulan para conservar el histórico contable.";
            return RedirectToAction(nameof(Details), new { id = factura.Id });
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var factura = await _context.Facturas.FindAsync(id);
            if (factura == null) return NotFound();

            factura.EstadoPago = EstadosPago.Anulada;
            _context.Update(factura);
            await _context.SaveChangesAsync();
            await AuditoriaHelper.RegistrarAsync(_context, User, "Facturas", "Anular", factura.Id,
                $"Se anuló la factura #{factura.Id} desde la ruta de eliminación heredada.");
            TempData["Success"] = "Factura anulada correctamente. El registro se conserva en el histórico.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Imprimir(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var factura = await _context.Facturas
                .Include(f => f.Pedido)
                .ThenInclude(p => p!.Producto)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (factura == null)
            {
                return NotFound();
            }

            var logoPath = Path.Combine(_webHostEnvironment.WebRootPath, "img", "logo.png");
            var document = new FacturaDocument(factura, logoPath);
            byte[] pdfBytes = document.GeneratePdf();

            return File(pdfBytes, "application/pdf", $"Factura_{factura.Id}.pdf");
        }

        private void CargarPedidos(int? pedidoSeleccionado = null)
        {
            var pedidosFacturados = _context.Facturas
                .Where(f => f.EstadoPago != EstadosPago.Anulada &&
                            (!pedidoSeleccionado.HasValue || f.PedidoId != pedidoSeleccionado.Value))
                .Select(f => f.PedidoId);

            var pedidos = _context.Pedidos
                .Include(p => p.Producto)
                .Where(p => p.Estado != EstadosPedido.Cancelado &&
                            (p.ProductoId != null || p.PrecioUnitario > 0) &&
                            (!pedidosFacturados.Contains(p.Id) || p.Id == pedidoSeleccionado))
                .OrderByDescending(p => p.FechaPedido)
                .AsNoTracking()
                .ToList()
                .Select(p => new
                {
                    Id = p.Id,
                    Nombre = $"{p.ClienteNombre} - {p.ProductoNombreMostrar} - {p.Cantidad} kg"
                });

            ViewBag.PedidoId = new SelectList(
                pedidos,
                "Id",
                "Nombre",
                pedidoSeleccionado
            );
        }

        private bool FacturaExists(int id)
        {
            return _context.Facturas.Any(e => e.Id == id);
        }
    }
}