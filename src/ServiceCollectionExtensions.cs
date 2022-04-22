using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sufficit.Telephony.EventsPanel
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddEventsPanel(this IServiceCollection services)
        {
            var provider = services.BuildServiceProvider();
            var configuration = provider.GetRequiredService<IConfiguration>();

            services.Configure<EventsPanelServiceOptions>(configuration.GetSection(EventsPanelServiceOptions.SECTIONNAME));
            services.Configure<AMIHubClientOptions>(configuration.GetSection(AMIHubClientOptions.SECTIONNAME));
            services.Configure<EventsPanelCardOptions>(configuration.GetSection(EventsPanelCardOptions.SECTIONNAME));
            services.AddSingleton<AMIHubClient>();
            services.AddSingleton<EventsPanelService>();
            services.AddHostedService(x => x.GetRequiredService<EventsPanelService>());

            return services;
        }
    }
}
