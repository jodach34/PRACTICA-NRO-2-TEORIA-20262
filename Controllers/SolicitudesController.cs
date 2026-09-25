using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using RabbitMQ.Client;
using System.Text;
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
        private readonly IDistributedCache _cache;
        private readonly IConfiguration _config;

        public SolicitudesController(ApplicationDbContext context, UserManager<IdentityUser> userManager, IDistributedCache cache, IConfiguration config)
        {
            _context = context;
            _userManager = userManager;
            _cache = cache;
            _config = config;
        }

        public async Task<IActionResult> MisSolicitudes(FiltroSolicitudesViewModel filtro)
        {
            var userId = _userManager.GetUserId(User);
            var ultimaVista = HttpContext.Session.GetInt32("UltimaSolicitudId");
            ViewBag.UltimaVista = ultimaVista;

            string cacheKey = $"solicitudes_{userId}";
            bool usaFiltros = filtro.Estado.HasValue || filtro.MontoMinimo.HasValue || filtro.MontoMaximo.HasValue || filtro.FechaInicio.HasValue || filtro.FechaFin.HasValue;

            if (!usaFiltros)
            {
                var cachedData = await _cache.GetStringAsync(cacheKey);
                if (!string.IsNullOrEmpty(cachedData))
                {
                    var jsonOpts = new JsonSerializerOptions { ReferenceHandler = ReferenceHandler.IgnoreCycles };
                    filtro.Solicitudes = JsonSerializer.Deserialize<List<SolicitudCredito>>(cachedData, jsonOpts)!;
                    return View(filtro);
                }
            }

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

            if (!usaFiltros && filtro.Solicitudes.Any())
            {
                var options = new DistributedCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(5));
                var jsonOpts = new JsonSerializerOptions { ReferenceHandler = ReferenceHandler.IgnoreCycles };
                await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(filtro.Solicitudes, jsonOpts), options);
            }

            return View(filtro);
        }

        public async Task<IActionResult> Detalle(int id)
        {
            var userId = _userManager.GetUserId(User);
            var solicitud = await _context.Solicitudes.Include(s => s.Cliente).FirstOrDefaultAsync(s => s.Id == id && s.Cliente!.UsuarioId == userId);

            if (solicitud == null) return NotFound();

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

            // 1. Guardar la solicitud en BD
            var nuevaSolicitud = new SolicitudCredito { ClienteId = cliente.Id, MontoSolicitado = model.MontoSolicitado, FechaSolicitud = DateTime.UtcNow, Estado = EstadoSolicitud.Pendiente };
            _context.Solicitudes.Add(nuevaSolicitud);
            await _context.SaveChangesAsync();

            // 2. Invalidar caché
            await _cache.RemoveAsync($"solicitudes_{userId}");

            // 3. PUBLICAR MENSAJE EN RABBITMQ (RabbitMQ v7+ asíncrono)
            bool notificacionEnviada = false;
            try
            {
                var factory = new ConnectionFactory()
                {
                    Uri = new Uri(_config["RabbitMq:ConnectionString"]!)
                };

                using var connection = await factory.CreateConnectionAsync();
                using var channel = await connection.CreateChannelAsync();

                string queueName = _config["RabbitMq:QueueName"]!;
                
                await channel.QueueDeclareAsync(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

                var mensajeData = new
                {
                    MessageId = Guid.NewGuid().ToString(),
                    SolicitudId = nuevaSolicitud.Id,
                    UsuarioId = userId,
                    FechaEventoUtc = DateTime.UtcNow
                };

                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(mensajeData));
                var properties = new BasicProperties { Persistent = true };

                await channel.BasicPublishAsync(exchange: "", routingKey: queueName, mandatory: true, basicProperties: properties, body: body);
                
                notificacionEnviada = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al encolar notificación: {ex.Message}");
            }

            if (notificacionEnviada)
            {
                TempData["MensajeExito"] = "Tu solicitud se ha registrado y la notificación está en proceso.";
            }
            else
            {
                TempData["MensajeExito"] = "Tu solicitud se registró con éxito, pero hubo un problema al enviar la notificación. (Reenvío manual requerido).";
            }

            return RedirectToAction(nameof(MisSolicitudes));
        }
    }
}