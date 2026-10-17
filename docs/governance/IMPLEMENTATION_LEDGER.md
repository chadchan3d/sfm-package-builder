# SFM Package Builder — Implementation Ledger

Last updated: `2026-10-17` (public sanitized checkpoint: Scene Package evidence Steps A–D, final restoration and Step 5A reconciliation recorded)

Maintainer: Claude Code

This is the public, sanitized Ledger revision. A fuller private Ledger and all raw evidence are retained outside the repository and are referenced here only by opaque evidence ID.

## Repository / working state
- Repository: `chadchan3d/sfm-package-builder`, branch `main`.
- Parent of this checkpoint: `3953654b020ea2e5d5db579a083ccdc3b8f4fcb3` ("Adopt Claude Code operating baseline"), itself parented by the public baseline `8503b0c3dc5e429424acb42f7b706809dd1de75f`.
- This revision is a documentation-only publication candidate. It changes this Ledger and `docs/scene-package/CURRENT_STATUS.md` only. Its exact reviewed identity is bound privately under `EV-PUB-CHK-001`.

## Governing authority
- Accepted repository baseline: `main` @ `8503b0c3dc5e429424acb42f7b706809dd1de75f`.
- Committed governance baseline: `3953654b020ea2e5d5db579a083ccdc3b8f4fcb3`.
- Blueprint/errata/decision identities (Git objects, unchanged):
  - `docs/scene-package/SCENE_PACKAGE_INTEGRATION_BLUEPRINT_REVISION_2.md` blob `3eb85679510c4e5ec333bbceb6eedccc3fc7a21b`, SHA-256 `64DD9981B0C4A8B59D3B11DEB0C708FF7B59B8E933D28517B96214E50976015C`
  - `docs/scene-package/REVISION_2_FREEZE_ERRATA_AND_AUTHORITY_RECORD.md` blob `085499b2c3e88e95d82023732ee8b5859ea095b1`, SHA-256 `4C43CC7DDE185B52C506F0CE6BDC7060113D22BEA582DD2FA93294CDE3BF0099`
  - `docs/scene-package/decisions/MODERN_PRIOR_ART_ARCHITECTURE_DECISION.md` blob `d0e6704e25a64d219dc7363be38ffbc180a7ff8c`
- Precedence: Modern Prior-Art decision (evidence order/scope only) > Freeze Errata > Revision 2.
- Operating contracts: Revision 2. Manifest SHA-256 `65C7B029A181653268A1DF8C9EC1346D9FF1A41450099523C1C072F704A676AD`.
- Approved design changes not yet incorporated: none. The Designer scope dispositions below bound the first implementation slice. They do not amend the Blueprint, the errata or the authority pointer.

## Current milestone
- **Assignment:** sanitized documentation checkpoint (Owner-authorized preparation; commit and push require separate approval).
- **Completion criterion:** public status and Ledger accurately reflect verified state, with privacy review and the publication gate passed.
- **Status:** preparation accepted (`EV-PUB-CHK-001`). Designer conditional approval of r3; the four required corrections are applied in this revision. Awaiting Owner authorization to commit and push.
- **Previous milestone:** Step 5A bounded evidence reconciliation — `PASS` (`EV-SP-5A-01`). Designer dispositions are recorded below.

### Scene Package evidence history (verified; scoped)
All runtime findings concern one genuine saved session (binary-5 / sfm_session-20) with one custom model/material fixture, on one SFM installation.

- **Stage 0A** (existing-evidence harvest): accepted. No E1–E7 clause fully satisfied (`EV-SP-S0A-01`, `EV-SP-S0A-02`).
- **Step A** (structural decoding): pinned KeyValues2 1.0.0 parsed the session.
  - 58 elements, zero errors, bytes unchanged.
  - Only relative resource values: one custom model occurrence, the active film's map name, and a `filenameBase` with an empty preset list.
  - One-file scope only (`EV-SP-A-03`, `EV-SP-A-04`).
