# 0001. WinUI 3 on .NET 9, unpackaged and self-contained

- Date: 2026-10-13
- Status: Accepted

## Context

Version 1 was a single WinForms file compiled on first run by the C# 5 compiler built into Windows,
so the folder held only plain source and needed no SDK. The user wanted a modern UI like PowerToys,
which is built on WinUI 3.

## Decision

WinUI 3 (Windows App SDK 1.8) on .NET 9, unpackaged (`WindowsPackageType=None`) and self-contained
(`SelfContained`, `WindowsAppSDKSelfContained`), x64 only. The portable folder ships a prebuilt `app\`;
building needs the .NET 9 SDK. Settings pages use the Community Toolkit `SettingsCard` and
`SettingsExpander`, as PowerToys does.

## Alternatives considered

- WPF on .NET 9 with its Fluent theme: smaller and simpler, but not the PowerToys look.
- Keep the built-in csc and restyle with WPF from .NET Framework 4: no SDK needed, but no Mica and
  older-looking controls.
- Windows App SDK 2.x: newer; 1.8 chosen because its APIs were known well.

## Consequences

- `app\` is about 174 MB and around 500 files. It runs on any 64-bit Windows 10 1809+ with nothing installed.
- The plain-source, build-on-first-run design is gone; `v1-winforms\` keeps the old version.
- See [0005](0005-publish-settings.md) for the publish settings this needs.
