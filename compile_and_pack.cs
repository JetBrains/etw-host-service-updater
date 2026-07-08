using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

// Builds the JetBrains ETW HostService Updater for each Windows runtime and packs
// the result into per-runtime NuGet packages.
//
// Usage: compile_and_pack.cmd [all|prepare-layout|finalize-nuget] [layoutDir]
//   all             prepare the layout AND pack (default)
//   prepare-layout  publish the project into the layout directory only
//   finalize-nuget  pack the (already prepared) layout into .nupkg files only
//
// Run from the repository root.

#region Configuration

const string SourceDir = "JetBrains.Etw.HostService.Updater/src";
const string ProjectFile = SourceDir + "/JetBrains.Etw.HostService.Updater.csproj";
const string TemplateFile = SourceDir + "/Package.nuspec.template";
const string ThirdPartyLibrariesFile = SourceDir + "/third-party-libraries.json";
const string DotNetSdkScript = "dotnet-sdk.cmd";
const string Framework = "net462";

string[] runtimes = ["win-arm64", "win-x64", "win-x86"];

#endregion

#region Parse and validate arguments

if (args.Length > 2)
{
  Usage();
  return 1;
}

string command = (args.Length > 0 ? args[0] : "all").ToLowerInvariant();
string layoutDir = args.Length > 1 ? args[1] : "package-layout";

if (command is not ("all" or "prepare-layout" or "finalize-nuget"))
{
  Usage();
  return 1;
}

RequireFile(DotNetSdkScript);
RequireFile(ProjectFile);
RequireFile(TemplateFile);
RequireFile(ThirdPartyLibrariesFile);

#endregion

#region Resolve context

string packageVersion = GetMsBuildProperty("Version");
string authors = GetMsBuildProperty("Authors");
string copyright = GetMsBuildProperty("Copyright");

string layoutDirFullPath = Path.GetFullPath(layoutDir);
// NuGet's nuspec <file src="..."> expects forward slashes, so normalize the path.
string layoutDirNuspecPath = layoutDirFullPath.Replace('\\', '/');

Console.WriteLine($"Framework:      {Framework}");
Console.WriteLine($"PackageVersion: {packageVersion}");
Console.WriteLine($"Authors:        {authors}");
Console.WriteLine($"Command:        {command}");
Console.WriteLine($"LayoutDir:      {layoutDir}");

#endregion

#region Run

bool shouldPrepareLayout = command is "all" or "prepare-layout";
bool shouldPack = command is "all" or "finalize-nuget";

foreach (string runtime in runtimes)
{
  if (shouldPrepareLayout)
    PrepareLayout(runtime);

  if (shouldPack)
    Pack(runtime);
}

return 0;

#endregion

#region Steps

void PrepareLayout(string runtime)
{
  Run(
    DotNetSdkScript,
    "publish",
    ProjectFile,
    "-f", Framework,
    "-r", runtime,
    "-c", "Release",
    "--self-contained", "true",
    "/p:EnableWindowsTargeting=true");

  string publishDir = Path.Combine(SourceDir, "bin", "Release", Framework, runtime, "publish");
  string toolsDir = Path.Combine(layoutDirFullPath, "tools", runtime);

  if (Directory.Exists(toolsDir))
    Directory.Delete(toolsDir, recursive: true);

  CopyDirectory(publishDir, toolsDir);

  // Copied for every runtime; the destination is identical each time (harmless overwrite).
  File.Copy(
    ThirdPartyLibrariesFile,
    Path.Combine(layoutDirFullPath, "third-party-libraries.json"),
    overwrite: true);
}

