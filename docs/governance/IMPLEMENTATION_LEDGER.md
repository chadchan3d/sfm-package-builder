# SFM Package Builder — Implementation Ledger

Last updated: `2026-10-14T19:30Z`  
Maintainer: Claude Code

## Repository / working state
- Repository: `chadchan3d/sfm-package-builder`
- Branch: `main` (tracking `origin/main`, ahead 0 / behind 0)
- HEAD: `8503b0c3dc5e429424acb42f7b706809dd1de75f` (tree `8a0bf062416414f43100b10026ccc630d667505d`)
- Working tree: no tracked modifications. The 12 governance files below are new paths, staged in the index as the M2 publication candidate. No commit exists.
- Relevant uncommitted/untracked state: the 11 operating-contract files (status-normalized in M2) plus this Ledger. Ignored build output under `work/` (gitignored).
- Dirty-state identity (SHA-256 of candidate files):
  - `CLAUDE.md` `FDB5ECD60982D85A6174BC838BF6A3108EA970575AFEDD72DE520C7360F81145`
  - `docs/governance/AUTHORITY_AND_ROLES.md` `41497AD7D7A65AD316D57918A170C47737EC13A1C64C8648A9363225E402A62E`
  - `docs/governance/AUTHORITY_POINTER.md` `C199E6E62D503F4F6841879381F3A99459EB925AAA4150F7EE8407993BBC80C7`
  - `docs/governance/LEDGER_CONTRACT.md` `FCC45569690DD8CA386ADD2B99D3F8F8D3FF113A1B9C30428EAAF8B3AC1E67AB`
  - `docs/governance/LEDGER_TEMPLATE.md` `1FFF36CB10AAC76257222652DFA52C98D25EAA75ACFBF1C806E7EA49010DDF72`
  - `docs/governance/OPERATING_BASELINE_MANIFEST.md` `65C7B029A181653268A1DF8C9EC1346D9FF1A41450099523C1C072F704A676AD`
  - `docs/governance/SANITATION_AND_PUBLICATION_POLICY.md` `300A68DBF2E4F1E1BB6A1054F66BAFE744997116891736933A1043EE810EAC35`
  - `docs/governance/TEST_AND_EVIDENCE_POLICY.md` `8953E3613E599E2FE38E2F8C3A70063A95A1E361F0779F453480BA6DED513CD8`
  - `docs/governance/TRANSFER_ADMISSION_CHECKPOINT.md` `7A41724FCB340979695FFBC78C363C0677A233E6ED8BCF9A7EDF0AA8EBD4F4AA`
  - `docs/governance/TRANSFER_BASELINE.md` `FB8A8819B63926D4B3494390C6C9F6F33FC58CF21679C9816810B0265A8D8C98`
  - `docs/governance/WORKSPACE_POLICY.md` `7AF05F2092881A9102ABA69D7B189AA450F27701EBFD4F14576052AD6DE700C6`
  - `docs/governance/IMPLEMENTATION_LEDGER.md`: this file. It cannot record its own hash; its identity and the exact staged-tree identity are bound privately under `EV-M2-GATE-001`.

## Governing authority
- Accepted repository baseline: `main` @ `8503b0c3dc5e429424acb42f7b706809dd1de75f`; observed remote `main` = accepted commit (no delta)
- Blueprint/errata/decision identities (verified from Git objects at accepted commit, `EV-M1-OPC-001`):
  - `docs/scene-package/SCENE_PACKAGE_INTEGRATION_BLUEPRINT_REVISION_2.md` blob `3eb85679510c4e5ec333bbceb6eedccc3fc7a21b`, SHA-256 `64DD9981B0C4A8B59D3B11DEB0C708FF7B59B8E933D28517B96214E50976015C`
  - `docs/scene-package/REVISION_2_FREEZE_ERRATA_AND_AUTHORITY_RECORD.md` blob `085499b2c3e88e95d82023732ee8b5859ea095b1`, SHA-256 `4C43CC7DDE185B52C506F0CE6BDC7060113D22BEA582DD2FA93294CDE3BF0099`
  - `docs/scene-package/decisions/MODERN_PRIOR_ART_ARCHITECTURE_DECISION.md` blob `d0e6704e25a64d219dc7363be38ffbc180a7ff8c`
- Operating-contract revision: Revision 2, adopted by the Owner. Manifest `docs/governance/OPERATING_BASELINE_MANIFEST.md` SHA-256 `65C7B029A181653268A1DF8C9EC1346D9FF1A41450099523C1C072F704A676AD` (status-normalized in M2). The M1-era identity `5F9A0F02B6C9A2972CCE68B0F03CB001F4B806DA3855730A5835F20A6885F085` is the reviewed pre-normalization identity. The before/after identity record is `EV-M2-NORM-001`; the Owner approval record is `EV-M1-OPC-001`.
- Approved design changes not yet incorporated: none

