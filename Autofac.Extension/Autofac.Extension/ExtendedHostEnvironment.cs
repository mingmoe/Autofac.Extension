using System.Diagnostics;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Utopia.Core;

public sealed class ExtendedHostEnvironment : IHostEnvironment
{
    public ExtendedHostEnvironment(string applicationName, string contentRootPath, string? environmentName = null)
    {
        if (environmentName is null)
        {
#if DEBUG
            EnvironmentName = Environments.Development;
#else
            EnvironmentName = Environments.Production;
#endif
        }
        else
        {
            EnvironmentName = environmentName;
        }
        ApplicationName = applicationName;
        ContentRootPath = contentRootPath;
        ContentRootFileProvider = new PhysicalFileProvider(contentRootPath);
    }

    public string EnvironmentName { get; set; }

    public string ApplicationName { get; set; }

    public string ContentRootPath { get; set; }

    public IFileProvider ContentRootFileProvider { get; set; }

}