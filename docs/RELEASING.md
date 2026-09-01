# Releasing SuperLighter

SuperLighter releases are built by GitHub Actions from immutable semantic-version tags. The workflow publishes a ready-to-run Windows executable, so end users do not need the source tree or the .NET SDK.

## Published assets

Every release contains these manually uploaded assets:

- `SuperLighter-win-x64.exe` — self-contained, single-file Windows x64 application
- `SHA256SUMS.txt` — SHA-256 checksum for the executable

GitHub also adds automatic source archives to every release. Users who only want the application should download `SuperLighter-win-x64.exe` from the Assets section.

The latest release page is:

```text
https://github.com/SamiKamara/SuperLighter/releases/latest
```

The stable direct-download URL is:

```text
https://github.com/SamiKamara/SuperLighter/releases/latest/download/SuperLighter-win-x64.exe
```

The repository and its releases are public. Both links work without authentication.

## Automated release process

1. Ensure `main` contains the code intended for release and the worktree is clean.
2. Update `<Version>` in `SuperLighter.App/SuperLighter.App.csproj`.
3. Add the release notes to `CHANGELOG.md`.
4. Run the local checks:

   ```powershell
   dotnet restore .\SuperLighter.sln
   dotnet format .\SuperLighter.sln --verify-no-changes --no-restore
   dotnet build .\SuperLighter.sln -c Release --no-restore
   $test = Start-Process dotnet -ArgumentList '.\SuperLighter.App\bin\Release\net9.0-windows\SuperLighter.dll', '--self-test' -Wait -PassThru -NoNewWindow
   if ($test.ExitCode -ne 0) { throw "Self-tests failed." }
   ```

5. Commit and push the release preparation to `main`.
6. Create an annotated tag matching the project version, then push it:

   ```powershell
   git tag -a v1.3.1 -m "SuperLighter v1.3.1"
   git push origin v1.3.1
   ```

7. The `Build release` workflow will validate, build, test, publish, hash, and create the GitHub Release.
8. Monitor and verify the result:

   ```powershell
   gh run list --workflow release.yml --limit 5
   $runId = gh run list --workflow release.yml --limit 1 --json databaseId --jq '.[0].databaseId'
   gh run watch $runId --exit-status
   gh release view v1.3.1
   ```

The tag must use exactly `vMAJOR.MINOR.PATCH`, and the numeric portion must match the project `<Version>`. A mismatch fails before publishing.

## Manual workflow rerun

If the tag exists but its release workflow needs to be rerun:

1. Open the repository's **Actions** tab.
2. Select **Build release**.
3. Choose **Run workflow**.
4. Enter the existing tag, such as `v1.3.1`.

The workflow checks out that tag. If the release already exists, its EXE and checksum assets are replaced with freshly built copies; otherwise the release is created.

The equivalent GitHub CLI command is:

```powershell
gh workflow run release.yml --ref main -f tag=v1.3.1
```

## What the workflow verifies

- The release tag is a three-part semantic version.
- The tag and project versions match.
- Dependencies restore successfully.
- Formatting is clean.
- The Release build succeeds without errors.
- Internal display-adapter routing, legacy gamma-ramp, NVIDIA-compatible color-matrix, monitor-brightness mapping, shortcut, and UI-construction self-tests pass.
- Publishing produces a self-contained Windows x64 executable.
- A SHA-256 checksum is generated alongside the EXE.

The workflow uses the repository-scoped `GITHUB_TOKEN` with only `contents: write`, which is required to create the release and upload its assets. No personal access token or repository secret is required.

## Verify a downloaded executable

Download both release assets into the same folder and run:

```powershell
$actual = (Get-FileHash .\SuperLighter-win-x64.exe -Algorithm SHA256).Hash.ToLowerInvariant()
$expected = (Get-Content .\SHA256SUMS.txt).Split(' ')[0].Trim()
if ($actual -ne $expected) { throw "Checksum mismatch." }
Write-Host "Checksum verified: $actual"
```

## Signing status

The current executable is not code-signed. Windows SmartScreen may therefore show an unknown-publisher warning. Code signing can be added later without changing the release asset contract: sign `SuperLighter-win-x64.exe` after publishing and before calculating `SHA256SUMS.txt`.
