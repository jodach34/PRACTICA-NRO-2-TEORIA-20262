using System.ComponentModel.DataAnnotations;

namespace PRACTICA_NRO_2_TEORIA_20262.Models
{
    public class RegistroSolicitudViewModel
    {
        [Required(ErrorMessage = "El monto solicitado es obligatorio.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El monto solicitado debe ser mayor a 0.")]
        [Display(Name = "Monto a Solicitar")]
        public decimal MontoSolicitado { get; set; }
    }
}