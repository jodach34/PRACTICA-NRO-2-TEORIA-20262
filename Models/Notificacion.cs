using System.ComponentModel.DataAnnotations;

namespace PRACTICA_NRO_2_TEORIA_20262.Models
{
    public class Notificacion
    {
        public int Id { get; set; }
        
        [Required]
        public string MessageId { get; set; } = string.Empty; 
        
        public int SolicitudId { get; set; }
        
        public string UsuarioId { get; set; } = string.Empty;
        
        public string Texto { get; set; } = string.Empty;
        
        public DateTime FechaProcesamientoUtc { get; set; }
    }
}