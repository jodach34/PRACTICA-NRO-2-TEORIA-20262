using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PRACTICA_NRO_2_TEORIA_20262.Data;
using PRACTICA_NRO_2_TEORIA_20262.Models;

namespace PRACTICA_NRO_2_TEORIA_20262.Controllers
{
    // Asegura que solo usuarios que han iniciado sesión puedan entrar aquí
    [Authorize] 
    public class SolicitudesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public SolicitudesController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Solicitudes/MisSolicitudes
        public async Task<IActionResult> MisSolicitudes(FiltroSolicitudesViewModel filtro)
        {
            // Validación server-side: No aceptar rangos de fechas inválidos
            if (filtro.FechaInicio.HasValue && filtro.FechaFin.HasValue && filtro.FechaInicio > filtro.FechaFin)
            {
                ModelState.AddModelError(string.Empty, "La fecha de inicio no puede ser mayor a la fecha de fin.");
            }

            // Obtener el ID del usuario actual
            var userId = _userManager.GetUserId(User);
            
            // Consulta inicial: solo las solicitudes del cliente autenticado
            var query = _context.Solicitudes
                .Include(s => s.Cliente)
                .Where(s => s.Cliente!.UsuarioId == userId)
                .AsQueryable();

            if (ModelState.IsValid)
            {
                // Aplicar filtros dinámicamente
                if (filtro.Estado.HasValue)
                    query = query.Where(s => s.Estado == filtro.Estado);

                if (filtro.MontoMinimo.HasValue)
                    query = query.Where(s => s.MontoSolicitado >= filtro.MontoMinimo);

                if (filtro.MontoMaximo.HasValue)
                    query = query.Where(s => s.MontoSolicitado <= filtro.MontoMaximo);

                if (filtro.FechaInicio.HasValue)
                    query = query.Where(s => s.FechaSolicitud >= filtro.FechaInicio);

                if (filtro.FechaFin.HasValue)
                    query = query.Where(s => s.FechaSolicitud <= filtro.FechaFin);
            }

            // Ejecutar la consulta y guardar en el ViewModel
            filtro.Solicitudes = await query.OrderByDescending(s => s.FechaSolicitud).ToListAsync();

            return View(filtro);
        }

        // GET: Solicitudes/Detalle/5
        public async Task<IActionResult> Detalle(int id)
        {
            var userId = _userManager.GetUserId(User);
            
            // Buscar la solicitud asegurando que pertenezca al usuario actual
            var solicitud = await _context.Solicitudes
                .Include(s => s.Cliente)
                .FirstOrDefaultAsync(s => s.Id == id && s.Cliente!.UsuarioId == userId);

            if (solicitud == null) return NotFound();

            return View(solicitud);
        }
        // GET: Solicitudes/Crear
        public IActionResult Crear()
        {
            return View();
        }

        // POST: Solicitudes/Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(RegistroSolicitudViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = _userManager.GetUserId(User);
            
            // Buscar al cliente asociado a este usuario
            var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.UsuarioId == userId);

            // 1. Validar que el cliente exista y esté activo
            if (cliente == null || !cliente.Activo)
            {
                ModelState.AddModelError(string.Empty, "Tu perfil de cliente no está activo para solicitar créditos.");
                return View(model);
            }

            // 2. Validar que no tenga una solicitud Pendiente
            var tienePendiente = await _context.Solicitudes
                .AnyAsync(s => s.ClienteId == cliente.Id && s.Estado == EstadoSolicitud.Pendiente);
            
            if (tienePendiente)
            {
                ModelState.AddModelError(string.Empty, "Ya tienes una solicitud de crédito en estado Pendiente. No puedes registrar otra.");
                return View(model);
            }

            // 3. Validar que el monto no supere 10 veces los ingresos mensuales
            if (model.MontoSolicitado > (cliente.IngresosMensuales * 10))
            {
                ModelState.AddModelError(string.Empty, $"El monto solicitado no puede superar 10 veces tus ingresos mensuales (Máximo permitido: {cliente.IngresosMensuales * 10:C}).");
                return View(model);
            }

            // 4. Registrar la nueva solicitud en estado Pendiente
            var nuevaSolicitud = new SolicitudCredito
            {
                ClienteId = cliente.Id,
                MontoSolicitado = model.MontoSolicitado,
                FechaSolicitud = DateTime.UtcNow,
                Estado = EstadoSolicitud.Pendiente
            };

            _context.Solicitudes.Add(nuevaSolicitud);
            await _context.SaveChangesAsync();

            // Mandar mensaje de éxito a la vista
            TempData["MensajeExito"] = "Tu solicitud de crédito se ha registrado correctamente y está pendiente de evaluación.";
            
            return RedirectToAction(nameof(MisSolicitudes));
        }
    }
}