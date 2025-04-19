using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MessageBroker.RabbitMQ.MassTransit
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMassTransitWithRabbitMq(
            this IServiceCollection services,
            IConfiguration configuration,
            Action<IBusRegistrationConfigurator> configureDelegate = null)
        {
            services.AddMassTransit(cfg =>
            {
                cfg.SetKebabCaseEndpointNameFormatter();

                cfg.UsingRabbitMq((context, config) =>
                {
                    var host = configuration["MessageBroker:RabbitMQ:Host"] ?? "localhost";
                    var username = configuration["MessageBroker:RabbitMQ:Username"] ?? "guest";
                    var password = configuration["MessageBroker:RabbitMQ:Password"] ?? "guest";
                    var virtualHost = configuration["MessageBroker:RabbitMQ:VirtualHost"] ?? "/";

                    config.Host(host, virtualHost, h =>
                    {
                        h.Username(username);
                        h.Password(password);
                    });

                    config.ConfigureEndpoints(context);
                });

                configureDelegate?.Invoke(cfg);
            });

            return services;
        }
    }
}