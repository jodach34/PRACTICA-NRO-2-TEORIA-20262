using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PRACTICA_NRO_2_TEORIA_20262.Models
{
    public class FiltroSolicitudesViewModel
    {
        public EstadoSolicitud? Estado { get; set; }
        
        [Range(0, double.MaxValue, ErrorMessage = "El monto no puede ser negativo.")]
        public decimal? MontoMinimo { get; set; }
        
        [Range(0, double.MaxValue, ErrorMessage = "El monto no puede ser negativo.")]
        public decimal? MontoMaximo { get; set; }
        
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        
        // Aquí guardaremos los resultados de la búsqueda
        public List<SolicitudCredito> Solicitudes { get; set; } = new List<SolicitudCredito>();
    }
}