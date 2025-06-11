using Autofac;
using Autofac.Diagnostics.DotGraph;
using Autofac.Extension;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using System;
using Utopia.Core;

namespace Sample;

public class Program
{
    class A
    {
        public required string Name { get; init; }

        public required object Value { get; init; }

        public required IOptions<C> Options { get; init; }

        public required ILogger<A> Logger { get; init; }

        public void Print()
        {
            Logger.LogInformation($"Name: {Name}, Value: {Value}\nConfigure: {Options.Value.Opt}");
        }
    }

    class C
    {
        public string Hit { get; set; } = "You should can not see this";
        public string Opt { get; set; } = "You should can not see this";
    }

    class Host : ExtendedHost
    {
        public Host(ILifetimeScope container) : base(container)
        {
        }

        protected override Task Main()
        {
            Console.WriteLine("Run in thread: " + Thread.CurrentThread.Name);
            Container.Resolve<A>().Print();
            return Task.CompletedTask;
        }

        protected override Task Start(CancellationToken abortStart)
        {
            return Task.CompletedTask;
        }

        protected override Task Stop(CancellationToken stopGracefullyShutdown)
        {
            return Task.CompletedTask;
        }
    }

    static void HostTest()
    {
        var builder = new ExtendedHostBuilder(
            new ExtendedHostEnvironment("test", ".", "Development"));

        builder.ConfigureServices((_, collection) =>
        {
            collection.AddSingleton(ctx => "Hello,but wrong");
            collection.AddSingleton(ctx => "Hello,OK!");
            collection.AddSingleton<object>(1);
            collection.AddSingleton<object>(2);
        });

        builder.ConfigureContainer((_, containerBuilder) =>
        {
            containerBuilder.AddLogging(static (f) =>
            {
                f.AddConsole(opt =>
                {

                }).AddFilter(null, LogLevel.Trace);
            });
            containerBuilder.AddOptions();
            containerBuilder
                .RegisterInstance<C>(new() { Hit = "You should can not see this,too" })
                .AsSelf()
                .SingleInstance();
            containerBuilder.Configure<C>((con, c) =>
            {
                c.Opt =
                $"You should can not see this!!!" +
                $"With Name: {con.Resolve<string>()} " +
                $"With Value: {con.Resolve<object>()} " +
                $"With Hit:{c.Hit}";
            });
            containerBuilder
                .RegisterInstance<C>(new() { Hit = "You should see this,OK!" })
                .AsSelf()
                .SingleInstance();
            containerBuilder.Configure<C>((con, c) =>
            {
                c.Opt =
                $"You should can see this,OK!" +
                $"With Name: {con.Resolve<string>()} " +
                $"With Value: {con.Resolve<object>()} " +
                $"With Hit:{c.Hit}";
            });
            containerBuilder.RegisterType<A>().AsSelf();
        });

        builder.ConfigureIContainer((_, container) =>
        {
            container.OutputDotGraph();
        });

        builder.RegisterHost<Host>();

        var host = (ExtendedHost)builder.Build();

        host.StartInCurrentThread();
        host.StopAsync().Wait();
    }

    static void Main(string[] args)
    {
        Thread.CurrentThread.Name = "Main Thread + Start Thread";
        Console.WriteLine("------- Host Test -------");
        HostTest();
    }

    public const string ExpectedOutput = @"
------- Host Test -------
Resolve Utopia.Core.ExtendedHost[graph]
trce: Utopia.Core.ExtendedHost[0]
      start application in current thread
trce: Utopia.Core.ExtendedHost[0]
      trigger Microsoft.Extensions.Hosting.IHostedLifecycleService.StartingAsync
Resolve System.Collections.Generic.IEnumerable`1[[Microsoft.Extensions.Hosting.IHostedLifecycleService, Microsoft.Extensions.Hosting.Abstractions, Version=9.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60]][graph]
trce: Utopia.Core.ExtendedHost[0]
      trigger Microsoft.Extensions.Hosting.IHostedService.StartAsync
Resolve System.Collections.Generic.IEnumerable`1[[Microsoft.Extensions.Hosting.IHostedService, Microsoft.Extensions.Hosting.Abstractions, Version=9.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60]][graph]
trce: Utopia.Core.ExtendedHost[0]
      trigger Microsoft.Extensions.Hosting.IHostedLifecycleService.StartedAsync
Resolve System.Collections.Generic.IEnumerable`1[[Microsoft.Extensions.Hosting.IHostedLifecycleService, Microsoft.Extensions.Hosting.Abstractions, Version=9.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60]][graph]
trce: Utopia.Core.ExtendedHost[0]
      cancel CancellationToken:IHostApplicationLifetime.ApplicationStarted
Run in thread: Main Thread + Start Thread
Resolve Sample.Program+A[graph]
info: Sample.Program.A[0]
      Name: Hello,OK!, Value: 2
Configure: You should can see this,OK!With Name: Hello,OK! With Value: 2 With Hit:You should see this,OK!
trce: Utopia.Core.ExtendedHost[0]
      stop application
trce: Utopia.Core.ExtendedHost[0]
      cancel CancellationToken:IHostApplicationLifetime.ApplicationStopping
trce: Utopia.Core.ExtendedHost[0]
      trigger Microsoft.Extensions.Hosting.IHostedLifecycleService.StoppingAsync
Resolve System.Collections.Generic.IEnumerable`1[[Microsoft.Extensions.Hosting.IHostedLifecycleService, Microsoft.Extensions.Hosting.Abstractions, Version=9.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60]][graph]
trce: Utopia.Core.ExtendedHost[0]
      trigger Microsoft.Extensions.Hosting.IHostedService.StopAsync
Resolve System.Collections.Generic.IEnumerable`1[[Microsoft.Extensions.Hosting.IHostedService, Microsoft.Extensions.Hosting.Abstractions, Version=9.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60]][graph]
trce: Utopia.Core.ExtendedHost[0]
      trigger Microsoft.Extensions.Hosting.IHostedLifecycleService.StoppedAsync
Resolve System.Collections.Generic.IEnumerable`1[[Microsoft.Extensions.Hosting.IHostedLifecycleService, Microsoft.Extensions.Hosting.Abstractions, Version=9.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60]][graph]
trce: Utopia.Core.ExtendedHost[0]
      cancel CancellationToken:IHostApplicationLifetime.ApplicationStopped
";
}
