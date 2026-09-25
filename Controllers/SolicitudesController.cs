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
    }
}