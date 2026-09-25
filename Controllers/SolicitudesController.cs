using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using System.Text.Json.Serialization;
using PRACTICA_NRO_2_TEORIA_20262.Data;
using PRACTICA_NRO_2_TEORIA_20262.Models;

namespace PRACTICA_NRO_2_TEORIA_20262.Controllers
{
    [Authorize]
    public class SolicitudesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IDistributedCache _cache; // <-- Herramienta de Redis

        // Inyectamos el caché en el constructor
        public SolicitudesController(ApplicationDbContext context, UserManager<IdentityUser> userManager, IDistributedCache cache)
        {
            _context = context;
            _userManager = userManager;
            _cache = cache;
        }

        // GET: Solicitudes/MisSolicitudes
        public async Task<IActionResult> MisSolicitudes(FiltroSolicitudesViewModel filtro)
        {
            var userId = _userManager.GetUserId(User);
            
            // Leemos la Sesión para saber cuál fue la última solicitud que vio
            var ultimaVista = HttpContext.Session.GetInt32("UltimaSolicitudId");
            ViewBag.UltimaVista = ultimaVista;

            string cacheKey = $"solicitudes_{userId}";
            bool usaFiltros = filtro.Estado.HasValue || filtro.MontoMinimo.HasValue || filtro.MontoMaximo.HasValue || filtro.FechaInicio.HasValue || filtro.FechaFin.HasValue;

            // 1. INTENTAR LEER DEL CACHÉ REDIS (Solo si no hay filtros aplicados)
            if (!usaFiltros)
            {
                var cachedData = await _cache.GetStringAsync(cacheKey);
                if (!string.IsNullOrEmpty(cachedData))
                {
                    var jsonOptions = new JsonSerializerOptions { ReferenceHandler = ReferenceHandler.IgnoreCycles };
                    filtro.Solicitudes = JsonSerializer.Deserialize<List<SolicitudCredito>>(cachedData, jsonOptions)!;
                    return View(filtro);
                }
            }

            // Si hay filtros o no hay caché, consultamos a la Base de Datos
            if (filtro.FechaInicio.HasValue && filtro.FechaFin.HasValue && filtro.FechaInicio > filtro.FechaFin)
            {
                ModelState.AddModelError(string.Empty, "La fecha de inicio no puede ser mayor a la fecha de fin.");
            }

            var query = _context.Solicitudes.Include(s => s.Cliente).Where(s => s.Cliente!.UsuarioId == userId).AsQueryable();

            if (ModelState.IsValid && usaFiltros)
            {
                if (filtro.Estado.HasValue) query = query.Where(s => s.Estado == filtro.Estado);
                if (filtro.MontoMinimo.HasValue) query = query.Where(s => s.MontoSolicitado >= filtro.MontoMinimo);
                if (filtro.MontoMaximo.HasValue) query = query.Where(s => s.MontoSolicitado <= filtro.MontoMaximo);
                if (filtro.FechaInicio.HasValue) query = query.Where(s => s.FechaSolicitud >= filtro.FechaInicio);
                if (filtro.FechaFin.HasValue) query = query.Where(s => s.FechaSolicitud <= filtro.FechaFin);
            }

            filtro.Solicitudes = await query.OrderByDescending(s => s.FechaSolicitud).ToListAsync();

            // 2. GUARDAR EN EL CACHÉ REDIS (Si es la lista general)
            if (!usaFiltros && filtro.Solicitudes.Any())
            {
                var options = new DistributedCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(5));
                var jsonOptions = new JsonSerializerOptions { ReferenceHandler = ReferenceHandler.IgnoreCycles };
                await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(filtro.Solicitudes, jsonOptions), options);
            }

            return View(filtro);
        }

        // GET: Solicitudes/Detalle/5
        public async Task<IActionResult> Detalle(int id)
        {
            var userId = _userManager.GetUserId(User);
            var solicitud = await _context.Solicitudes.Include(s => s.Cliente).FirstOrDefaultAsync(s => s.Id == id && s.Cliente!.UsuarioId == userId);

            if (solicitud == null) return NotFound();

            // 3. GUARDAR EN SESIÓN LA ÚLTIMA SOLICITUD VISITADA
            HttpContext.Session.SetInt32("UltimaSolicitudId", id);

            return View(solicitud);
        }

        public IActionResult Crear()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(RegistroSolicitudViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var userId = _userManager.GetUserId(User);
            var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.UsuarioId == userId);

            if (cliente == null || !cliente.Activo)
            {
                ModelState.AddModelError(string.Empty, "Tu perfil de cliente no está activo.");
                return View(model);
            }

            if (await _context.Solicitudes.AnyAsync(s => s.ClienteId == cliente.Id && s.Estado == EstadoSolicitud.Pendiente))
            {
                ModelState.AddModelError(string.Empty, "Ya tienes una solicitud Pendiente.");
                return View(model);
            }

            if (model.MontoSolicitado > (cliente.IngresosMensuales * 10))
            {
                ModelState.AddModelError(string.Empty, $"El monto no puede superar {cliente.IngresosMensuales * 10:C}.");
                return View(model);
            }

            var nuevaSolicitud = new SolicitudCredito { ClienteId = cliente.Id, MontoSolicitado = model.MontoSolicitado, FechaSolicitud = DateTime.UtcNow, Estado = EstadoSolicitud.Pendiente };
            _context.Solicitudes.Add(nuevaSolicitud);
            await _context.SaveChangesAsync();

            // 4. INVALIDAR EL CACHÉ PARA QUE SE REFLEJE EL NUEVO REGISTRO
            await _cache.RemoveAsync($"solicitudes_{userId}");

            TempData["MensajeExito"] = "Tu solicitud se ha registrado correctamente.";
            return RedirectToAction(nameof(MisSolicitudes));
        }
    }
}