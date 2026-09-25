using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using PRACTICA_NRO_2_TEORIA_20262.Data;
using PRACTICA_NRO_2_TEORIA_20262.Models;

namespace PRACTICA_NRO_2_TEORIA_20262.Controllers
{
    [Authorize(Roles = "Analista")] // 1. Restricción de acceso
    public class AnalistaController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IDistributedCache _cache;

        public AnalistaController(ApplicationDbContext context, IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
        }

        // GET: Analista/Index
        public async Task<IActionResult> Index()
        {
            // Solo trae solicitudes pendientes
            var pendientes = await _context.Solicitudes
                .Include(s => s.Cliente)
                .Where(s => s.Estado == EstadoSolicitud.Pendiente)
                .OrderBy(s => s.FechaSolicitud)
                .ToListAsync();

            return View(pendientes);
        }

        // GET: Analista/Evaluar/5
        public async Task<IActionResult> Evaluar(int id)
        {
            var solicitud = await _context.Solicitudes
                .Include(s => s.Cliente)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (solicitud == null || solicitud.Estado != EstadoSolicitud.Pendiente)
                return NotFound("La solicitud no existe o ya fue procesada.");

            return View(solicitud);
        }

        // POST: Analista/Procesar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Procesar(int id, string accion, string motivoRechazo)
        {
            var solicitud = await _context.Solicitudes
                .Include(s => s.Cliente)
                .FirstOrDefaultAsync(s => s.Id == id);

            // 2. No procesar solicitudes ya aprobadas o rechazadas
            if (solicitud == null || solicitud.Estado != EstadoSolicitud.Pendiente)
                return BadRequest("La solicitud ya fue evaluada.");

            if (accion == "Aprobar")
            {
                // 3. No aprobar si excede 5 veces los ingresos
                if (solicitud.MontoSolicitado > (solicitud.Cliente!.IngresosMensuales * 5))
                {
                    TempData["Error"] = $"No se puede aprobar. El monto excede el límite permitido (Máx: {solicitud.Cliente.IngresosMensuales * 5:C}).";
                    return RedirectToAction(nameof(Evaluar), new { id });
                }
                solicitud.Estado = EstadoSolicitud.Aprobado;
            }
            else if (accion == "Rechazar")
            {
                // 4. Motivo obligatorio en rechazo
                if (string.IsNullOrWhiteSpace(motivoRechazo))
                {
                    TempData["Error"] = "El motivo de rechazo es obligatorio.";
                    return RedirectToAction(nameof(Evaluar), new { id });
                }
                solicitud.Estado = EstadoSolicitud.Rechazado;
                solicitud.MotivoRechazo = motivoRechazo;
            }

            await _context.SaveChangesAsync();

            // 5. Invalidar la caché de este cliente específico tras actualizar el estado
            if (solicitud.Cliente?.UsuarioId != null)
            {
                await _cache.RemoveAsync($"solicitudes_{solicitud.Cliente.UsuarioId}");
            }

            TempData["Exito"] = $"La solicitud #{solicitud.Id} ha sido {solicitud.Estado.ToString().ToLower()}.";
            return RedirectToAction(nameof(Index));
        }
    }
}