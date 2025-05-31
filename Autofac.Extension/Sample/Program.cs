using Autofac;
using Autofac.Diagnostics.DotGraph;
using Autofac.Extension;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using System;

namespace Sample;

internal class Program
{
    class A
    {
        public required string Name { get; init; }

        public required object Value { get; init; }

        public required IOptions<C> Options { get; init; }

        public required ILogger<A> Logger { get; init; }

        public void Print()
        {
            Logger.LogInformation($"A: {Name}, Value: {Value}, Configure Name: {Options.Value.Name}");
        }
    }

    class C
    {
        public string Name { get; set; } = "Problem with ConfigurationHelper!";
    }

    static void Main(string[] args)
    {
        var containerBuilder = new ContainerBuilder();

        containerBuilder.AddLogging(static (f) =>
        {
            f.AddConsole();
        });
        containerBuilder.AddOptions();
        containerBuilder.Configure<C>((c) =>
        {
            c.Name = "ConfigurationHelper's first configuration!";
        });
        containerBuilder.Configure<C>((c) =>
        {
            c.Name = "ConfigurationHelper's second configuration!";
        });

        containerBuilder.Register(ctx => "Hello");
        containerBuilder.RegisterInstance<object>(1);
        containerBuilder.RegisterType<A>().AsSelf();
        var container = containerBuilder.Build();

        container.OutputDotGraph();

        using var scope = container.BeginLifetimeScope();
        scope.Resolve<A>().Print();
    }
}
