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
            Logger.LogInformation($"Name: {Name}, Value: {Value}\nConfigure: {Options.Value.Opt}");
        }
    }

    class C
    {
        public string Hit { get; set; } = "HIT";
        public string Opt { get; set; } = "Problem with ConfigurationHelper!";
    }

    static void Main(string[] args)
    {
        var containerBuilder = new ContainerBuilder();

        containerBuilder.AddLogging(static (f) =>
        {
            f.AddConsole();
        });
        containerBuilder.AddOptions();
        containerBuilder
            .RegisterInstance<C>(new() { Hit = "SHOULD KEEP" })
            .AsSelf()
            .SingleInstance();
        containerBuilder.Configure<C>((con, c) =>
        {
            c.Opt =
            $"ConfigurationHelper's first configuration! " +
            $"With Name: {con.Resolve<string>()} " +
            $"With Value: {con.Resolve<object>()} " +
            $"With Hit:{c.Hit}";
        });
        containerBuilder
            .RegisterInstance<C>(new() { Hit = "SHOULD OVERRIDE" })
            .AsSelf()
            .SingleInstance();
        containerBuilder.Configure<C>((con, c) =>
        {
            c.Opt =
            $"ConfigurationHelper's second configuration! " +
            $"With Name: {con.Resolve<string>()} " +
            $"With Value: {con.Resolve<object>()} " +
            $"With Hit:{c.Hit}";
        });
        containerBuilder
            .RegisterInstance<C>(new() { Hit = "SHOULD OVERRIDE AGAIN" })
            .AsSelf()
            .SingleInstance();

        containerBuilder.Register(ctx => "Hello");
        containerBuilder.RegisterInstance<object>(1);
        containerBuilder.RegisterType<A>().AsSelf();
        var container = containerBuilder.Build();

        container.OutputDotGraph();

        using var scope = container.BeginLifetimeScope();
        scope.Resolve<A>().Print();
    }
}
