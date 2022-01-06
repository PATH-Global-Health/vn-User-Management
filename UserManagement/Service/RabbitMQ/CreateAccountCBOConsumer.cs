using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Data.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Service.Interfaces;

namespace Service.RabbitMQ
{
    public class CreateAccountCBOConsumer:BackgroundService
    {
        private IConnection connection;
        private IModel channel;
        private readonly IServiceScopeFactory _scopeFactory;
        private EventingBasicConsumer consumer;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CreateAccountCBOConsumer> _logger;
        string queue = "CreateAccountCBODev1";

        public CreateAccountCBOConsumer(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<CreateAccountCBOConsumer> logger)
        {
            _scopeFactory = scopeFactory;
            _configuration = configuration;
            _logger = logger;
            InitRabbitMQ();
        }

        private void InitRabbitMQ()
        {
            try
            {

                var factory = new ConnectionFactory();
                _configuration.Bind("RabbitMqConnection", factory);

                connection = factory.CreateConnection();
                channel = connection.CreateModel();

                channel.QueueDeclare(queue: queue, durable: false,
                    exclusive: false, autoDelete: false, arguments: null);

                //channel.BasicQos(0, 1, false);

                _logger.LogInformation($"-RabbitMQ Queue created: {queue}");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "RabbitMQ queue create fail.");
            }
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();
            var consumer = new EventingBasicConsumer(channel);

            channel.BasicConsume(queue: queue,
                autoAck: false, consumer: consumer);

            consumer.Received += async (model, ea) =>
            {
                string response = null;

                var body = ea.Body.ToArray();
                var props = ea.BasicProperties;
                var replyProps = channel.CreateBasicProperties();
                replyProps.CorrelationId = props.CorrelationId;

                try
                {
                    var message = Encoding.UTF8.GetString(body);
                    var result = await CreateAccountCBO(message);
                    response = JsonConvert.SerializeObject(result);
                }
                catch (Exception e)
                {
                    var result = new ResultModel();
                    result.Succeed = false;
                    result.ErrorMessage = e.InnerException != null ? e.InnerException.Message + "\n" + e.StackTrace : e.Message + "\n" + e.StackTrace;
                    response = JsonConvert.SerializeObject(result);
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

        private async Task<ResultModel> CreateAccountCBO(string message)
        {
            var messageAccountDTO = JsonConvert.DeserializeObject<CBOCreateModel>(message);
            using (var scope = _scopeFactory.CreateScope())
            {
                IUserService _userService = scope.ServiceProvider.GetRequiredService<IUserService>();
                var result = await _userService.CreateAccountCBO(messageAccountDTO);
                return result;
            }
        }


    }
}
