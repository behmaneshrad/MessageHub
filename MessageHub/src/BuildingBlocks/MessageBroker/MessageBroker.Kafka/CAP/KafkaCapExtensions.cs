using DotNetCore.CAP;
using MessageBroker.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace MessageBroker.Kafka.CAP
{
	public static class KafkaCapExtensions
	{
		public static IServiceCollection AddCAPWithKafka(this IServiceCollection services, IConfiguration configuration)
		{
			// Register CAP with Kafka
			services.AddCap(x =>
			{
				// Use MongoDB for storage
				x.UseMongoDB(configuration.GetConnectionString("MongoDb"));

				// Use Kafka as transport
				var kafkaConfig = configuration.GetSection("MessageBroker:Kafka");
				x.UseKafka(opt =>
				{
					opt.Servers = kafkaConfig["BootstrapServers"];					
				});

				var groupId = kafkaConfig["GroupId"] ?? throw new ArgumentNullException("Kafka GroupId is required");
				x.DefaultGroupName = kafkaConfig["GroupId"];

				// CAP dashboard
				x.UseDashboard();
			});

			// Register Message Broker
			services.AddSingleton<IMessageBroker, KafkaCapMessageBroker>();

			return services;
		}
	}

	public class KafkaCapMessageBroker : IMessageBroker
	{
		private readonly ICapPublisher _capPublisher;

		public KafkaCapMessageBroker(ICapPublisher capPublisher)
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