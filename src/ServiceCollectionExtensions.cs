using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using System;

namespace Sufficit.Telephony.EventsPanel
{
    public static class ServiceCollectionExtensions
    {

        #region SERVER CONFIGURATION

        /// <summary>
        /// Add EventsPanel Server services - Singleton background service that connects to AMI and processes events
        /// Used on ASP.NET Core Server side (Blazor Server, API Server, etc.)
        /// Handles AMI connection, event processing, and broadcasting to clients via SignalR
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configuration">Configuration instance</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddEventsPanelServer(this IServiceCollection services, IConfiguration configuration)
        {
            // Configure options from appsettings.json
            services.Configure<EventsPanelServiceCards>(configuration.GetSection(EventsPanelServiceCards.SECTIONNAME));
            services.Configure<EventsPanelServiceOptions>(configuration.GetSection(EventsPanelServiceOptions.SECTIONNAME));
            services.Configure<AMIHubClientOptions>(configuration.GetSection(AMIHubClientOptions.SECTIONNAME));
            services.Configure<EventsPanelCardOptions>(configuration.GetSection(EventsPanelCardOptions.SECTIONNAME));

            // Register AMIHubClient as Singleton (persistent connection to Asterisk AMI)
            // Only used on server side - clients should NOT connect directly to AMI
            services.AddSingleton<AMIHubClient>();

            // Register EventsPanelService as Singleton (background service)
            services.TryAddSingleton<EventsPanelService>();
            services.TryAddSingleton<IEventsPanelService>(provider => provider.GetRequiredService<EventsPanelService>());

            // Register hosted service to start EventsPanelService automatically on application startup
            services.AddHostedService<EventsPanelHostedService>();

            // Note: SignalR Hub and Broadcast Service are registered in the server's Startup.cs
            // This keeps the EventsPanel library independent of ASP.NET Core hosting
    
            return services;
        }

        #endregion
        #region CLIENT CONFIGURATION

        /// <summary>
        /// Add EventsPanel Client services - Scoped service for user-specific filtering and UI components
        /// Used on Blazor WebAssembly client side or per-request scenarios
        /// For now, this is the same as the original implementation (connects directly to AMI)
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configuration">Configuration instance</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddEventsPanelClient(this IServiceCollection services, IConfiguration configuration)
        {
            // Configure client-side options
            services.Configure<EventsPanelServiceCards>(configuration.GetSection(EventsPanelServiceCards.SECTIONNAME));
            services.Configure<EventsPanelServiceOptions>(configuration.GetSection(EventsPanelServiceOptions.SECTIONNAME));
            services.Configure<AMIHubClientOptions>(configuration.GetSection(AMIHubClientOptions.SECTIONNAME));
            services.Configure<EventsPanelCardOptions>(configuration.GetSection(EventsPanelCardOptions.SECTIONNAME));

            // Register AMIHubClient as Scoped (per user/session)
            services.AddScoped<AMIHubClient>();

            // Register EventsPanelService as Scoped (per user/session)
            services.TryAddScoped<EventsPanelService>();
            services.TryAddScoped<IEventsPanelService>(provider => provider.GetRequiredService<EventsPanelService>());

            return services;
        }

        #endregion

    }
}
