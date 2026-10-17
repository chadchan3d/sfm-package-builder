# Scene Package Current Status

Date: 2026-10-17

Status: Public sanitized project checkpoint.

## Canonical authority

The governing design authority is unchanged and is identified by `docs/governance/AUTHORITY_POINTER.md`:

- `SCENE_PACKAGE_INTEGRATION_BLUEPRINT_REVISION_2.md` (frozen base Blueprint)
- `REVISION_2_FREEZE_ERRATA_AND_AUTHORITY_RECORD.md` (bounded authoritative errata)
- `decisions/MODERN_PRIOR_ART_ARCHITECTURE_DECISION.md` (later bounded decision; governs evidence order and scope only)

The frozen historical baseline also includes the accepted Stage 0A evidence harvest and ledger (`stage0/`) and the frozen E1 experiment design. The repository checkpoint before the modern-prior-art pivot was commit `430a55b4b443c4554689839c87ad5ac3e5ab8ab3` ("Freeze E1 portability experiment design"). The Modern Prior-Art decision supersedes only the evidence-order and scope interpretations it explicitly identifies; it does not rewrite the frozen historical documents.

This file is a sanitized handoff summary. It does not amend those documents, and it is not a substitute for the implementation Ledger (`docs/governance/IMPLEMENTATION_LEDGER.md`). Private raw evidence is retained outside the repository and is referred to only by opaque evidence ID.

## Architecture

The retained production direction is:

`immutable saved DMX -> structural decoding -> SFM semantic occurrences -> Source-profile resolution and typed dependency closure -> Scene policy/final package plan -> shared staging/ZIP verification -> recipient verification`

Saved DMX bytes are never rewritten. Model Package behavior remains the protected baseline. Scene-specific parsing, resolution, closure, policy and persistence stay outside Model discovery logic.

## Evidence route

The Modern Prior-Art decision sets this evidence route:

1. qualify saved-file structural decoding;
2. establish independent expected dependency/provider truth for a suitable saved scene;
3. compare existing packager output against that known truth;
4. perform a controlled recipient witness;
5. investigate only observed disagreements and remaining runtime gaps;
6. make component/adoption decisions;
7. begin bounded Scene Package product integration under the retained Revision 2 boundaries.

Steps 1–4 are satisfied at the scoped one-scene level described below. Step 5 has completed its reconciliation phase (Step 5A).

All findings below concern **one** genuine saved SFM session (binary-5 / sfm_session-20) containing one custom model/material fixture, on **one** SFM installation.

## Completed evidence

### Earlier work (unchanged from the previous checkpoint)

- Revision 2 and the Freeze Errata were frozen after adversarial review.
- The Stage 0A existing-evidence harvest and ledger were accepted.
- P0/P1 experimental contracts and external controls were completed.
- P3 owned test resources were prepared and preserved.
- Modern prior-art research covered SFMPackager, SrcPack, KeyValues2/Datamodel.NET, srctools and related Source dependency tooling.
- Architecture authority accepted the modern-prior-art pivot with bounded modifications.

### Step A — saved-DMX structural decoding

- Pinned KeyValues2 1.0.0 parsed the session read-only: 58 elements, 23 classes, 395 attributes, 76 references, zero errors, input bytes unchanged.
- The session's only resource-bearing values are relative:
  - one custom model occurrence;
  - the active film's map name;
  - a model-associated `DmePresetGroupInfo.filenameBase` with an empty preset list.
- **Scope:** one-file parsing qualification only. This is not production parser adoption, safety qualification or corpus coverage.

Details: `stage0/STEP_A_KEYVALUES2_ASSESSMENT.md`.

### Step B — independent expected closure

- From the fixture's own construction records, the expected custom model/material closure is:
  - **five required assets:** MDL, VVD, DX90 VTX, VMT, VTF;
  - **two policy-permitted companions:** DX80 VTX and SW VTX, which are matching-family files and not independently necessary.
- An independent Valve decoder corroborated 12 selected structural associations from Step A.
- **Limits:**
  - **Map:** full map closure was held because the first map-inspection tool failed. Step 5A later reconciled this (see below).
  - **`filenameBase`:** its role was unresolved; Step 5A reconciled this too.
  - **Not assessed:** provider equivalence and recipient portability.

### Step C — bounded packager comparison

