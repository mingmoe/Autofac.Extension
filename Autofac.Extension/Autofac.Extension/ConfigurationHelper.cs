using Microsoft.Extensions.Options;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Autofac.Extension.ConfigurationHelper;

namespace Autofac.Extension;
public static class ConfigurationHelper
{
    internal class Configuration<T>
    {
        public required Action<T> ConfigureAction { get; init; }
    }

    public static void AddOptions(this ContainerBuilder builder)
    {
        builder.RegisterGeneric(typeof(OptionsWrapper<>)).As(typeof(IOptions<>));
    }

    public static void Configure<T>(this ContainerBuilder builder, Action<T> configure)
        where T : class, new()
    {
        builder.RegisterInstance<Configuration<T>>(new Configuration<T>()
        {
            ConfigureAction = configure
        }).AsSelf().SingleInstance();

        builder.Register<T>((context) =>
        {
            T options = new();
            context.Resolve<IEnumerable<Configuration<T>>>()
                .ToList()
                .ForEach(c => c.ConfigureAction(options));
            return options;
        }).AsSelf().SingleInstance();
    }
}
