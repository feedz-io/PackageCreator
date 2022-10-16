//////////////////////////////////////////////////////////////////////
// TOOLS
//////////////////////////////////////////////////////////////////////
#tool "nuget:?package=GitVersion.CommandLine&version=4.0.0-beta0011"

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////
var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

///////////////////////////////////////////////////////////////////////////////
// GLOBAL VARIABLES
///////////////////////////////////////////////////////////////////////////////
var artifactsDir = "./artifacts/";
GitVersion gitVersionInfo;
string nugetVersion;

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////
Setup(context =>
{
    gitVersionInfo = GitVersion(new GitVersionSettings {
        OutputType = GitVersionOutput.Json
    });

  
    nugetVersion = gitVersionInfo.NuGetVersion;

    if(BuildSystem.IsRunningOnAppVeyor)
        BuildSystem.AppVeyor.UpdateBuildVersion(nugetVersion);

    Information("Building Feedz.PackageCreator v{0}", nugetVersion);
    Information("Informational Version {0}", gitVersionInfo.InformationalVersion);
});

Teardown(context =>
{
    Information("Finished running tasks.");
});

//////////////////////////////////////////////////////////////////////
//  PRIVATE TASKS
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() => {
		CleanDirectory(artifactsDir);
		CleanDirectory("./output");
		CleanDirectories("./**/bin");
		CleanDirectories("./**/obj");
	});

Task("Restore")
    .IsDependentOn("Clean")
    .Does(() => {
		DotNetCoreRestore(".");
    });

Task("Build")
    .IsDependentOn("Restore")
    .IsDependentOn("Clean")
    .Does(() => {
		DotNetCoreBuild(".", new DotNetCoreBuildSettings
		{
			Configuration = configuration,
			ArgumentCustomization = args => args.Append($"/p:Version={nugetVersion}")
		});
	});

Task("Publish")
    .IsDependentOn("Build")
    .Does(() => {
        DotNetCorePublish("./PackageCreator", new DotNetCorePublishSettings
        {
            Configuration = configuration,
            OutputDirectory = "./output",
            ArgumentCustomization = args => args.Append($"/p:Version={nugetVersion}")
        });
    });

Task("Pack")
    .IsDependentOn("Build")
    .Does(() => {
        DotNetCorePack("./PackageCreator", new DotNetCorePackSettings
        {
            Configuration = configuration,
            NoBuild = true,
            OutputDirectory = artifactsDir,
            ArgumentCustomization = args => args.Append($"/p:Version={nugetVersion}")
        });
    });

Task("Push")
    .WithCriteria(BuildSystem.IsRunningOnAppVeyor)
    .IsDependentOn("Pack")
    .Does(() =>
    {
        NuGetPush($"{artifactsDir}Feedz.PackageCreator.{nugetVersion}.nupkg", new NuGetPushSettings {
            Source = "https://f.feedz.io/feedz-io/public/nuget",
            ApiKey = EnvironmentVariable("FeedzApiKey")
        });
    });

Task("Zip")
    .IsDependentOn("Publish")
    .Does(() => {
        Zip("./output", $"{artifactsDir}/PackageCreator.zip");
    });

Task("Default")
    .IsDependentOn("Push")
    .IsDependentOn("Zip");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////
RunTarget(target);
