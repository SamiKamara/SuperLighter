# SuperLighter

<p align="center">
  <img src="SuperLighter.App/Assets/SuperLighter.png" alt="SuperLighter icon" width="96" height="96">
</p>

SuperLighter is a standalone Windows utility for adjusting the rendered desktop image beyond the standard Windows brightness controls. It combines a click-through topmost overlay, the display gamma LUT, and a full-screen color matrix in one lightweight tray application.

## Download

Download the ready-to-run application from the [latest GitHub Release](https://github.com/SamiKamara/SuperLighter/releases/latest), or use the [direct SuperLighter-win-x64.exe download](https://github.com/SamiKamara/SuperLighter/releases/latest/download/SuperLighter-win-x64.exe).

The release EXE is self-contained: extract nothing and install no SDK. `SHA256SUMS.txt` is provided beside it for integrity verification. Because the executable is not currently code-signed, Windows SmartScreen may show an unknown-publisher warning.

The repository and its releases are public, so the application can be downloaded without a GitHub account.

## Features

- Gamma adjustment from `0.50` to `6.00`
- Contrast adjustment from `50%` to `200%`
- Saturation adjustment from grayscale (`0%`) to boosted color (`300%`)
- Click-through, always-on-top brightness overlay from `0%` to `60%`
- Adaptive physical-backlight controls for monitors that expose DDC/CI brightness adjustment
- Live preview in a responsive dark-mode settings window
- Multi-monitor support
- Configurable global keyboard shortcuts
- Single-instance tray application
- Automatic restoration of the original gamma LUT and color matrix when enhancement is disabled or the application exits normally
- Self-contained, single-file Windows x64 publishing

## Default shortcuts

- `Ctrl+Alt+B`: toggle enhancement
- `Ctrl+Alt+O`: open settings

Both shortcuts can be changed in the settings window. Focus a shortcut field and press the new combination; press `Delete` to clear it.

New installations start with enhancement enabled, gamma at `2.50`, contrast at `120%`, saturation at `140%`, and brightness overlay at `0%`.

## Requirements

- Windows 10 or Windows 11
- .NET 9 SDK only when building from source

The published executable is self-contained and does not require a separate .NET installation.

## Run from source

```powershell
dotnet run --project .\SuperLighter.App\SuperLighter.App.csproj
```

SuperLighter starts in the Windows notification area and opens the settings window on launch. Starting the executable again reuses the existing instance and opens its settings.

## Build and test

```powershell
dotnet build .\SuperLighter.sln -c Release
dotnet .\SuperLighter.App\bin\Release\net9.0-windows\SuperLighter.dll --self-test
```

The self-test validates settings normalization, shortcut defaults, gamma-ramp generation, saturation-matrix generation, monitor-brightness value mapping, and creation of the main WinForms controls without changing display effects.

## Publish a standalone executable

```powershell
.\publish-win-x64.ps1
```

The script creates one self-contained file at:

```text
artifacts\publish\win-x64\SuperLighter.exe
```

Tagged releases are built and published automatically by GitHub Actions. See [docs/RELEASING.md](docs/RELEASING.md) for the versioning, tagging, validation, rerun, and checksum process.

## Settings and migration

Settings are stored locally at:

```text
%AppData%\SuperLighter\settings.json
```

SuperLighter performs a one-time settings migration from the former `%AppData%\ScreenBoostOverlay\settings.json` location when no SuperLighter settings file exists. It does not send settings or telemetry anywhere.

## How the effects work

- **Gamma and contrast** are composed onto the gamma LUT captured from each display at startup.
- **Saturation** is applied with the Windows Magnification API full-screen color matrix while preserving the previously active matrix.
- **Brightness overlay** is a white, non-activating topmost window that passes mouse input through to applications beneath it.
- **Physical backlight** controls are created per monitor only when Windows can read that monitor's hardware brightness through DDC/CI. Saved values are reapplied when the same monitor is detected again.

The original gamma LUT and color matrix are retained in memory and restored when enhancement is disabled or SuperLighter exits normally.

## Limitations

- Software cannot exceed a display panel's physical backlight or OLED brightness limit. The overlay changes the rendered image and therefore also raises black levels.
- HDR, Remote Desktop, exclusive fullscreen, anti-cheat software, another color-management utility, or the display driver can block or replace display effects.
- Some exclusive-fullscreen applications can cover topmost windows.
- Physical-backlight controls require working DDC/CI/MCCS support from the monitor, GPU, cable, and any dock or adapter in between. Unsupported displays are omitted automatically.
- A forced process termination or system crash can prevent normal cleanup; Windows typically resets gamma ramps during display-mode changes or restart.

## Repository layout

```text
SuperLighter.App/       WinForms application source
SuperLighter.sln        Visual Studio / dotnet solution
publish-win-x64.ps1     Self-contained single-file publish script
CHANGELOG.md             Version history
docs/RELEASING.md        Release process and verification guide
.github/workflows/       Automated tagged release workflow
README.md               Project documentation
```
