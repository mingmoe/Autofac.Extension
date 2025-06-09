using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Utopia.Core;

/// <summary>
/// Designed for Autofac and other high-level features.
/// </summary>
public sealed class ExtendedHostBuilder : IHostBuilder
{
    public ExtendedHostBuilder(IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(environment);
        Dictionary<object, object> properties = [];
        Properties = properties;
        Environment = environment;
        Context = new HostBuilderContext(Properties)
        {
            Configuration = null!,
            HostingEnvironment = Environment,
        };
    }

    private bool _built = false;

    private HostBuilderContext Context { get; }

    private IHostEnvironment Environment { get; }

    public IDictionary<object, object> Properties { get; }

    private List<Action<HostBuilderContext, IServiceCollection>> ConfigureServicesDelegates { get; } = new();
    private List<Action<IConfigurationBuilder>> ConfigureHostConfigurationDelegates { get; } = new();
    private List<Action<HostBuilderContext, IConfigurationBuilder>> ConfigureAppConfigurationDelegates { get; } = new();
    private List<Action<HostBuilderContext, ContainerBuilder>> ConfigureContainerDelegates { get; } = new();

    public ContainerBuilder Builder { get; } = new();

    public IHostBuilder ConfigureHostConfiguration(Action<IConfigurationBuilder> configureDelegate)
    {
        ConfigureHostConfigurationDelegates.Add(configureDelegate);
        return this;
    }

    public IHostBuilder ConfigureAppConfiguration(
        Action<HostBuilderContext, IConfigurationBuilder> configureDelegate)
    {
        ConfigureAppConfigurationDelegates.Add(configureDelegate);
        return this;
    }

    public IHostBuilder ConfigureServices(Action<HostBuilderContext, IServiceCollection> configureDelegate)
    {
        ArgumentNullException.ThrowIfNull(configureDelegate);
        ConfigureServicesDelegates.Add(configureDelegate);
        return this;
    }

    public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(
        IServiceProviderFactory<TContainerBuilder> factory) where TContainerBuilder : notnull
    {
        // should not call this
        // we use Autofac
        throw new NotSupportedException();
    }

    public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(
        Func<HostBuilderContext, IServiceProviderFactory<TContainerBuilder>> factory) where TContainerBuilder : notnull
    {
        // should not call this
        // we use Autofac
        throw new NotSupportedException();
    }

    public void RegisterHost<T>() where T : ExtendedHost
    {
        ConfigureContainerDelegates.Add((_, builder) =>
        {
            builder.RegisterType<T>().As<ExtendedHost>().As<IHost>().As<IHostApplicationLifetime>().SingleInstance();
        });
    }

    public IHostBuilder ConfigureContainer<TContainerBuilder>(
        Action<HostBuilderContext, TContainerBuilder> configureDelegate)
    {
        if (configureDelegate is Action<HostBuilderContext, ContainerBuilder> action)
        {
            ConfigureContainerDelegates.Add(action);
        }
        else
        {
            // should not call this
            // we use Autofac
            throw new NotSupportedException();
        }

        return this;
    }

    public IHost Build()
    {
        // ****************
        // If you want to modify this file,
        // https://github.com/dotnet/runtime/blob/main/src/libraries/Microsoft.Extensions.Hosting/src/HostBuilder.cs
        // mey be a good place
        // ****************
        if (_built)
        {
            throw new InvalidOperationException("The host has already been built.");
        }
        _built = true;
        IContainer container;
        // build system configuration first
        {
            ConfigurationBuilder builder = new();
            foreach (var action in ConfigureHostConfigurationDelegates)
            {
                action(builder);
            }
            Context.Configuration = builder.Build();
        }
        // build application configuration then
        {
            ConfigurationBuilder builder = new();
            foreach (var action in ConfigureAppConfigurationDelegates)
            {
                action(Context, builder);
            }
            Context.Configuration = builder.Build();
        }
        // build services lastly
        {
            ContainerBuilder containerBuilder = new();
            ServiceCollection serviceCollection = new();

            foreach (var action in ConfigureServicesDelegates)
            {
                action(Context, serviceCollection);
            }

            containerBuilder.Populate(serviceCollection);

            foreach (var action in ConfigureContainerDelegates)
            {
                action(Context, containerBuilder);
            }

            // inject things we need
            containerBuilder.RegisterInstance(Environment).As<IHostEnvironment>().SingleInstance();
            containerBuilder.RegisterInstance(Context).As<HostBuilderContext>().SingleInstance();
            containerBuilder.RegisterInstance(Context.Configuration).As<IConfiguration>().SingleInstance();

            container = containerBuilder.Build();
        }
        return container.Resolve<ExtendedHost>();
    }
}