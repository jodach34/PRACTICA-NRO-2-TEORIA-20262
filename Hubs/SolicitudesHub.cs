using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace PRACTICA_NRO_2_TEORIA_20262.Hubs
{
    // Protegemos el Hub para que solo usuarios autenticados puedan conectarse
    [Authorize] 
    public class SolicitudesHub : Hub
    {
        // No necesitamos métodos aquí adentro porque el servidor 
        // empujará los mensajes directamente al usuario usando su ID.
    }
}