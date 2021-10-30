using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using System;
using System.Text;

namespace Service.RabbitMQ
{
    public interface IVerifyUserPublisher : IDisposable
    {
        void Publish(string message);
        void Close();
    }

    public class VerifyUserPublisher : IVerifyUserPublisher
    {
        private IConnection connection;
        private IModel channel;
        private readonly IConfiguration _configuration;
        private ConnectionFactory _factory;

        public VerifyUserPublisher(IConfiguration configuration)
        {
            _configuration = configuration;
            _factory = new ConnectionFactory();
            // bind configuaration from appsettings.json
            _configuration.Bind("RabbitMqConnection", _factory);
            //_factory.ClientProvidedName = "Examination Publisher 2" + " | Examination";
        }

        public void Publish(string message)
        {
            string exchange = "SetStatusProfile2";
            //string exchange = "VerifyUserExchange";
            try
            {
                connection = _factory.CreateConnection();
                channel = connection.CreateModel();
                var props = channel.CreateBasicProperties();

                channel.ExchangeDeclare(exchange: exchange, type: ExchangeType.Fanout);
                //props.AppId = ApplicationType.Examination.ToString("d");
                var body = Encoding.UTF8.GetBytes(message);
                channel.BasicPublish(exchange: exchange,
                                     routingKey: "",
                                     basicProperties: props,
                                     body: body);
            }
            catch (Exception) { }
            finally
            {
            }
        }

        public void Close()
        {
            if (channel != null)
            {
                channel.Close();
            }

            if (connection != null)
            {
                connection.Close();
            }
        }

        public void Dispose()
        {
            Close();
        }
    }
}