void Pack(string runtime)
{
  if (!shouldPrepareLayout)
    RestorePackAssets(runtime);

  Directory.CreateDirectory("out");
  string nuspecFile = Path.Combine("out", $"Package.{runtime}.nuspec");

  string nuspec = File.ReadAllText(TemplateFile)
    .Replace("{{RUNTIME}}", EscapeXml(runtime))
    .Replace("{{PACKAGE_VERSION}}", EscapeXml(packageVersion))
    .Replace("{{AUTHORS}}", EscapeXml(authors))
    .Replace("{{COPYRIGHT}}", EscapeXml(copyright))
    .Replace("{{LAYOUT_DIR}}", EscapeXml(layoutDirNuspecPath));

  File.WriteAllText(nuspecFile, nuspec, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

  Run(
    DotNetSdkScript,
    "msbuild",
    ProjectFile,
    "-nologo",
    "-t:Pack",
    $"-p:NuspecFile={Path.GetFullPath(nuspecFile)}",
    $"-p:PackageOutputPath={Path.GetFullPath("out")}",
    $"-p:TargetFrameworks={Framework}",
    $"-p:TargetFramework={Framework}",
    $"-p:RuntimeIdentifier={runtime}",
    "-p:IsPackable=true",
    "-p:NoBuild=true",
    "-p:IncludeBuildOutput=false",
    "-p:EnableWindowsTargeting=true");

  File.Delete(nuspecFile);
}

void RestorePackAssets(string runtime)
{
  Run(
    DotNetSdkScript,
    "msbuild",
    ProjectFile,
    "-nologo",
    "-t:Restore",
    $"-p:TargetFrameworks={Framework}",
    $"-p:TargetFramework={Framework}",
    $"-p:RuntimeIdentifier={runtime}",
    "-p:EnableWindowsTargeting=true");
}

string GetMsBuildProperty(string propertyName) =>
  Capture(DotNetSdkScript, "msbuild", ProjectFile, "-nologo", $"-getProperty:{propertyName}").Trim();

#endregion

#region Helpers

static void RequireFile(string path)
{
  if (!File.Exists(path))
    throw new FileNotFoundException(
      $"Could not find {path}. Run this script from the repository root.", path);
}

static string EscapeXml(string value) => SecurityElement.Escape(value) ?? "";

static void CopyDirectory(string sourceDir, string targetDir)
{
  Directory.CreateDirectory(targetDir);

  foreach (var sourceFile in Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories))
  {
    string targetFile = Path.Combine(targetDir, Path.GetRelativePath(sourceDir, sourceFile));
    Directory.CreateDirectory(Path.GetDirectoryName(targetFile) ?? targetDir);
    File.Copy(sourceFile, targetFile, overwrite: true);
  }
}

static void Usage() =>
  Console.Error.WriteLine("Usage: compile_and_pack.cmd [all|prepare-layout|finalize-nuget] [layoutDir]");

#endregion

#region Process execution

static void Run(string fileName, params string[] arguments) =>
  Execute(fileName, captureOutput: false, arguments);

static string Capture(string fileName, params string[] arguments) =>
  Execute(fileName, captureOutput: true, arguments);

// Runs a command, streaming its output to the console (Run) or returning it (Capture).
// On a non-zero exit code the whole script terminates with that same code.
static string Execute(string fileName, bool captureOutput, params string[] arguments)
{
  ProcessStartInfo startInfo = CreateStartInfo(fileName, arguments);
  startInfo.UseShellExecute = false;
  startInfo.RedirectStandardOutput = captureOutput;
  startInfo.RedirectStandardError = captureOutput;

  using Process process = Process.Start(startInfo)
                          ?? throw new InvalidOperationException($"Failed to start {fileName}.");

  Task<string> outputTask = captureOutput ? process.StandardOutput.ReadToEndAsync() : Task.FromResult("");
  Task<string> errorTask = captureOutput ? process.StandardError.ReadToEndAsync() : Task.FromResult("");

  process.WaitForExit();

  string output = outputTask.GetAwaiter().GetResult();
  string error = errorTask.GetAwaiter().GetResult();

  if (process.ExitCode == 0)
    return output;

  if (!string.IsNullOrWhiteSpace(output))
    Console.Out.Write(output);

  if (!string.IsNullOrWhiteSpace(error))
    Console.Error.Write(error);

  Environment.Exit(process.ExitCode);
  return "";
}

static ProcessStartInfo CreateStartInfo(string fileName, string[] arguments)
{
  bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
  bool isCmdScript = Path.GetExtension(fileName).Equals(".cmd", StringComparison.OrdinalIgnoreCase);

  ProcessStartInfo startInfo;

  if (isCmdScript)
  {
    startInfo = new ProcessStartInfo(isWindows ? "cmd.exe" : "/bin/sh");

    if (isWindows)
    {
      startInfo.ArgumentList.Add("/d");
      startInfo.ArgumentList.Add("/c");
    }

    startInfo.ArgumentList.Add(fileName);
  }
  else
  {
    startInfo = new ProcessStartInfo(fileName);
  }

  foreach (string argument in arguments)
    startInfo.ArgumentList.Add(argument);

  return startInfo;
}

#endregion
