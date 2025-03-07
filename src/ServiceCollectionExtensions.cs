using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace Sufficit.Telephony.EventsPanel
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddEventsPanel(this IServiceCollection services)
        {
            var provider = services.BuildServiceProvider();
            var configuration = provider.GetRequiredService<IConfiguration>();
            
            services.Configure<EventsPanelServiceCards>(configuration.GetSection(EventsPanelServiceCards.SECTIONNAME));
            services.Configure<EventsPanelServiceOptions>(configuration.GetSection(EventsPanelServiceOptions.SECTIONNAME));
            services.Configure<AMIHubClientOptions>(configuration.GetSection(AMIHubClientOptions.SECTIONNAME));
            services.Configure<EventsPanelCardOptions>(configuration.GetSection(EventsPanelCardOptions.SECTIONNAME));

            services.AddScoped<AMIHubClient>();

            services.TryAddScoped<EventsPanelService>();
            services.TryAddScoped<IEventsPanelService>((provider) => provider.GetRequiredService<EventsPanelService>());

            return services;
        }
    }
}
