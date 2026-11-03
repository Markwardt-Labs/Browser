# Project

Documentation for this repository's own tooling and workflow.

## Scripts

`Scripts/` holds file-based C# apps (`dotnet run Scripts/<Name>.cs`, from the root) and tasks for
automated repository actions. Only what a developer runs by hand is in the folder itself; what
those are built from is in `Scripts/Support/`.

- `Scripts/Run.cs` - builds the app, adds codec support to the build output, and runs it locally.
- `Scripts/Install.cs` - publishes the app as one executable with codec support, and installs it for the
  current user on Linux, so it appears as "Browser" in the application launcher. It replaces `~/.local/opt/browser` with the new
  build, installs the icon, and writes `~/.local/share/applications/browser.desktop`; running it again updates
  the install.
- `Scripts/Codecs.cs` - writes the CEF runtime files of a same-version build that has H.264 and AAC (Linux x64
  only) into a folder, downloaded once into `~/.cache/Markwardt.Browser` and checked against a pinned SHA-256:
  over a build output for `Run.cs`, or as the runtime to pack when publishing. The release workflow does not run
  it, so the zips on GitHub still have Chromium's default codecs.
- `Scripts/Publish.cs` - cuts a release (see Publishing below). A manual, human-only action.

## Publishing

Run `Scripts/Publish.cs` locally to cut a release:

1. It prompts for the version to publish (e.g. `1.2.3`) - `App/App.csproj` carries no `<Version>` of its
   own, so this is what actually gets built and published.
2. It creates the GitHub Release (and its underlying tag) for that version locally via `gh release
   create` - done locally because a repo ruleset blocks the default `GITHUB_TOKEN` from creating tags.
3. It dispatches `build.yml`'s `workflow_dispatch` trigger with the version as input. The workflow
   verifies the dispatcher has Admin permission on the repo, refuses to run from anything but `main`,
   publishes self-contained single-file executables for Windows and Linux (x64) with the Chromium runtime
   packed inside, zips each with `THIRD-PARTY-NOTICES.txt`, and uploads the zips as release assets.
4. It waits for that run to finish, rolling the release/tag back if the workflow fails, so a failed
   publish never leaves one behind. If it can't confirm the run happened at all, it leaves the release
   in place instead, rather than risk deleting one that's still running.

## Workflows

- `.github/workflows/build.yml` - builds, verifies formatting (`dotnet format --verify-no-changes`,
  never applies fixes), and runs tests on every push/PR to `main`; its `publish` job (see Publishing
  above) only runs on `workflow_dispatch`, gated on the dispatcher having Admin permission on the repo
  and the run being on `main`.
- `.github/workflows/codeql.yml` - CodeQL security analysis on push/PR to `main` and a weekly schedule.
