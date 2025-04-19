using DotNetCore.CAP;
using MessageBroker.Common.Interfaces;
using MessageBroker.Common.Models;
using MessageBroker.Common.Outbox;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace MessageBroker.RabbitMQ.CAP
{
	public static class RabbitMQCapExtensions
	{
		public static IServiceCollection AddCAPWithRabbitMq(this IServiceCollection services, IConfiguration configuration)
		{           

            // Register CAP with RabbitMQ
            services.AddCap(x =>
			{
				// Use MongoDB for storage
				//x.UseMongoDB(configuration.GetConnectionString("MongoDbs"));

				x.UseSqlServer(configuration.GetConnectionString("SqlDb"));

				// Use RabbitMQ as transport
				var rabbitMqConfig = configuration.GetSection("MessageBroker:RabbitMQ");
				x.UseRabbitMQ(opt =>
				{
					opt.ExchangeName = rabbitMqConfig["Exchange"];
					opt.HostName = rabbitMqConfig["Host"];
					opt.UserName = rabbitMqConfig["Username"];
					opt.Password = rabbitMqConfig["Password"];
					opt.VirtualHost = rabbitMqConfig["VirtualHost"];
				});

				// CAP dashboard
				x.UseDashboard();
			});

			// Register Message Broker
			services.AddSingleton<IMessageBroker, RabbitMQCapMessageBroker>();

			return services;
		}
	}

	public class RabbitMQCapMessageBroker : IMessageBroker
	{
		private readonly ICapPublisher _capPublisher;

		public RabbitMQCapMessageBroker(ICapPublisher capPublisher)
		{
			_capPublisher = capPublisher;
		}

        public async Task PublishAsync<T>(T message, string topic = null) where T : class
        {
            topic = topic ?? typeof(T).Name;

            // سریالایز دستی با تنظیمات خاص
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            var messageStr = JsonSerializer.Serialize(message, options);
            var headers = new Dictionary<string, string>();

            await _capPublisher.PublishAsync(topic, messageStr, headers);
        }
    }
}