using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Utopia.Core;

/// <summary>
/// Designed for Autofac and other high-level features.
/// </summary>
public abstract class ExtendedHost(IContainer container) : IHost, IHostApplicationLifetime
{
    private readonly CancellationTokenSource _startedCtx = new();
    private readonly CancellationTokenSource _stoppingCts = new();
    private readonly CancellationTokenSource _stoppedCts = new();

    public CancellationToken ApplicationStarted => _startedCtx.Token;
    public CancellationToken ApplicationStopping => _stoppingCts.Token;
    public CancellationToken ApplicationStopped => _stoppedCts.Token;

    private bool _disposed = false;

    public required ILogger<ExtendedHost> Logger { protected get; init; }

    public IContainer Container => container;

    public IServiceProvider Services { get; } = new AutofacServiceProvider(container);

    protected async Task StartServer<T>(IHttpApplication<T> application, CancellationToken abortStart) where T : notnull
    {
        var servers = container.Resolve<IEnumerable<IServer>>();
        foreach (var server in servers)
        {
            await server.StartAsync(application, abortStart).ConfigureAwait(false);
        }
    }

    protected async Task StopServer(CancellationToken stopGracefullyShutdown)
    {
        var servers = container.Resolve<IEnumerable<IServer>>();
        foreach (var server in servers)
        {
            await server.StopAsync(stopGracefullyShutdown).ConfigureAwait(false);
        }
    }

    private async Task BeforeStartServices(CancellationToken abortStart)
    {
        Logger.LogTrace("trigger {interface}.{lifetime}", nameof(IHostedLifecycleService), nameof(IHostedLifecycleService.StartingAsync));
        var services = container.Resolve<IEnumerable<IHostedLifecycleService>>();
        foreach (var service in services)
        {
            await service.StartingAsync(abortStart).ConfigureAwait(false);
        }
    }

    private async Task StartServices(CancellationToken abortStart)
    {
        Logger.LogTrace("trigger {interface}.{lifetime}", nameof(IHostedService), nameof(IHostedService.StartAsync));
        var services = container.Resolve<IEnumerable<IHostedService>>();
        foreach (var service in services)
        {
            await service.StartAsync(abortStart).ConfigureAwait(false);
        }
    }

    private async Task AfterStartServices(CancellationToken abortStart)
    {
        Logger.LogTrace("trigger {interface}.{lifetime}", nameof(IHostedLifecycleService), nameof(IHostedLifecycleService.StartedAsync));
        var services = container.Resolve<IEnumerable<IHostedLifecycleService>>();
        foreach (var service in services)
        {
            await service.StartedAsync(abortStart).ConfigureAwait(false);
        }
    }

    private async Task BeforeStopServices(CancellationToken stopGracefullyShutdown)
    {
        Logger.LogTrace("trigger {interface}.{lifetime}", nameof(IHostedLifecycleService), nameof(IHostedLifecycleService.StoppingAsync));
        var services = container.Resolve<IEnumerable<IHostedLifecycleService>>();
        foreach (var service in services)
        {
            await service.StoppingAsync(stopGracefullyShutdown).ConfigureAwait(false);
        }
    }

    private async Task StopServices(CancellationToken stopGracefullyShutdown)
    {
        Logger.LogTrace("trigger {interface}.{lifetime}", nameof(IHostedService), nameof(IHostedService.StopAsync));
        var services = container.Resolve<IEnumerable<IHostedService>>();
        foreach (var service in services)
        {
            await service.StopAsync(stopGracefullyShutdown).ConfigureAwait(false);
        }
    }

    private async Task AfterStopServices(CancellationToken stopGracefullyShutdown)
    {
        Logger.LogTrace("trigger {interface}.{lifetime}", nameof(IHostedLifecycleService),
            nameof(IHostedLifecycleService.StoppedAsync));
        var services = container.Resolve<IEnumerable<IHostedLifecycleService>>();
        foreach (var service in services)
        {
            await service.StoppedAsync(stopGracefullyShutdown).ConfigureAwait(false);
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    ~ExtendedHost()
    {
        Dispose(disposing: false);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            CancellationTokenSource source = new();
            source.Cancel();
            WrappedStop(source.Token).Wait(CancellationToken.None);
            container.Dispose();
        }

        _disposed = true;
    }

    protected abstract Task Start(CancellationToken abortStart);

    protected abstract Task Main();

    protected abstract Task Stop(CancellationToken stopGracefullyShutdown);

    /// <summary>
    /// Start the application.
    /// </summary>
    public Task StartAsync(CancellationToken abortStart = new())
    {
        return Task.Run(async () =>
        {
            Logger.LogTrace("start application");
            try
            {
                await BeforeStartServices(abortStart).ConfigureAwait(false);
                await StartServices(abortStart).ConfigureAwait(false);
                await Start(abortStart).ConfigureAwait(false);
                await AfterStartServices(abortStart).ConfigureAwait(false);
                Logger.LogTrace("cancel CancellationToken:{interface}.{token}", nameof(IHostApplicationLifetime),
                                nameof(IHostApplicationLifetime.ApplicationStarted));
                await _startedCtx.CancelAsync().ConfigureAwait(false);
                abortStart.ThrowIfCancellationRequested();
                await Main().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.LogCritical("uncaught exception:{exception}", ex);
                throw;
            }
        }, CancellationToken.None);
    }

    private async Task WrappedStop(CancellationToken stopGracefullyShutdown)
    {
        Logger.LogTrace("stop application");
        try
        {
            Logger.LogTrace("cancel CancellationToken:{interface}.{token}", nameof(IHostApplicationLifetime), nameof(IHostApplicationLifetime.ApplicationStopping));
            await _stoppingCts.CancelAsync().ConfigureAwait(false);
            await BeforeStopServices(stopGracefullyShutdown).ConfigureAwait(false);
            await StopServices(stopGracefullyShutdown).ConfigureAwait(false);
            await Stop(stopGracefullyShutdown).ConfigureAwait(false);
            await AfterStopServices(stopGracefullyShutdown).ConfigureAwait(false);
        }
        finally
        {
            Logger.LogTrace("cancel CancellationToken:{interface}.{token}", nameof(IHostApplicationLifetime), nameof(IHostApplicationLifetime.ApplicationStopped));
            await _stoppedCts.CancelAsync().ConfigureAwait(false);
        }
    }

    public async Task StopAsync(CancellationToken stopGracefullyShutdown = new())
    {
        await WrappedStop(stopGracefullyShutdown).ConfigureAwait(false);
    }

    public void StopApplication()
    {
        StopAsync(CancellationToken.None).GetAwaiter().GetResult();
    }
}