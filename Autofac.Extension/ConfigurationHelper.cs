using Autofac.Core.Lifetime;
using Microsoft.Extensions.Configuration;
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
    private class OptionsWrapper<T> : IOptions<T>
        where T : class, new()
    {
        public T Value { get; init; }

        public OptionsWrapper(IOptionsFactory<T> newOptions)
        {
            Value = newOptions.Create(Options.DefaultName);
        }
    }

    private sealed class DefaultConfigureNamedOptions<T> : IConfigureNamedOptions<T>
        where T : class, new()
    {
        public void Configure(T options)
        {
            return;
        }
        public void Configure(string? name, T options)
        {
            return;
        }
    }
    private sealed class DefaultPostConfigureOptions<T> : IPostConfigureOptions<T>
        where T : class, new()
    {
        public void PostConfigure(string? name, T options)
        {
            return;
        }
    }

    private sealed class DefaultValidateOptions<T> : IValidateOptions<T>
        where T : class, new()
    {
        public ValidateOptionsResult Validate(string? name, T options)
        {
            return ValidateOptionsResult.Success;
        }
    }

    public static void AddOptions(this ContainerBuilder builder)
    {
        builder.RegisterGeneric(typeof(OptionsWrapper<>)).As(typeof(IOptions<>));
        builder.RegisterGeneric(typeof(DefaultConfigureNamedOptions<>))
            .As(typeof(IConfigureOptions<>))
            .As(typeof(IConfigureNamedOptions<>));
        builder.RegisterGeneric(typeof(DefaultPostConfigureOptions<>)).As(typeof(IPostConfigureOptions<>));
        builder.RegisterGeneric(typeof(DefaultValidateOptions<>)).As(typeof(IValidateOptions<>));
        builder.RegisterGeneric(typeof(OptionsCache<>)).As(typeof(IOptionsMonitorCache<>));
        builder.RegisterGeneric(typeof(OptionsFactory<>)).As(typeof(IOptionsFactory<>));
        builder.RegisterGeneric(typeof(OptionsMonitor<>)).As(typeof(IOptionsMonitor<>));
        // the IOptionsChangeTokenSource should from the user
        // if user did not provide it,the optionsMonitor will not work
        // more detailed,the optionsMonitor will only be constructed by the IConfiguration
        // so user should bind the IConfiguration to the options
    }

    public static void Configure<TOptions>(
        this ContainerBuilder builder,
        string? name,
        Action<TOptions> configure)
        where TOptions : class, new()
    {
        builder.Register<IConfigureNamedOptions<TOptions>>((context) =>
        {
            return new ConfigureNamedOptions<TOptions>(name, configure);
        })
            .As<IConfigureOptions<TOptions>>()
            .SingleInstance();
    }

    public static void Configure<TOptions, UserObj>(
        this ContainerBuilder builder,
        string? name,
        Action<TOptions, UserObj> configure)
        where TOptions : class, new()
        where UserObj : class
    {
        builder.Register<IConfigureNamedOptions<TOptions>>((context) =>
        {
            return new ConfigureNamedOptions<TOptions, UserObj>(name, context.Resolve<UserObj>(), configure);
        })
            .As<IConfigureOptions<TOptions>>()
            .SingleInstance();
    }

    public static void PostConfigure<TOptions, UserObj>(
        this ContainerBuilder builder,
        string? name,
        Action<TOptions, UserObj> configure)
        where TOptions : class, new()
        where UserObj : class
    {
        builder.Register<IPostConfigureOptions<TOptions>>((context) =>
        {
            return new PostConfigureOptions<TOptions, UserObj>(name, context.Resolve<UserObj>(), configure);
        })
            .As<IPostConfigureOptions<TOptions>>()
            .SingleInstance();
    }
    public static void PostConfigure<TOptions>(
        this ContainerBuilder builder,
        string? name,
        Action<TOptions> configure)
        where TOptions : class, new()
    {
        builder.Register<IPostConfigureOptions<TOptions>>((context) =>
        {
            return new PostConfigureOptions<TOptions>(name, configure);
        })
            .As<IPostConfigureOptions<TOptions>>()
            .SingleInstance();
    }

    public static void ValidateOptions<TOptions, UserObj>(
        this ContainerBuilder builder,
        string? name,
        Func<TOptions, UserObj, bool> configure,
        string failureMessage)
        where TOptions : class, new()
        where UserObj : class
    {
        builder.Register<IValidateOptions<TOptions>>((context) =>
        {
            return new ValidateOptions<TOptions, UserObj>(name, context.Resolve<UserObj>(), configure, failureMessage);
        })
            .As<IValidateOptions<TOptions>>()
            .SingleInstance();
    }
    public static void ValidateOptions<TOptions>(
        this ContainerBuilder builder,
        string? name,
        Func<TOptions, bool> configure,
        string failureMessage)
        where TOptions : class, new()
    {
        builder.Register<IValidateOptions<TOptions>>((context) =>
        {
            return new ValidateOptions<TOptions>(name, configure, failureMessage);
        })
            .As<IValidateOptions<TOptions>>()
            .SingleInstance();
    }
}