- One operator-run SFMPackager comparison was made. Its archive contained:
  - all five required assets, byte-exact, at the correct logical paths;
  - the unchanged root DMX;
  - the saved map;
  - 30 additional material/texture files.
- Accepted as a bounded observation with disclosed protocol deviations, not a strict protocol pass, and closed without a repeat.
- **What it supports:** packaging this scene's known custom closure with the original DMX bytes preserved.
- **What it does not qualify:** editable-session traversal, engine precedence, map closure or recipient behavior.

### Step D — controlled recipient witness

**Historical attempt (retained as recorded):**
- **D0 (donor baseline): PASS.**
- **R0 (pre-install absence): PASS.**
- **R1: HOLD.**
  - The receiver-side unit restored the intended model and material.
  - One representative transform edit did not produce established visible motion.
  - Its cause was never diagnosed.
- **R1-U adjudication:** a later design adjudication replaced the transform edit, prospectively, with an interactive-inspection use test (R1-U). It explicitly kept the R1 HOLD and left editability unqualified.
- **Tooling HOLDs:** later R1-U attempts stopped on observation-tooling defects.

**Fresh campaign `b30e4987` (governing; no reuse of historical rows):**
- Rows ran on a foreground recorder that retained the SFM process handle for each row, with an independent read-only assessor and hash-chained evidence seals.
- From R0 onward, the seven donor-side fixture files were quarantined.
- The receiver-side assets went into a second existing content root on the same installation. Each row used its specified variant of the five-asset receiver unit: absent, partial, the intended geometry, or an alternate geometry.
- **All eight rows were accepted:**

