namespace MessageBroker.Infrastructure.Extensions
{
	using MessageBroker.Common.Interfaces;
	using MessageBroker.Common.Outbox;
	using MessageBroker.Infrastructure.Persistence;
	using Microsoft.EntityFrameworkCore;
	using Microsoft.Extensions.DependencyInjection;

	public static class ServiceCollectionExtensions
	{
		public static IServiceCollection AddMessageBroker(this IServiceCollection services)
		{
			services.AddScoped<IMessageBroker, OutboxMessageBroker>();
			services.AddScoped<IOutboxStore, DbOutboxStore>();
			services.AddHostedService<OutboxProcessor>();

			return services;
		}
	}
}