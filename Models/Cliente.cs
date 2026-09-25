using System.ComponentModel.DataAnnotations;

namespace PRACTICA_NRO_2_TEORIA_20262.Models
{
    public class Cliente
    {
        public int Id { get; set; }

        [Required]
        public string UsuarioId { get; set; } // Se vinculará con el ID del IdentityUser

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Los ingresos mensuales deben ser mayores a 0.")]
        public decimal IngresosMensuales { get; set; }

        public bool Activo { get; set; }

        // Propiedad de navegación
        public List<SolicitudCredito>? Solicitudes { get; set; }
    }
}