- **Step B** (independent expected closure from the fixture's construction truth):
  - five required custom assets (MDL, VVD, DX90 VTX, VMT, VTF);
  - two policy-permitted companions (DX80 VTX, SW VTX);
  - 12 structural associations corroborated by an independent decoder;
  - map closure held; `filenameBase` unresolved (`EV-SP-B-01` to `EV-SP-B-03`).
- **Step C** (SFMPackager black-box comparison):
  - the archive held all five required assets byte-exact, plus the unchanged root DMX, the saved map and 30 map-related extras;
  - accepted as a bounded observation with disclosed protocol deviations; closed without repeat (`EV-SP-C-03` to `EV-SP-C-05`).
- **Step D, historical attempt:** D0 PASS, R0 PASS, R1 HOLD (`EV-SP-D-02`, `EV-SP-D-03`).
  - R1 restored model/material, but the representative transform edit did not establish visible motion.
  - The R1 adjudication replaced the edit prospectively with an interactive-inspection use test (R1-U), kept the R1 HOLD, and left editability unqualified (`EV-SP-D-ADJ-01`).
  - Three later R1-U attempts stopped on tooling (`EV-SP-D-04` to `EV-SP-D-06`).
- **Step D tooling:**
  - The fresh no-reuse campaign governs (`EV-M4A-AUTH-001`).
  - Observation method: an external retained-process-handle method (`EV-M4C-A1-AUTH-001`, `EV-M4C-R2-AUTH-001`).
  - Recorder: a foreground per-row recorder with an independent assessor and hash-chained seals, qualified non-natively and adopted by the Owner (`EV-M4C-R2-QUAL-001`, `EV-M4C-R2-QUAL-003`, `EV-M4C-A2-AUTH-001`). Row-adapted candidates were preserved per row.
- **Step D fresh campaign `b30e4987` — all eight rows accepted:**

  | Row | Result | Evidence |
  | --- | --- | --- |
  | D0 donor baseline | PASS by Designer manual evidence-integrity adjudication; assessor HOLD retained (attempt-01 was a tooling HOLD) | `EV-SP-D-FRESH-D0-02` |
  | R0 pre-install absence | PASS | `EV-SP-D-FRESH-R0-01` |
  | R1-U receiver unit + interactive inspection | PASS | `EV-SP-D-FRESH-R1U-01` |
  | M0 material pair withdrawn | PASS | `EV-SP-D-FRESH-M0-01` |
  | M1 material pair restored | PASS | `EV-SP-D-FRESH-M1-01` |
  | R2 alternate geometry | PASS | `EV-SP-D-FRESH-R2-01` |
  | R3 post-warm absence | PASS | `EV-SP-D-FRESH-R3-01` |
  | R4 recovery of the receiver unit | PASS | `EV-SP-D-FRESH-R4-01` |

  - **Allowable conclusion:** *scoped receiver claim established for interactive inspection and the measured model/material controls.* It covers this one saved scene, with its declared map/support environment, on the same installation.
  - It is not the transform-edit witness. The historical R1 HOLD stands. Editability, persisted edits, cross-installation portability, precedence over conflicting copies, map closure and other runtime classes are not established.
- **Final restoration:** Owner-run and independently verified PASS (`EV-SP-D-FRESH-CLOSEOUT-01`).
  - Donor baseline restored with exact identities.
  - Receiver files and experiment-created folders are absent.
  - Protected autosaves, profile and configuration are unchanged.
  - Shared Steam bookkeeping is documented, not restored.
  - Normal SFM use is permitted. Recovery copies and evidence are retained privately.
- **Step 5A reconciliation** (`EV-SP-5A-01`; existing evidence only):
  - **R1 edit HOLD — deferred.** For the narrow failed claim there is a plausible but unconfirmed structural hypothesis (a play-mode channel with a single logged key); it is not an established cause. Editability is not claimed. The minimal later observation would be a paired donor/recipient normal-route edit.
  - **Map closure — deferred to Stage 6.** The proposed first slice treats the map as an exact-identity recipient prerequisite. This does not waive Blueprint Stage 5's inspected-map or dependency-free-fixture requirement.
  - **`filenameBase` — resolved** for the first slice: no derived resource is required. Unsupported when the preset list is non-empty.
  - **Step C's 30 extras — resolved** as one map-derived support class.
  - **Q05 — resolved** at one-scene, one-installation, DX9-profile scope.
  - **E1/E4/E5 — first-slice scope limits only.** These are not waivers. E1 viability, narrow E4 qualification and the applicable E5 stage requirements remain outstanding.
  - **E2 corpus and E6 parser adoption — blocking.** These are the next evidence task (proposed Step 5B).
  - No governing-invariant contradiction.
- **Designer dispositions** (recorded at this checkpoint; scoped dispositions, not replacement governing authority; `AUTHORITY_POINTER.md` unchanged):
  1. **Relative paths only.** Relative-path-only support is acceptable for the first internal slice; absolute or topology-sensitive candidates are reported as unsupported. **E1 viability is not fully qualified.** This is not a public portability promise.
  2. **Editability timing.** Transform editability is not a prerequisite for the limited internal Stage 5 milestone. Under the current product definition, editability must be qualified before public release. The historical R1 HOLD is preserved.
  3. **Push gate.** Unpushed predecessor commit `3953654b…` must pass its own push gate before publication.

### Governance and tooling milestone history (verified; retained)
- M1 — Claude Code migration / transfer admission: `PASS` (`EV-M1-GIT-001`, `EV-M1-OPC-001`, `EV-M1-PROV-001`, `EV-M1-OPS-001`, `EV-M1-TEST-001`, `EV-M1-TEST-002`, `EV-M1-LEDGER-001`).
- M2 — governance status normalization and publication candidate: `PASS`. The commit-content gate passed (`EV-M2-NORM-001`, `EV-M2-GATE-001`).
- M3 — local governance commit `3953654b…` with post-commit checkpoint: `PASS` (`EV-M3-COMMIT-001`, `EV-M3-LEDGER-001`). Not pushed; its push gate has not been executed. M3A: Astra synchronization audit, READY WITH NON-BLOCKING CONDITIONS.
- M4 — Scene Package evidence-state reconciliation: `PASS` (`EV-M4-SP-RECON-001`, `EV-M4-SP-INDEX`).
- M4A — Owner route decision: the fresh no-reuse Step D campaign governs. Historical D0/R0 are cited only for what they established (`EV-M4A-AUTH-001`).
- M4B — Designer selection of the external retained-process-handle observation method. Method selection was not campaign readiness.
- M4C-R1 — read-only tooling reconciliation (`EV-M4C-R1-REPORT-001`). It confirmed the original continuous observer failed (heartbeat publication denied); the root cause was not established (`EV-M4C-ASTRA-001`).
- M4C-A1 — Owner adoption of the tooling-amendment design approach, after Astra review READY WITH CONDITIONS (`EV-M4C-A1-AUTH-001`). Design approval only.
- M4C-R2 — foreground D0 recorder, qualified non-natively: 63/63 cases at declared scope (`EV-M4C-R2-AUTH-001`, `EV-M4C-R2-QUAL-001`).
  - The v1 typed-console supplement was a **HOLD** (input defect; `EV-M4C-R2-QUAL-002`).
  - A bounded correction produced v2, `PASS` at declared scope (`EV-M4C-R2-QUAL-003`).
- M4C-A2 — Owner adoption of v2 for D0 after Designer review (PASS with documented limitations) (`EV-M4C-A2-AUTH-001`). v1 was never adopted.
- Fresh campaign `b30e4987`: rows D0 through R4 and the closeout, as tabled above. Each row ran on a preserved, row-adapted candidate (v3–v11; `EV-M4C-R2-CANDIDATE-003` to `-011`).

### Retained HOLD / invalid records
- Step D first D0: INVALID (wrong subject opened) (`EV-SP-D-01`).
- Step D historical R1: HOLD (transform motion unestablished) (`EV-SP-D-02`). Preserved; never relabelled PASS.
- Step D R1-U tooling HOLDs (`EV-SP-D-04` to `EV-SP-D-06`). The aborted-attempt recovery was verified (`EV-SP-D-07`).
- Original fresh-campaign continuous-observer tooling HOLD (`EV-SP-D-08` to `EV-SP-D-10`). Its root cause was not established; it was superseded by the adopted foreground method.
- M4C-R2 v1 typed-console HOLD (`EV-M4C-R2-QUAL-002`).
- Fresh D0 attempt-01 tooling HOLD (`EV-SP-D-FRESH-D0-01`). The D0 automatic-assessor HOLD is retained beside the Designer's PASS adjudication.
- Step A and Step C acquisition/preflight HOLDs, superseded by the accepted attempts (`EV-SP-A-01`, `EV-SP-A-02`, `EV-SP-C-01`, `EV-SP-C-02`).

**Partial / open**
- Step B map closure: deferred to Stage 6 (see Step 5A).
- Provider equivalence, general precedence, cross-installation portability and editability: not established.

**Deferred / not run**
- Frozen E1 campaign: deferred, not passed. P4 paused (preparation HOLD, zero baseline rows). P2 parked. Universal P6 deferred.
- Not established for a supported profile: E2 corpus, E3, E4 precedence, E5 baseline, E6 safety/resources, E7.

**Gate mapping**
- Modern route steps 1–4: satisfied at the scoped one-scene model/material level.
- Step 5: reconciliation complete. E2/E6 are the next evidence task (Step 5B). E1 viability, narrow E4 qualification and the applicable E5 stage requirements remain outstanding, and Blueprint Stage 5's map/fixture requirement is unchanged.
- No production code is authorized. The Blueprint §9 parser preconditions remain in force.

**Contradictions:** none against a governing invariant. No DESIGN REVIEW REQUIRED.

## Implemented state
Scene Package product work has not begun. The existing Model Package source and tests remain identical to the v1.0.4 publication commit `e3caabce45192419e97dd9c6b9ece68fad2cf246`. Step D tooling was private experiment tooling outside the repository. Scene Package production implementation has not begun.

## Verification / evidence
- Model Package qualification (unchanged): 544 passed / 0 failed / 2 fixture-gated skips at source identical to the current HEAD (`EV-M1-TEST-002`). Not re-run; no source has changed since.
- Toolchain recorded at M2: Git 2.41.0 (`core.autocrlf=true`); Python 3.10.6 for manifest and scan tooling.
- Scene Package evidence: as listed above. Runtime rows were native SFM runs with Owner/operator observation, recorder receipts and an independent assessor. Step 5A was static reading of retained evidence.
- Governance evidence: `EV-M1-*`, `EV-M2-NORM-001`, `EV-M2-GATE-001`, `EV-M3-COMMIT-001`; Ledger revision identities retained privately.

## Failures / uncertainties / untested behavior
- Model Package: MDL golden-oracle and installed-SFM acceptance fixtures are skipped, and `dotnet publish` was not run. WinForms.Tests require build artifacts inside the repository tree (`EV-M1-TEST-001`).
- Scene Package: all runtime claims are scoped to one scene on one installation. The historical R1 HOLD and the Step D tooling HOLDs remain historical records.
- The committed M2 Ledger revision's `Last updated` value is later than its actual staging time. This is a non-substantive timestamp inaccuracy.

## Transfer admission
- Accepted baseline verified: yes.
- Operating contracts approved/pinned: yes (Revision 2, committed in `3953654b…`).
- Model Package baseline build/test capability verified: yes (`EV-M1-TEST-002`; gaps as above).
- Outstanding operational ownership/HOLDs: none active. Step D campaign `b30e4987` is closed and restored. Retained private recovery copies remain under Owner authority (`EV-SP-D-FRESH-CLOSEOUT-01`).
- Transfer admission: `PASS` (M1).

## Sanitation / publication
- Private-only material: none in this candidate. Private receipts are referenced by opaque evidence ID only.
- Candidate identity and gate results: bound privately under `EV-PUB-CHK-001`.
- Commit authorized: no. Push/publication authorized: no.

## Next bounded action
Designer/Owner review of this sanitized publication candidate, followed by explicit authorization (or not) to commit and push. After that, Step 5B (E2/E6 corpus/parser qualification) resumes only on explicit authorization. Blueprint Stage 4A remains a possible separate task, not authorized.
