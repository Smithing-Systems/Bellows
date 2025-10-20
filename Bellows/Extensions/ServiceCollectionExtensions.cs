using System.Reflection;
using Bellows.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Bellows.Extensions;

/// <summary>
/// Extension methods for registering Bellows with DI container
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Bellows services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="assemblies">Assemblies to scan for handlers</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddBellows(this IServiceCollection services, params Assembly[] assemblies)
    {
        return AddBellows(services, null, assemblies);
    }

    /// <summary>
    /// Adds Bellows services to the service collection with configuration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureOptions">Action to configure mediator options</param>
    /// <param name="assemblies">Assemblies to scan for handlers</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddBellows(
        this IServiceCollection services,
        Action<MediatorOptions>? configureOptions,
        params Assembly[] assemblies)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        var options = new MediatorOptions();
        configureOptions?.Invoke(options);

        services.AddSingleton(options);
        services.AddTransient<IMediator>(sp => new Mediator(sp, sp.GetService<MediatorOptions>()));

        var assembliesToScan = assemblies?.Length > 0
            ? assemblies
            : new[] { Assembly.GetCallingAssembly() };

        foreach (var assembly in assembliesToScan)
        {
            RegisterHandlers(services, assembly);
        }

        return services;
    }

    /// <summary>
    /// Adds a pipeline behavior to the service collection
    /// </summary>
    /// <typeparam name="TBehavior">The pipeline behavior type</typeparam>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddPipelineBehavior<TBehavior>(this IServiceCollection services)
        where TBehavior : class
    {
        return AddPipelineBehavior(services, typeof(TBehavior));
    }

    /// <summary>
    /// Adds a pipeline behavior to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="behaviorType">The pipeline behavior type</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddPipelineBehavior(this IServiceCollection services, Type behaviorType)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
        if (behaviorType == null)
            throw new ArgumentNullException(nameof(behaviorType));

        var interfaces = behaviorType.GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>))
            .ToList();

        if (interfaces.Count == 0)
            throw new ArgumentException($"Type {behaviorType.Name} does not implement IPipelineBehavior<,>", nameof(behaviorType));

        foreach (var @interface in interfaces)
        {
            services.AddTransient(@interface, behaviorType);
        }

        return services;
    }

    private static void RegisterHandlers(IServiceCollection services, Assembly assembly)
    {
        var types = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .ToList();

        // Register request handlers
        foreach (var type in types)
        {
            var interfaces = type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>))
                .ToList();

            foreach (var @interface in interfaces)
            {
                services.AddTransient(@interface, type);
            }
        }

        // Register notification handlers
        foreach (var type in types)
        {
            var interfaces = type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(INotificationHandler<>))
                .ToList();

            foreach (var @interface in interfaces)
            {
                services.AddTransient(@interface, type);
            }
        }

        // Register pipeline behaviors (only concrete closed types, not open generic types)
        foreach (var type in types)
        {
            // Skip open generic types - they need to be registered explicitly
            if (type.IsGenericTypeDefinition)
                continue;

            var interfaces = type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>))
                .ToList();

            foreach (var @interface in interfaces)
            {
                services.AddTransient(@interface, type);
            }
        }
    }
}
