// See https://aka.ms/new-console-template for more information
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Codeworx.PackageVersion;
using CommandLine;
using NuGet.Commands;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Credentials;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

internal class Program
{
    private async static Task Main(string[] args)
    {

        await Parser.Default.ParseArguments<PackageOptions>(args)
                  .WithParsedAsync<PackageOptions>(ProcessAsync);
    }

    private static async Task ProcessAsync(PackageOptions options)
    {

        Console.WriteLine(Directory.GetCurrentDirectory());

        var nonInteractive = false;
        //var nonInteractive = true;

        DefaultCredentialServiceUtility.SetupDefaultCredentialService(NullLogger.Instance, nonInteractive);

        ISettings settings = Settings.LoadDefaultSettings(
            Directory.GetCurrentDirectory(),
            configFileName: null,
            machineWideSettings: new XPlatMachineWideSetting());
        PackageSourceProvider sourceProvider = new PackageSourceProvider(settings);

        NextVersionResult result = new NextVersionResult(options.Major, options.Minor, options.BuildNumberOffset, 0, options.PreRelease);

        foreach (var item in sourceProvider.LoadPackageSources())
        {
            if (item.IsEnabled)
            {
                var repository = Repository.Factory.GetCoreV3(item);
                var resource = await repository.GetResourceAsync<PackageSearchResource>();

                if (resource != null)
                {
                    var data = await resource.SearchAsync(
                        options.Package,
                        new SearchFilter(includePrerelease: true),
                        0,
                        100,
                        NullLogger.Instance, default);

                    var package = data.Where(p => p.Identity.Id == options.Package).FirstOrDefault();

                    NuGetVersion? lastStable = null;
                    NuGetVersion? latest = null;

                    if (package != null)
                    {
                        var versions = await package.GetVersionsAsync();

                        var matchingVersions = versions
                            .Where(p => p.Version.Major == options.Major && p.Version.Minor == options.Minor)
                            .Select(p => p.Version)
                            .ToList();

                        lastStable = matchingVersions
                            .Where(p => !p.IsPrerelease)
                            .OrderByDescending(p => p)
                            .FirstOrDefault();

                        latest = matchingVersions
                            .OrderByDescending(p => p)
                            .FirstOrDefault();
                    }

                    var nextPath = lastStable?.Patch + 1 ?? 0;
                    var nextBuild = options.BuildNumberOffset;

                    if (latest != null)
                    {
                        var download = await repository.GetResourceAsync<DownloadResource>();
                        var path = Path.GetTempPath();
                        var tempPathDll = Path.GetTempFileName() + ".dll";
                        var context = new PackageDownloadContext(NullSourceCacheContext.Instance, path, true);

                        var identity = new PackageIdentity(package!.Identity.Id, latest);

                        using var packageDownload = await download.GetDownloadResourceResultAsync(identity, context, null, NullLogger.Instance, default);
                        using var archive = new ZipArchive(packageDownload.PackageStream, ZipArchiveMode.Read);

                        var entry = archive.Entries
                                            .Where(p => p.Name.Equals($"{options.Package}.dll", StringComparison.OrdinalIgnoreCase))
                                            .FirstOrDefault();

                        if (entry != null)
                        {
                            entry.ExtractToFile(tempPathDll);

                            var versionInfo = FileVersionInfo.GetVersionInfo(tempPathDll);
                            var build = versionInfo.FileBuildPart;
                            nextBuild = build + 1;
                        }
                    }

                    if (result == null || result.Revision < nextPath)
                    {
                        result = new NextVersionResult(options.Major, options.Minor, nextBuild, nextPath, options.PreRelease);
                    }
                }
            }
        }

        WriteResult(result, options.OutputFormatter);
    }

    private static void WriteResult(NextVersionResult result, OutputFormatter outputFormatter)
    {
        if (outputFormatter == OutputFormatter.Text)
        {
            Console.WriteLine($"Major: {result.Major}");
            Console.WriteLine($"Minor: {result.Minor}");
            Console.WriteLine($"Build: {result.Build}");
            Console.WriteLine($"Revision: {result.Revision}");
            Console.WriteLine($"FileVersion: {result.FileVersion}");
            Console.WriteLine($"PackageVersion: {result.PackageVersion}");
        }
        else if (outputFormatter == OutputFormatter.Json)
        {
            var output = JsonSerializer.SerializeToUtf8Bytes(
                result,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                    WriteIndented = true,
                });

            Console.WriteLine(System.Text.Encoding.UTF8.GetString(output));
        }
        else if (outputFormatter == OutputFormatter.DevOps)
        {
            Console.WriteLine($"##vso[task.setvariable variable=PackageVersionMajor;]{result.Major}");
            Console.WriteLine($"##vso[task.setvariable variable=PackageVersionMinor;]{result.Minor}");
            Console.WriteLine($"##vso[task.setvariable variable=PackageVersionBuild;]{result.Build}");
            Console.WriteLine($"##vso[task.setvariable variable=PackageVersionRevision;]{result.Revision}");
            Console.WriteLine($"##vso[task.setvariable variable=PackageVersionFileVersion;]{result.FileVersion}");
            Console.WriteLine($"##vso[task.setvariable variable=PackageVersion;]{result.PackageVersion}");
        }
    }
}