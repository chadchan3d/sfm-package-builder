# SFM Package Builder Development History

This chronology is reconstructed from surviving Codex/ChatGPT development records, milestone reports, release reports, source files, tests, and release artifacts. It is not Git history. The project did not have an authentic pre-existing Git commit graph before this repository was initialized.

Do not treat any entry below as proof that a complete matching source snapshot survives unless explicitly stated. The surviving source tree represents the current 1.0.4 state at the point version control begins.

## Verified Current Source State

- Product: SFM Package Builder
- Current surviving source version: 1.0.4
- Core target framework: `net10.0`
- WinForms target framework: `net10.0-windows`
- Windows release runtime identifier: `win-x64`
- Distribution form: portable self-contained ZIP
- Source projects: `src/SfmPackageBuilder.Core`, `src/SfmPackageBuilder.WinForms`
- Test projects: `tests/SfmPackageBuilder.Core.Tests`, `tests/SfmPackageBuilder.IntegrationTests`, `tests/SfmPackageBuilder.WinForms.Tests`

## Reconstructed Milestone Path

The surviving milestone reports show a staged implementation path:

- Milestones 0-1 established the .NET solution, WinForms shell, domain model, `.sfmpack` JSON serialization, and schema-version handling.
- Later milestones added path services, model-family expansion, README generation/import/customization, package planning, validation, staging, archive creation, build coordination, persistence, and the WinForms workflow.
- Milestone 13 focused on acceptance/integration testing. The surviving report records 37 of 37 Design Book section-43 criteria passing for automated packaging, archive inspection, controlled install-tree extraction, and source-safety verification.
- Milestone 14 prepared an initial portable self-contained Windows x64 release candidate for version 1.0.0.
- Subsequent RC2 and Pass 9 work refined packaging, native .NET ZIP use, workflow/UX, MDL-aware metadata/material planning, missing-source recovery, generated README behavior, and release presentation.

## Verified Release Artifacts Found Locally

The following release artifacts survived locally under `outputs/`. These are binary/package evidence, not complete historical source snapshots.

| Version | Artifact | Size | Timestamp |
|---|---|---:|---|
| 1.0.0 | `outputs/SFM-Package-Builder-v1.0.0-win-x64.zip` | 45,927,750 bytes | 2026-08-16 00:27:35 |
| 1.0.0 RC2 | `outputs/SFM-Package-Builder-v1.0.0-rc2-win-x64.zip` | 47,921,167 bytes | 2026-08-16 16:16:48 |
| 1.0.1 | `outputs/distribution-1.0.1/SFM-Package-Builder-v1.0.1-win-x64.zip` | 53,533,251 bytes | 2026-08-22 09:32:47 |
| 1.0.2 | `outputs/distribution-1.0.2/SFM-Package-Builder-v1.0.2-win-x64.zip` | 53,549,398 bytes | 2026-08-22 14:59:55 |
| 1.0.3 | `outputs/distribution-1.0.3/SFM-Package-Builder-v1.0.3-win-x64.zip` | 53,550,183 bytes | 2026-08-22 15:42:33 |
| 1.0.4 | `outputs/distribution-1.0.4/SFM-Package-Builder-v1.0.4-win-x64.zip` | 53,553,326 bytes | 2026-08-22 18:22:51 |
| 1.0.4 source | `outputs/distribution-1.0.4/SFM-Package-Builder-v1.0.4-source.zip` | 3,321,494 bytes | 2026-08-26 23:20:40 |

The surviving `outputs/distribution-1.0.4/SFM-Package-Builder-v1.0.4-win-x64.sha256.txt` records the v1.0.4 Windows ZIP digest:

```text
D9904034065839F3EB8E1776F0243808AB8F8B2EFAE64071C4569E3DA9D30B5E
```

The source ZIP created for SFMLab moderation was extracted and used to reproduce a Windows x64 package with the same size and expected layout. Exact ZIP bytes may differ because PowerShell ZIP creation records file metadata and compression details.

## Acceptance Evidence Boundary

Milestone 13 verified automated packaging behavior using installed SFM assets, produced ZIP inspection, controlled install-tree extraction, and source immutability checks. It did not perform actual SFM/HLMV runtime/model-browser verification. No model was loaded in SFM/HLMV, spawned, rendered, flexed, or inspected for bodygroups as part of that milestone.

Outstanding release-candidate manual/environmental checks recorded in surviving reports include:

- actual SFM/HLMV load, model-browser visibility, spawn, and render check;
- true 125% Windows DPI/scaling pass;
- true 150% Windows DPI/scaling pass;
- current/newer 7-Zip compatibility for older 7-Zip-based milestones;
- physical offline/no-network run;
- genuine human mouse/keyboard end-to-end pass.

Later accepted work replaced the 7-Zip archive dependency with native .NET ZIP creation, so the current 1.0.4 source does not require 7-Zip to build or package.

## Version Control Boundary

This repository starts with the surviving 1.0.4 project state. No historical Git commits or tags are created for earlier releases. Earlier artifacts and reports are preserved as local evidence and summarized here only where supported by surviving files.
