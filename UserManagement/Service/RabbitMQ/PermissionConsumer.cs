using Data.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Service.Interfaces;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Service.RabbitMQ
{
    public class PermissionConsumer : BackgroundService
    {
        private IConnection connection;
        private IModel channel;
        private readonly IServiceScopeFactory _scopeFactory;
        private EventingBasicConsumer consumer;
        private readonly IConfiguration _configuration;


        public PermissionConsumer(IServiceScopeFactory scopeFactory, IConfiguration configuration)
        {
            _scopeFactory = scopeFactory;
            _configuration = configuration;
            InitRabbitMQ();
        }

        private void InitRabbitMQ()
        {
            var factory = new ConnectionFactory();
            _configuration.Bind("RabbitMqConnection", factory);

            connection = factory.CreateConnection();
            channel = connection.CreateModel();

            channel.QueueDeclare(queue: "ValidatePermission", durable: false,
              exclusive: false, autoDelete: false, arguments: null);
            channel.BasicQos(0, 1, false);
            consumer = new EventingBasicConsumer(channel);
            channel.BasicConsume(queue: "ValidatePermission",
              autoAck: false, consumer: consumer);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();
            consumer.Received += (model, ea) =>
            {
                string response = null;

                var body = ea.Body.ToArray();
                var props = ea.BasicProperties;
                var replyProps = channel.CreateBasicProperties();
                replyProps.CorrelationId = props.CorrelationId;

                try
                {
                    //Get Message
                    var message = Encoding.UTF8.GetString(body);

                    #region Do some logics here
                    var validationMessageModel = JsonConvert.DeserializeObject<MQPermissionValidationMessageModel>(message);

                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionsService>();

                        var validationResult = permissionService.Validate(validationMessageModel.ValidationModel, validationMessageModel.UserId);
                        response = validationResult.Succeed.ToString();
                    }
                    #endregion

                }
                catch (Exception e)
                {
                    response = e.Message;
                }
                finally
                {
                    var responseBytes = Encoding.UTF8.GetBytes(response);
                    channel.BasicPublish(exchange: "", routingKey: props.ReplyTo,
                      basicProperties: replyProps, body: responseBytes);
                    channel.BasicAck(deliveryTag: ea.DeliveryTag,
                      multiple: false);
                }

            };
            return Task.CompletedTask;
        }


    }
}
