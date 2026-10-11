# Scene Package Current Status

Date: 2026-10-11  
Status: Public sanitized project checkpoint.

## Canonical authority

The frozen baseline remains:

- `SCENE_PACKAGE_INTEGRATION_BLUEPRINT_REVISION_2.md`
- `REVISION_2_FREEZE_ERRATA_AND_AUTHORITY_RECORD.md`
- accepted Stage 0A evidence harvest and ledger
- frozen E1 experiment design

The baseline repository checkpoint before the modern-prior-art pivot was commit:

`430a55b4b443c4554689839c87ad5ac3e5ab8ab3` — `Freeze E1 portability experiment design`

The later public decision record `decisions/MODERN_PRIOR_ART_ARCHITECTURE_DECISION.md` supersedes only the evidence-order and scope interpretations it explicitly identifies. It does not rewrite the frozen historical documents.

## Architecture

The retained production direction is:

`immutable saved DMX -> structural decoding -> SFM semantic occurrences -> Source-profile resolution and typed dependency closure -> Scene policy/final package plan -> shared staging/ZIP verification -> recipient verification`

Model Package behavior remains protected. Scene-specific parsing, resolution, closure, policy, and persistence stay outside Model discovery logic.

## Completed evidence

- Revision 2 and Freeze Errata were frozen after adversarial review.
- Stage 0A existing-evidence harvest and ledger were accepted.
- P0/P1 experimental contracts and external controls were completed.
- P3 owned test resources were prepared and preserved.
- Modern prior-art research covered SFMPackager, SrcPack, KeyValues2/Datamodel.NET, srctools, and related Source dependency tooling.
- Architecture authority accepted the modern-prior-art pivot with bounded modifications.
- Step A qualified exact KeyValues2 1.0.0 for read-only parsing of one genuine saved SFM binary-5 / sfm_session-20 session.
- Step A confirmed a real saved custom-model occurrence and map-name candidate in that session while preserving the DMX bytes.

Private raw receipts and machine-local experiment records are retained outside the public repository. This checkpoint intentionally excludes local filesystem paths, machine usernames, process identifiers, private source bindings, unrelated personal information, and raw environment logs.

## Paused / deferred work

- **P2:** parked. General live-SFM observer/marker work is not a prerequisite.
- **P4:** paused. Existing preparation evidence is preserved; no E1 baseline row has run.
- **Frozen E1 campaign:** deferred, not passed or deleted. Its product questions remain; any replacement runtime protocol requires explicit claim/control review.
- **Universal P6 rehearsal/validator:** deferred in favor of task-specific evidence validation.

## Active evidence task

**Step B — Existing-scene expected-closure and comparison-readiness dossier**

Goal: establish, independently of the packager under test, which asset bytes the existing saved scene's first comparison should require, permit, treat as prerequisites, exclude as unrelated, or retain as unresolved.

Step B is expected to:

- reverify the accepted scene and supporting evidence identities;
- establish provider candidates and distinguish historical placement, current static candidates, and runtime-qualified winners;
- derive the custom model/material closure from fixture/source/build evidence rather than packager output;
- keep unrelated build outputs out of the expected set unless they have a dependency or explicit policy reason;
- treat the saved `stage.bsp` separately from the owned P3 map and preserve any unresolved map/provider boundary;
- carry a small independent fidelity gate for the specific decoded associations used in scoring;
- produce a reviewable manifest with path, provider identity, size/hash, reason, evidence source, and disposition.

No packager execution is authorized by Step B itself.

## Likely next sequence

If Step B establishes comparison readiness:

1. review the expected-closure dossier;
2. run a bounded SFMPackager black-box comparison against known truth;
3. use SrcPack only where it adds useful secondary evidence;
4. perform a controlled recipient/opening witness with donor availability and relevant winner controls;
5. investigate only observed path, precedence, dependency, or semantic disagreements;
6. make component/adoption decisions;
7. begin bounded Scene Package product integration under the retained architecture.

## Product implementation status

Scene Package production implementation has **not started**.

Current work is deliberately reducing architectural and runtime uncertainty before adding Scene-specific production code or refactoring shared Model Package execution.
