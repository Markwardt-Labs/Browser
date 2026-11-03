#:package Markwardt.ScriptUtilities@0.2.0
#:property TreatWarningsAsErrors=true

// Writes a CEF runtime with H.264 and AAC playback into a folder. The CEF runtime from NuGet is Chromium's default
// build, which cannot decode them, so most video sites that offer nothing else (Steam's trailers pair AV1 with AAC)
// show "Video format not supported". This provides the files of a build of the same Chromium version made with
// proprietary codecs (a community build, not an official CEF one), downloaded once into ~/.cache and checked against a
// pinned SHA-256. The folder is either a build output, whose loose runtime files it replaces, or a new folder to
// publish with (`-p:CefRuntimeFolder=<folder>`), which packs it into the executable. Linux x64 only; elsewhere it does
// nothing. Patent licensing for the codecs is the responsibility of whoever distributes the result. File-based app
// (dotnet run) - run from the repo root, e.g. `dotnet run Scripts/Codecs.cs -- App/bin/Debug/net10.0/linux-x64`.

using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Markwardt.ScriptUtilities;

string url = "https://github.com/obs-rust/cef/releases/download/8078/cef_binary_8078_linux_x86_64.tar.xz";
string sha256 = "fb36df11bc8280c9cb666732becc1fe267f833bb964dbb13d69cad33488171dc";
string root = "cef_binary_8078_linux_x86_64";

if (args.Length != 1)
{
    Script.Error("Usage: dotnet run Scripts/Codecs.cs -- <folder to write the runtime files to>");
    return 1;
}

if (!OperatingSystem.IsLinux() || RuntimeInformation.OSArchitecture != Architecture.X64)
{
    Script.Log("Codec runtime is only available for Linux x64; nothing written.");
    return 0;
}

string cache = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cache", "Markwardt.Browser");
string archive = Path.Combine(cache, Path.GetFileName(url));
Directory.CreateDirectory(cache);

if (!File.Exists(archive) || await Hash(archive) != sha256)
{
    Script.Log($"Downloading {url} ...");
    string partial = $"{archive}.part";

    using (HttpClient client = new() { Timeout = TimeSpan.FromMinutes(20) })
    await using (Stream source = await client.GetStreamAsync(url))
    await using (FileStream target = File.Create(partial))
    {
        await source.CopyToAsync(target);
    }

    if (await Hash(partial) != sha256)
    {
        File.Delete(partial);
        Script.Error("The downloaded codec runtime does not match its pinned SHA-256; not using it.");
        return 1;
    }

    File.Move(partial, archive, true);
}

string extracted = Path.Combine(cache, "extracted");

if (Directory.Exists(extracted))
{
    Directory.Delete(extracted, true);
}

Directory.CreateDirectory(extracted);
(await Script.Run("tar", "-xJf", archive, "-C", extracted, "--strip-components=1", $"{root}/Release", $"{root}/Resources")).Verify();

Copy(Path.Combine(extracted, "Release"), args[0]);
Copy(Path.Combine(extracted, "Resources"), args[0]);
Directory.Delete(extracted, true);
Script.Log($"Wrote the runtime with H.264 and AAC playback to {args[0]}.");
return 0;

async Task<string> Hash(string path)
{
    await using FileStream stream = File.OpenRead(path);
    return Convert.ToHexStringLower(await SHA256.HashDataAsync(stream));
}

void Copy(string from, string to)
{
    Directory.CreateDirectory(to);

    foreach (string file in Directory.GetFiles(from))
    {
        File.Copy(file, Path.Combine(to, Path.GetFileName(file)), true);
    }

    foreach (string folder in Directory.GetDirectories(from))
    {
        Copy(folder, Path.Combine(to, Path.GetFileName(folder)));
    }
}
