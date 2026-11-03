#:package Markwardt.ScriptUtilities@0.2.0
#:property TreatWarningsAsErrors=true

// Runs the app locally. File-based app (dotnet run) - run from the repo root, e.g. `dotnet run Scripts/Run.cs`.
// Builds first so that H.264 and AAC support (see Codecs.cs) can be added to the build output before it starts.

using Markwardt.ScriptUtilities;

RunResult built = await Script.Run(false, "dotnet", "build", "App/App.csproj", "-getProperty:TargetDir");
built.Verify();

if ((await Script.Run(false, "dotnet", "run", "Scripts/Codecs.cs", "--", built.Output.Trim().Split('\n').Last().Trim())).IsFailure)
{
    Script.Error("Running without H.264/AAC support, see the output above.");
}

(await Script.Run("dotnet", "run", "--project", "App/App.csproj", "--no-build")).Verify();
