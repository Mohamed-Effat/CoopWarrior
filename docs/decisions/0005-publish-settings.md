# 0005. Publish settings: MSIX tooling on, ML runtime files removed

- Date: 2026-10-13
- Status: Accepted

## Context

- The first `dotnet publish` output crashed at startup inside `Microsoft.UI.Xaml.dll` (0xc000027b).
  It was missing `CoopWarrior.pri` and the compiled pages, which the `bin` build had.
- The Windows App SDK 1.8 metapackage copies ONNX Runtime and DirectML (about 40 MB) into the output.
  Its ML package comes in through the Community Toolkit's dependency on the metapackage, and the files are
  copied by the SDK's own self-contained targets, so `ExcludeAssets` on the package does not remove them.

## Decision

- `EnableMsixTooling=true` in the project. Publishing then produces `CoopWarrior.pri` with the pages inside.
- A `RemoveUnusedMachineLearningFiles` target after Publish deletes `onnxruntime.dll`,
  `onnxruntime_providers_shared.dll` and `DirectML.dll`.

## Consequences

- If the app ever uses the Windows ML or AI APIs, remove that target.
- After changing package versions, check the publish output still starts (see testing.md).
