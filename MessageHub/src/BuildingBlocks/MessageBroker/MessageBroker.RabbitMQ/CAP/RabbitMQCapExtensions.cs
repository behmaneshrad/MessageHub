using DotNetCore.CAP;
using MessageBroker.Common.Interfaces;
using MessageBroker.Common.Models;
using MessageBroker.Common.Outbox;
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
				x.UseMongoDB(configuration.GetConnectionString("MongoDb"));

				// Use RabbitMQ as transport
				var rabbitMqConfig = configuration.GetSection("MessageBroker:RabbitMQ");
				x.UseRabbitMQ(opt =>
				{
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
			// CAP already implements the outbox pattern internally
			topic = topic ?? typeof(T).Name;
			await _capPublisher.PublishAsync(topic, message);
		}
	}
}