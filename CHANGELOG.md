# Changelog

All notable SuperLighter changes are documented here. Versions follow semantic versioning.

## [Unreleased]

### Added

- Adaptive per-monitor physical-backlight controls for displays with working DDC/CI brightness support

### Changed

- Increased the maximum gamma value to 6.00
- Updated new-user defaults to gamma 2.50, contrast 120%, saturation 140%, and brightness overlay 0%
- Sized the settings window to its visible content

### Fixed

- Vertically centered shortcut text in its fields
- Ensured tray Exit closes an open settings dialog and releases the executable

## [1.2.1] - 2026-08-05

### Added

- A custom three-color SuperLighter icon for the executable, system tray, and settings window

## [1.2.0] - 2026-08-05

### Added

- Configurable gamma, contrast, saturation, and brightness-overlay controls
- Configurable global shortcuts with validation
- Responsive English dark-mode settings interface
- Multi-monitor click-through topmost overlays
- Gamma-LUT and full-screen color-matrix restoration
- Migration from the former ScreenBoostOverlay settings location
- Self-contained Windows x64 publishing
- Automated tagged GitHub Releases with an EXE and SHA-256 checksum

### Changed

- Renamed the project and application to SuperLighter
