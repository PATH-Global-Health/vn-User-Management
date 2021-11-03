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

namespace Service.RabbitMQ.RPC.Servers
{
    public class PermissionRpcServer : BackgroundService
    {
        private readonly IConnection connection;
        private readonly IModel channel;
        private readonly EventingBasicConsumer consumer;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;

        public PermissionRpcServer(IServiceScopeFactory scopeFactory, IConfiguration configuration)
        {
            _scopeFactory = scopeFactory;
            _configuration = configuration;

            #region Initialize Connection
            var factory = new ConnectionFactory();
            _configuration.Bind("RabbitMqConnection", factory);

            connection = factory.CreateConnection();
            channel = connection.CreateModel();

            channel.QueueDeclare(queue: "ValidatePermissionDemo", durable: false,
              exclusive: false, autoDelete: false, arguments: null);
            channel.BasicQos(0, 1, false);
            consumer = new EventingBasicConsumer(channel);
            channel.BasicConsume(queue: "ValidatePermissionDemo",
              autoAck: false, consumer: consumer);
            #endregion
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
                    var jsonMessage = Encoding.UTF8.GetString(body);
                    var messageObject = JsonConvert.DeserializeObject<PermissionCheckMessageModel>(jsonMessage);

                    var validationResult = CheckPermission(messageObject);
                    if (validationResult.Succeed)
                    {
                        response = "True";
                    }
                    else if (!validationResult.Succeed && string.IsNullOrEmpty(validationResult.ErrorMessage))
                    {
                        response = "False";
                    }
                    else
                    {
                        response = validationResult.ErrorMessage;
                    }
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

        ResultModel CheckPermission(PermissionCheckMessageModel model)
        {
            var result = new ResultModel();
            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionsService>();

                    result = permissionService.Validate(new ResourcePermissionValidationModel { ApiPath = model.ApiPath, Method = model.Method }, model.UserId);

                    return result;
                }
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.Message;
            }
            return result;
        }

        class PermissionCheckMessageModel
        {
            public string UserId { get; set; }
            public string ApiPath { get; set; }
            public string Method { get; set; }
        }
    }
}
