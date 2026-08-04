using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MicrobeneficioSanGabriel.Constants;
using MicrobeneficioSanGabriel.Data;
using MicrobeneficioSanGabriel.Models;
using MicrobeneficioSanGabriel.Services;

namespace MicrobeneficioSanGabriel.Controllers
{
    [Authorize(Roles = "Administrador,Operador")]
    public class LotesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LotesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var lotes = await _context.Lotes
                .Include(l => l.Productor)
                .Include(l => l.Finca)
                .OrderByDescending(l => l.FechaRecepcion)
                .ToListAsync();

            return View(lotes);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var lote = await _context.Lotes
                .Include(l => l.Productor)
                .Include(l => l.Finca)
                .FirstOrDefaultAsync(l => l.Id == id);

            return lote == null ? NotFound() : View(lote);
        }

        public IActionResult Create()
        {
            CargarFincas();

            return View(new Lote
            {
                FechaRecepcion = DateTime.Now,
                Estado = "Recibido"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("FincaId,PesoKg,FechaRecepcion,Estado,Observacion")]
            Lote lote)
        {
            var finca = lote.FincaId.HasValue
                ? await _context.Fincas
                    .Include(f => f.Productor)
                    .FirstOrDefaultAsync(f => f.Id == lote.FincaId.Value && f.Activa)
                : null;

            if (finca == null)
            {
                ModelState.AddModelError(nameof(Lote.FincaId), "Seleccione una finca activa y válida.");
            }
            else
            {
                // El productor no viene en el formulario: se obtiene de la finca seleccionada.
                lote.ProductorId = finca.ProductorId;
            }

            // ProductorId es obligatorio en el modelo, pero deliberadamente no se publica desde la vista.
            // La validación automática lo marca como 0 antes de entrar al método; al asignarlo en el
            // servidor debemos retirar ese resultado obsoleto para permitir guardar el lote.
            ModelState.Remove(nameof(Lote.ProductorId));

            if (!EstadoLoteValido(lote.Estado))
            {
                ModelState.AddModelError(nameof(Lote.Estado), "Seleccione un estado válido.");
            }

            if (ModelState.IsValid)
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Código temporal corto para que no falle el límite de 30 caracteres.
                    // Antes estaba usando TEMP + Guid completo y eso podía pasar el límite de la columna CodigoLote.
                    lote.CodigoLote = $"TMP-{Guid.NewGuid():N}".Substring(0, 30);

                    lote.Observacion = string.IsNullOrWhiteSpace(lote.Observacion)
                        ? null
                        : lote.Observacion.Trim();

                    _context.Lotes.Add(lote);
                    await _context.SaveChangesAsync();

                    // Código final visible para el usuario.
                    lote.CodigoLote = $"SG-{lote.FechaRecepcion:yyyy}-{lote.Id:D5}";

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    await AuditoriaHelper.RegistrarAsync(
                        _context,
                        User,
                        "Lotes",
                        "Crear",
                        lote.Id,
                        $"Se registró el lote {lote.CodigoLote} para la finca {finca!.Nombre}.");

                    TempData["Success"] = $"Lote {lote.CodigoLote} registrado correctamente.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException)
                {
                    await transaction.RollbackAsync();

                    ModelState.AddModelError(
                        string.Empty,
                        "No fue posible registrar el lote. Revise la información e inténtelo nuevamente.");
                }
            }

            ViewBag.SoloEstado = EsOperadorSoloEstado();
            CargarFincas(lote.FincaId);
            return View(lote);
        }

        [Authorize(Roles = "Administrador,Operador")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var lote = await _context.Lotes.FindAsync(id);

            if (lote == null) return NotFound();

            ViewBag.SoloEstado = EsOperadorSoloEstado();
            CargarFincas(lote.FincaId);
            return View(lote);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Operador")]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,FincaId,PesoKg,FechaRecepcion,Estado,Observacion")]
            Lote lote)
        {
            if (id != lote.Id) return NotFound();

            // Se carga la entidad real para conservar siempre el código y evitar sobrescribir
            // campos protegidos con valores vacíos provenientes del formulario.
            var loteActual = await _context.Lotes.FirstOrDefaultAsync(l => l.Id == id);
            if (loteActual == null) return NotFound();

            lote.CodigoLote = loteActual.CodigoLote;

            if (EsOperadorSoloEstado())
            {
                if (!EstadoLoteValido(lote.Estado))
                {
                    ModelState.AddModelError(nameof(Lote.Estado), "Seleccione un estado válido.");
                    ViewBag.SoloEstado = true;
                    CargarFincas(loteActual.FincaId);
                    loteActual.Estado = lote.Estado;
                    return View(loteActual);
                }

                var estadoAnterior = loteActual.Estado;
                loteActual.Estado = lote.Estado;

                await _context.SaveChangesAsync();
                await AuditoriaHelper.RegistrarAsync(
                    _context,
                    User,
                    "Lotes",
                    "Cambiar estado",
                    loteActual.Id,
                    $"El operador cambió el estado del lote {loteActual.CodigoLote} de {estadoAnterior} a {loteActual.Estado}.");

                TempData["Success"] = $"Estado del lote {loteActual.CodigoLote} actualizado correctamente.";
                return RedirectToAction(nameof(Index));
            }

            var finca = lote.FincaId.HasValue
                ? await _context.Fincas.FirstOrDefaultAsync(f => f.Id == lote.FincaId.Value)
                : null;

            if (finca == null)
            {
                ModelState.AddModelError(nameof(Lote.FincaId), "Seleccione una finca válida.");
            }
            else
            {
                // Igual que en Create, el productor se deriva de la finca y no se acepta del cliente.
                lote.ProductorId = finca.ProductorId;
            }

            // Estos campos no se publican desde la vista. Se restauran en el servidor y se elimina
            // cualquier resultado de validación generado antes de asignarlos.
            ModelState.Remove(nameof(Lote.ProductorId));
            ModelState.Remove(nameof(Lote.CodigoLote));

            if (!EstadoLoteValido(lote.Estado))
            {
                ModelState.AddModelError(nameof(Lote.Estado), "Seleccione un estado válido.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Actualización explícita: el código queda intacto y no se permite overposting.
                    loteActual.FincaId = lote.FincaId;
                    loteActual.ProductorId = finca!.ProductorId;
                    loteActual.PesoKg = lote.PesoKg;
                    loteActual.FechaRecepcion = lote.FechaRecepcion;
                    loteActual.Estado = lote.Estado;
                    loteActual.Observacion = string.IsNullOrWhiteSpace(lote.Observacion)
                        ? null
                        : lote.Observacion.Trim();

                    await _context.SaveChangesAsync();

                    await AuditoriaHelper.RegistrarAsync(
                        _context,
                        User,
                        "Lotes",
                        "Editar",
                        loteActual.Id,
                        $"Se actualizó el lote {loteActual.CodigoLote}.");

                    TempData["Success"] = $"Lote {loteActual.CodigoLote} actualizado correctamente.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LoteExists(lote.Id)) return NotFound();
                    throw;
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "No fue posible actualizar el lote. Verifique sus relaciones.");
                }
            }

            // Si hay un error de validación, el código original sigue visible y protegido.
            lote.ProductorId = finca?.ProductorId ?? loteActual.ProductorId;
            ViewBag.SoloEstado = false;
            CargarFincas(lote.FincaId);
            return View(lote);
        }

        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var lote = await _context.Lotes
                .Include(l => l.Productor)
                .Include(l => l.Finca)
                .FirstOrDefaultAsync(l => l.Id == id);

            return lote == null ? NotFound() : View(lote);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var lote = await _context.Lotes.FindAsync(id);

                if (lote == null) return NotFound();

                var producciones = await _context.Producciones
                    .Include(p => p.Producto)
                    .Where(p => p.LoteId == id)
                    .ToListAsync();

                var produccionesCompletadas = producciones
                    .Where(p => p.Estado == EstadosProduccion.Completado && p.ProductoId.HasValue)
                    .ToList();

                var cantidadesPorProducto = produccionesCompletadas
                    .GroupBy(p => p.ProductoId!.Value)
                    .ToDictionary(g => g.Key, g => g.Sum(p => p.CantidadResultanteKg));

                foreach (var item in cantidadesPorProducto)
                {
                    var producto = producciones.First(p => p.ProductoId == item.Key).Producto;

                    if (producto == null || producto.Stock < item.Value)
                    {
                        TempData["Error"] =
                            "No se puede eliminar el lote porque parte del inventario generado por sus producciones ya fue utilizado.";

                        return RedirectToAction(nameof(Index));
                    }
                }

                foreach (var item in cantidadesPorProducto)
                {
                    var producto = producciones.First(p => p.ProductoId == item.Key).Producto!;
                    producto.Stock -= item.Value;
                }

                foreach (var produccion in produccionesCompletadas)
                {
                    _context.MovimientosInventario.Add(new MovimientoInventario
                    {
                        ProductoId = produccion.ProductoId!.Value,
                        ProductoNombre = produccion.ProductoNombreMostrar,
                        TipoMovimiento = "Salida",
                        Cantidad = produccion.CantidadResultanteKg,
                        FechaMovimiento = DateTime.Now,
                        Observacion = $"Reversión automática al eliminar el lote {lote.CodigoLote}; producción #{produccion.Id}",
                        OrigenTipo = OrigenMovimiento.ReversionProduccion,
                        OrigenId = produccion.Id,
                        EsAutomatico = true
                    });
                }

                var idsProducciones = producciones.Select(p => p.Id).ToList();

                var trazabilidades = await _context.Trazabilidades
                    .Where(t => t.LoteId == id || idsProducciones.Contains(t.ProduccionId))
                    .ToListAsync();

                var codigo = lote.CodigoLote;

                _context.Trazabilidades.RemoveRange(trazabilidades);
                // Los movimientos automáticos se conservan como historial contable del inventario.
                _context.Producciones.RemoveRange(producciones);
                _context.Lotes.Remove(lote);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                await AuditoriaHelper.RegistrarAsync(
                    _context,
                    User,
                    "Lotes",
                    "Eliminar",
                    id,
                    $"Se eliminó el lote {codigo} y sus registros dependientes.");

                TempData["Success"] = $"Lote {codigo} eliminado correctamente.";
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync();

                TempData["Error"] =
                    "No fue posible eliminar el lote porque todavía tiene información relacionada.";
            }

            return RedirectToAction(nameof(Index));
        }

        private void CargarFincas(int? seleccionada = null)
        {
            var fincas = _context.Fincas
                .Where(f => f.Activa && f.Productor != null && f.Productor.Activo)
                .OrderBy(f => f.Productor!.Nombre)
                .ThenBy(f => f.Nombre)
                .Select(f => new
                {
                    f.Id,
                    Texto = f.Productor!.Nombre + " — " + f.Nombre + " (" + f.Distrito + ", " + f.Canton + ")"
                });

            ViewBag.FincaId = new SelectList(fincas, "Id", "Texto", seleccionada);
        }

        private bool EsOperadorSoloEstado()
        {
            return User.IsInRole("Operador") && !User.IsInRole("Administrador");
        }

        private static bool EstadoLoteValido(string? estado)
        {
            return estado is "Recibido" or "En proceso" or "Finalizado" or "Cancelado";
        }

        private bool LoteExists(int id)
        {
            return _context.Lotes.Any(e => e.Id == id);
        }
    }
}