## Current milestone
- Assignment: M2 — governance adoption normalization + publication candidate (no commit, no push)
- Completion criterion: status-only normalization applied and recorded; manifest identities regenerated and verified; exact 12-file candidate staged by explicit allowlist; full fail-closed publication gate executed against the exact staged candidate; proposed commit metadata assessed
- Status: `PASS`

### Milestone history (verified; retained)
- M1 — Claude Code migration / transfer admission: `PASS` (Designer-accepted). Evidence: `EV-M1-GIT-001`, `EV-M1-OPC-001`, `EV-M1-PROV-001`, `EV-M1-OPS-001`, `EV-M1-TEST-001`, `EV-M1-TEST-002`, `EV-M1-LEDGER-001`.

## Implemented state
No product source or test changes in M1 or M2. M2 changed status-label wording only in 9 operating-contract files (each `Status:` line set to `OPERATING BASELINE REVISION 2`; the stale word "candidate" removed from one manifest sentence). It also regenerated 8 affected manifest SHA-256 rows. Authority hierarchy, roles, evidence, sanitation/publication, STOP/HOLD, workspace, and Blueprint-precedence rules are unchanged. Model Package source/tests at HEAD remain identical to the v1.0.4 publication commit `e3caabce45192419e97dd9c6b9ece68fad2cf246`.

## Verification / evidence
- Toolchain/runtime identity: Git 2.41.0 (system `core.autocrlf=true`); Python 3.10.6 (manifest and scan tooling). The M1 .NET toolchain is unchanged and was not re-run.
- Tests/inspections actually run (M2):
  - Starting-state check: remote `main` = HEAD = accepted commit; no tracked changes; empty index; governance bytes = M1 identities (`EV-M2-NORM-001`)
  - Status-only diff review and manifest regeneration; all 10 manifest rows re-verified byte-for-byte; no CR bytes (`EV-M2-NORM-001`)
  - Explicit 12-path staging and full publication gate on the exact staged blobs (`EV-M2-GATE-001`)
- Outcomes: normalization 10 changed lines plus 8 hash rows; manifest 10/10 match; publication gate PASS on content (`EV-M2-GATE-001`)
- Product build/test: not re-run (governance-only change). The qualified baseline remains M1 `EV-M1-TEST-002`: 544 passed / 0 failed / 2 fixture-gated skips at HEAD `8503b0c3dc5e429424acb42f7b706809dd1de75f`.
- Evidence IDs: `EV-M2-NORM-001`, `EV-M2-GATE-001` (durable private retention); M1 IDs above
- Working-state identity: HEAD `8503b0c3dc5e429424acb42f7b706809dd1de75f` + candidate file identities listed above; staged-tree identity bound in `EV-M2-GATE-001`

## Failures / uncertainties / untested behavior
- Skipped tests carried from M1: MDL golden-oracle corpus (private aid, not promoted) and installed-SFM acceptance fixture (env var unset). `dotnet publish` was not run.
- WinForms.Tests require test binaries inside the repository tree (`EV-M1-TEST-001`). Build artifacts stay under ignored `work/`.
- Commit identity: no Git author/committer identity is configured for this checkout, so a commit cannot be created as-is. A per-commit public project identity method is identified privately (`EV-M2-GATE-001`) and needs Owner confirmation. No configuration was changed.

## Transfer admission
- Accepted baseline verified: yes (`EV-M1-GIT-001`; re-verified at M2 start)
- Operating contracts approved/pinned: yes. Revision 2, status-normalized; identities verified (`EV-M2-NORM-001`). Not yet committed.
- Model Package baseline build/test capability verified: yes (`EV-M1-TEST-002`; gaps as above)
- Outstanding operational ownership/HOLDs: `EV-M1-OPS-001`. One prior Scene Package Step D runtime campaign remains under a tooling HOLD with Owner-held authority. It was not touched in M2.
- Transfer admission: `PASS` (M1)

## Sanitation / publication
- Private-only material present: none in the candidate. Private receipts are held outside the repository and referenced by opaque evidence ID only.
- Exact publication candidate identity: staged 12-file index tree, bound privately in `EV-M2-GATE-001`
- Sanitized for publication: staged content passed the fail-closed gate (`EV-M2-GATE-001`). Proposed commit metadata needs Owner confirmation of the public identity before any commit.
- Commit authorized: no
- Push/publication authorized: no

## Next bounded action
Owner decision on the staged M2 candidate: confirm the public commit identity, then explicitly authorize (or decline) a single local commit of exactly the staged tree recorded in `EV-M2-GATE-001`.