| Row | Purpose | Result |
| --- | --- | --- |
| D0 | Donor baseline: donor fixture present, receiver absent | PASS (Designer manual evidence-integrity adjudication; the automatic assessor's HOLD is retained) |
| R0 | Pre-install absence: donor and receiver absent; model does not resolve | PASS |
| R1-U | Receiver unit present: intended model/material plus interactive Work Camera inspection | PASS |
| M0 | Receiver material pair withdrawn: geometry stays, material fails | PASS |
| M1 | Material pair restored: material recovers | PASS |
| R2 | Alternate receiver geometry: the receiver unit determines the geometry | PASS |
| R3 | Whole receiver unit withdrawn after warm runs: neither geometry resolves | PASS |
| R4 | Exact receiver unit restored: model/material and saved relationship recover | PASS |

- **Final restoration:** the original donor baseline was restored by an Owner-run, journalled procedure and then independently verified:
  - donor files have their exact original identities;
  - receiver files and experiment-created folders are absent;
  - protected user autosaves, profile and configuration are unchanged.
- Shared Steam bookkeeping changes are documented, not restored, by design. Normal SFM use is permitted. Recovery copies and evidence are retained privately.

**Allowable conclusion:** *scoped receiver claim established for interactive inspection and the measured model/material controls.* This covers this one saved scene, with its declared map/support environment, on the same installation.

**Not established by Step D:**
- transform editability or general editability: the historical R1 HOLD stands, and the eight PASS results do not prove editability;
- persisted-edit behavior;
- cross-installation or general portability;
- provider precedence over a conflicting copy;
- map closure;
- sound, particle or other runtime classes.

## Step 5A — evidence reconciliation

Step 5A reconciled the open Step A–D issues using existing evidence only, with no new runtime work.

| Issue | Finding | Disposition |
| --- | --- | --- |
| R1 transform-edit HOLD | The unproven claim is narrow: one direct value edit on the model's transform control produced no observed motion. A plausible but unconfirmed structural hypothesis exists: the saved animation structure has a play-mode channel with a single logged key that would hold the saved position. It is not an established cause and was never tested at runtime. Packaging leaves the DMX byte-identical. | Deferred for the internal Stage 5 milestone; required before public release. The R1 HOLD is preserved. The minimal discriminating observation is one paired normal-route edit performed identically in donor and recipient. |
| Map closure | The first inspection failure was a tooling failure. Later read-only inspection identified the map's embedded files, sky and detail settings and texture names. Static props and detail-sprite data were not inspected. | Map closure deferred to Blueprint Stage 6. The proposed first slice treats the map as an exact-identity recipient prerequisite and does not package it. This does not waive Blueprint Stage 5's requirement for an inspected map or a genuinely dependency-free minimal fixture. |
| `filenameBase` | No derived resource was needed to open or inspect the scene. Recipient rows ran with only the five receiver assets, and the packager added nothing for this field. | Resolved for the first slice: authoring metadata when the preset list is empty. Unsupported when the preset list is non-empty. |
| Step C's 30 extra files | All 30 derive from the saved map: its texture list, its sky setting, its detail-sprite material, and those materials' texture references. None belongs to the custom model/material closure. Their stock or loose provenance is unverified. | Resolved as one map-derived support class. Packaging them is a Stage 6 and prerequisite/rights decision. |
| Blueprint Q05 | The recipient custom model was visible with donor files unavailable (R1-U, R4). R2, M0 and M1 show the receiver unit supplies the geometry and material. MDL, VVD and DX90 VTX sufficed. | Resolved at one-scene, one-installation, DX9-profile scope. Other VTX profiles, PHY/ANI/include-models/skins/LODs and cross-installation behavior remain unsupported. |
| E1 absolute paths | No absolute candidates occur in this session. Relative candidates resolved from a second content root. | Relative-path-only support for the first internal slice; absolute candidates are reported as unsupported. E1 viability is not fully qualified and remains outstanding. |
| E4 precedence and relocation | One receiver-root winner was observed. Session file location was not load-bearing for this scene. Precedence over a conflicting copy is untested. | No override promise. Install only into an explicitly reviewed existing content root, with collision rejection and recipient conflict checks. The narrow E4 qualification required by the Blueprint remains outstanding. |
| E5 stock baseline | No automatic stock classification is qualified. | No automatic stock classification in the first slice; every packaged resource requires an explicit include decision. The applicable E5 requirements for later stages remain outstanding. |
| E2 corpus profile | Only one real session has been examined. | Blocking; the next evidence task (proposed Step 5B). |
| E6 parser adoption | KeyValues2 1.0.0 is qualified on one file only. | Blocking: folded into Step 5B. |

These dispositions bound the first slice. They do not waive any governing E1, E4 or E5 requirement. E2/E6 are the next evidence task; E1 viability, narrow E4 qualification and the applicable E5 stage requirements remain outstanding.

No contradiction of a governing invariant was found. No design review is required.

## Designer dispositions recorded at this checkpoint

These are scoped Designer dispositions. They do not replace or amend the governing authority identified by `docs/governance/AUTHORITY_POINTER.md`.

1. **Relative-path-only internal support.**
   - Relative-path-only support is acceptable for the first internal implementation slice. Any absolute or topology-sensitive candidate is reported as unsupported.
   - **E1 viability is not fully qualified.** Any absolute slot found later requires its own E1 classification.
   - This bounds the internal slice; it is not a public portability promise.
2. **Editability timing.**
   - Transform editability is not a prerequisite for the limited internal Stage 5 milestone.
   - Under the current product definition, editability must be qualified before public release.
   - The historical R1 HOLD is preserved. No product text may claim that packaged scenes are editable until editability is qualified.

## Current direction

- **Next evidence task (proposed, paused for this checkpoint) — Step 5B: E2/E6 corpus and parser qualification.** E1 viability, narrow E4 qualification and the applicable E5 stage requirements remain outstanding alongside it.
  - **Inputs:** an Owner-supplied small set of ordinary saved sessions, some held out.
  - **Method:** inventoried offline with the pinned parser, together with package/notice and warnings-as-errors build inspection.
  - **Excluded:** SFM runtime testing.
  - **Acceptance:** zero parse errors, unchanged input bytes, a classified resource-slot inventory with absolute candidates listed, and no new in-slice slot in the held-out set.
- **Possible separate task — Blueprint Stage 4A** (mechanical, Model-parity extraction of shared build mechanics). It lists no evidence-gate dependency, but it is not authorized.
- **Not begun:** Scene Package production implementation.
- **Unchanged:** the existing, released Model Package functionality, which remains the protected baseline.

## Paused / deferred work

- **P2:** parked. General live-SFM observer/marker work is not a prerequisite.
- **P4:** paused. Existing preparation evidence is preserved; no E1 baseline row has run.
- **Frozen E1 campaign:** deferred, not passed or deleted. Absolute-path tests are triggered by actual saved candidates.
- **Universal P6 rehearsal/validator:** deferred in favor of task-specific evidence validation.
