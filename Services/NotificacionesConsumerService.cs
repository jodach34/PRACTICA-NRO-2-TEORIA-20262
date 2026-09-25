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
        private IChannel? _channel;

        public NotificacionesConsumerService(IConfiguration config, IServiceScopeFactory scopeFactory)
        {
            _config = config;
            _scopeFactory = scopeFactory;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            if (!_config.GetValue<bool>("RabbitMq:ConsumerEnabled")) return;

            var factory = new ConnectionFactory() { Uri = new Uri(_config["RabbitMq:ConnectionString"]!) };
            _connection = await factory.CreateConnectionAsync(cancellationToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
            
            await _channel.QueueDeclareAsync(
                queue: _config["RabbitMq:QueueName"]!, 
                durable: true, 
                exclusive: false, 
                autoDelete: false, 
                arguments: null, 
                cancellationToken: cancellationToken
            );

            await base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_channel == null) return;

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var data = JsonSerializer.Deserialize<JsonElement>(message);

                    string messageId = data.GetProperty("MessageId").GetString()!;
                    int solicitudId = data.GetProperty("SolicitudId").GetInt32();
                    string usuarioId = data.GetProperty("UsuarioId").GetString()!;

                    using var scope = _scopeFactory.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

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
                    
                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                }
                catch (JsonException)
                {
                    await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error procesando mensaje MQ: {ex.Message}");
                    await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                }
            };

            await _channel.BasicConsumeAsync(
                queue: _config["RabbitMq:QueueName"]!, 
                autoAck: false, 
                consumer: consumer, 
                cancellationToken: stoppingToken
            );
        }

        public override async void Dispose()
        {
            if (_channel != null) await _channel.DisposeAsync();
            if (_connection != null) await _connection.DisposeAsync();
            base.Dispose();
        }
    }
}