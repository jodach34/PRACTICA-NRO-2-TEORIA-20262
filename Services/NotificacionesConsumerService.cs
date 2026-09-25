using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using PRACTICA_NRO_2_TEORIA_20262.Data;
using PRACTICA_NRO_2_TEORIA_20262.Models;

namespace PRACTICA_NRO_2_TEORIA_20262.Services
{
    public class NotificacionesConsumerService : BackgroundService
    {
        private readonly IConfiguration _config;
        private readonly IServiceScopeFactory _scopeFactory;
        private IConnection? _connection;
        private IModel? _channel;

        public NotificacionesConsumerService(IConfiguration config, IServiceScopeFactory scopeFactory)
        {
            _config = config;
            _scopeFactory = scopeFactory;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            // Validamos si el consumidor está activado en appsettings.json
            if (!_config.GetValue<bool>("RabbitMq:ConsumerEnabled")) return Task.CompletedTask;

            var factory = new ConnectionFactory() { Uri = new Uri(_config["RabbitMq:ConnectionString"]!) };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            
            _channel.QueueDeclare(queue: _config["RabbitMq:QueueName"]!, durable: true, exclusive: false, autoDelete: false, arguments: null);

            return base.StartAsync(cancellationToken);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_channel == null) return Task.CompletedTask;

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var data = JsonSerializer.Deserialize<JsonElement>(message);

                    string messageId = data.GetProperty("MessageId").GetString()!;
                    int solicitudId = data.GetProperty("SolicitudId").GetInt32();
                    string usuarioId = data.GetProperty("UsuarioId").GetString()!;

                    // El DbContext se debe instanciar por cada mensaje en un BackgroundService
                    using var scope = _scopeFactory.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    // Validación de duplicados (Idempotencia)
                    bool existe = context.Notificaciones.Any(n => n.MessageId == messageId);
                    if (!existe)
                    {
                        var notificacion = new Notificacion
                        {
                            MessageId = messageId,
                            SolicitudId = solicitudId,
                            UsuarioId = usuarioId,
                            Texto = "Recibimos tu solicitud de crédito y está pendiente de evaluación",
                            FechaProcesamientoUtc = DateTime.UtcNow
                        };
                        context.Notificaciones.Add(notificacion);
                        await context.SaveChangesAsync();
                    }
                    
                    // Confirmación manual (ACK) solo DESPUÉS de guardar en BD
                    _channel.BasicAck(ea.DeliveryTag, multiple: false);
                }
                catch (JsonException)
                {
                    // Mensaje inválido, rechazar sin reencolar
                    _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
                }
                catch (Exception ex)
                {
                    // Error general, rechazar sin reencolar para evitar loops infinitos (según reglas del examen)
                    Console.WriteLine($"Error procesando mensaje MQ: {ex.Message}");
                    _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
                }
            };

            _channel.BasicConsume(queue: _config["RabbitMq:QueueName"]!, autoAck: false, consumer: consumer);
            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
            base.Dispose();
        }
    }
}