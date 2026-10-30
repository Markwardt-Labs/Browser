#:package Markwardt.ScriptUtilities@0.2.0
#:property TreatWarningsAsErrors=true

// Builds the browser and installs it for the current user on Linux, so it shows up in the desktop's application
// launcher as "Browser". File-based app (dotnet run) - run from the repo root, e.g. `dotnet run Scripts/Install.cs`.
// Safe to run again to update: the previous install is replaced.

using Markwardt.ScriptUtilities;

string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
string install = Path.Combine(home, ".local", "opt", "browser");
string staging = $"{install}.new";
string applications = Path.Combine(home, ".local", "share", "applications");
string icons = Path.Combine(home, ".local", "share", "icons", "hicolor", "512x512", "apps");

if (Directory.Exists(staging))
{
    Directory.Delete(staging, true);
}

(await Script.Run("dotnet", "publish", "App/App.csproj", "--configuration", "Release", "--runtime", "linux-x64", "--output", staging, "-p:RestoreLockedMode=true")).Verify();

// Replacing the folder, not copying over it: a running copy keeps its already-open files, and nothing stale is left behind.
if (Directory.Exists(install))
{
    Directory.Delete(install, true);
}

Directory.Move(staging, install);

Directory.CreateDirectory(icons);
// A name of its own, not the generic "browser": the desktop caches icons by name and kept showing an older picture under it.
string legacyIcon = Path.Combine(icons, "browser.png");

if (File.Exists(legacyIcon))
{
    File.Delete(legacyIcon);
}

File.Copy("App/Assets/app.png", Path.Combine(icons, "markwardt-browser.png"), true);

Directory.CreateDirectory(applications);
await File.WriteAllTextAsync(Path.Combine(applications, "browser.desktop"), $"""
    [Desktop Entry]
    Type=Application
    Name=Browser
    Comment=A web browser that shows only the page
    Exec={Path.Combine(install, "Markwardt.Browser")} %U
    Icon=markwardt-browser
    Terminal=false
    Categories=Network;WebBrowser;
    StartupWMClass=Markwardt.Browser
    """ + "\n");

// Refreshing the caches is best effort: not every desktop ships these tools, and the entry is picked up on the next login without them.
await Script.Run(false, "update-desktop-database", applications);
await Script.Run(false, "gtk-update-icon-cache", "-f", "-t", Path.Combine(home, ".local", "share", "icons", "hicolor"));

Script.Log($"Installed Browser to {install}.");
