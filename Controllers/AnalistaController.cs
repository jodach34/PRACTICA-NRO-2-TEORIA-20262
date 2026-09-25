using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using PRACTICA_NRO_2_TEORIA_20262.Data;
using PRACTICA_NRO_2_TEORIA_20262.Hubs;
using PRACTICA_NRO_2_TEORIA_20262.Models;

namespace PRACTICA_NRO_2_TEORIA_20262.Controllers
{
    [Authorize(Roles = "Analista")]
    public class AnalistaController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly IHubContext<SolicitudesHub> _hubContext; // Herramienta de WebSockets

        // Inyectamos el HubContext en el constructor
        public AnalistaController(ApplicationDbContext context, IDistributedCache cache, IHubContext<SolicitudesHub> hubContext)
        {
            _context = context;
            _cache = cache;
            _hubContext = hubContext;
        }

        public async Task<IActionResult> Index()
        {
            var pendientes = await _context.Solicitudes
                .Include(s => s.Cliente)
                .Where(s => s.Estado == EstadoSolicitud.Pendiente)
                .OrderBy(s => s.FechaSolicitud)
                .ToListAsync();

            return View(pendientes);
        }

        public async Task<IActionResult> Evaluar(int id)
        {
            var solicitud = await _context.Solicitudes
                .Include(s => s.Cliente)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (solicitud == null || solicitud.Estado != EstadoSolicitud.Pendiente)
                return NotFound("La solicitud no existe o ya fue procesada.");

            return View(solicitud);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Procesar(int id, string accion, string motivoRechazo)
        {
            var solicitud = await _context.Solicitudes
                .Include(s => s.Cliente)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (solicitud == null || solicitud.Estado != EstadoSolicitud.Pendiente)
                return BadRequest("La solicitud ya fue evaluada.");

            if (accion == "Aprobar")
            {
                if (solicitud.MontoSolicitado > (solicitud.Cliente!.IngresosMensuales * 5))
                {
                    TempData["Error"] = $"No se puede aprobar. El monto excede el límite permitido (Máx: {solicitud.Cliente.IngresosMensuales * 5:C}).";
                    return RedirectToAction(nameof(Evaluar), new { id });
                }
                solicitud.Estado = EstadoSolicitud.Aprobado;
            }
            else if (accion == "Rechazar")
            {
                if (string.IsNullOrWhiteSpace(motivoRechazo))
                {
                    TempData["Error"] = "El motivo de rechazo es obligatorio.";
                    return RedirectToAction(nameof(Evaluar), new { id });
                }
                solicitud.Estado = EstadoSolicitud.Rechazado;
                solicitud.MotivoRechazo = motivoRechazo;
            }

            await _context.SaveChangesAsync();

            // Invalidar Caché
            if (solicitud.Cliente?.UsuarioId != null)
            {
                await _cache.RemoveAsync($"solicitudes_{solicitud.Cliente.UsuarioId}");

                // ENVIAR NOTIFICACIÓN EN TIEMPO REAL VÍA WEBSOCKET
                // Se envía exclusivamente al usuario dueño de la solicitud usando su UsuarioId
                await _hubContext.Clients.User(solicitud.Cliente.UsuarioId).SendAsync(
                    "SolicitudEstadoActualizado", 
                    solicitud.Id, 
                    solicitud.Estado.ToString(), 
                    solicitud.MotivoRechazo ?? ""
                );
            }

            TempData["Exito"] = $"La solicitud #{solicitud.Id} ha sido {solicitud.Estado.ToString().ToLower()}.";
            return RedirectToAction(nameof(Index));
        }
    }
}