---
title: Changelog
description: Release history for SampleLibrary
order: 99
---

# Changelog

All notable changes to SampleLibrary are documented below.

:::changelog

## v2.1.0 — 2026-03-15
type: minor
### Added
- New `ResultBuilder<T>` for fluent operation chaining
- `Rectangle` shape implementing `IShape` with full area and perimeter support
- `Color.Lerp()` method for linear interpolation between two colors

### Changed
- `Calculator.Divide` now returns `OperationResult<T>` instead of throwing exceptions
- Improved XML documentation coverage across all public types

### Fixed
- Division by zero no longer crashes; returns a descriptive error result
- `Circle.Area` precision improved for very small radii

## v2.0.0 — 2026-02-01
type: major
### Added
- Generic `Process<T>` method on `Calculator` for type-safe numeric operations
- `ObservableList<T>` with event-driven collection change notifications
- `IShape` interface unifying `Circle`, `Rectangle`, and future shapes
- Comprehensive analyzer package `SampleLibrary.Analyzers`

### Changed
- Migrated from .NET 8 to .NET 9 target framework
- `Color` struct now implements `IEquatable<Color>` and `IFormattable`

### Breaking
- Removed legacy `LegacyCalculator` class — use `Calculator` instead
- Changed `IShape.Area` from method to read-only property
- Renamed `ColorUtils` to `ColorExtensions` for consistency

### Deprecated
- `Calculator.OldDivide()` is deprecated in favor of the new `Divide` returning `OperationResult<T>`

## v1.1.0 — 2026-01-10
type: minor
### Added
- `Color.FromHex()` static factory for parsing hex color strings
- `Calculator.Modulo()` operation with full error handling

### Fixed
- `Color.ToString()` now correctly formats alpha channel when opacity is less than 1.0
- `Circle` constructor validates that radius is non-negative

### Security
- Updated dependency on `System.Text.Json` to patch CVE-2026-0001

## v1.0.0 — 2025-12-01
type: initial
### Added
- Initial release with `Calculator`, `Circle`, and `Color` types
- Full XML documentation on all public APIs
- NuGet package publishing via CI/CD pipeline
- Unit test suite with 95% code coverage

:::
