# Stage 0A: Existing SFM Evidence Harvest

**Scope:** read-only internal evidence harvest against the frozen Scene Package Stage 0 gates E1–E7.
**Authority applied:** `SCENE_PACKAGE_INTEGRATION_BLUEPRINT_REVISION_2.md` (SHA-256 `64DD9981…6015C`) together with `REVISION_2_FREEZE_ERRATA_AND_AUTHORITY_RECORD.md` (SHA-256 `4C43CC7D…0099`). Both hashes were verified against `control/HARVEST_MANIFEST.txt` at the access gate.
**Companion ledger:** `output/STAGE0A_EVIDENCE_LEDGER.tsv` has 45 claim rows. IDs below (`S0A-Ex-nn`) refer to that file.

---

## 1. Executive conclusion

1. **No E1–E7 clause is fully satisfied by existing evidence.** I found no claim that meets the Freeze Errata §3.2 bar for full reuse. There are two recurring reasons.
   - **Runtime claims repeatedly lack sufficient identity.** That can be build/profile identity, input identity, or both. The SFM Steam build/engine version was never recorded. The only recorded profile is one machine's install path, embedded Python 2.7.5 (32-bit) and Qt4/PySide. The key DMX session (`testscripts.dmx`) has no preserved bytes, hash or location.
   - **The remaining preserved evidence is narrower than the complete frozen clause it bears on.** Examples: live values but not serialized strings; a custom corpus but not stock content; one profile and not a recipient.

   Per Errata §3.2, whether a missing identity field matters is decided claim by claim. The absent build ID does not by itself invalidate every possible reuse. For each claim here, the combination of missing identity and narrower scope is what prevents full satisfaction.
2. **Five gates have at least one partially narrowing claim: E1, E2, E4, E5 and E7.** For E1, this is limited to the live-object side. Most of this evidence is **live-SFM object and runtime evidence**, not serialized-DMX evidence:
   - **E2:** one real 15-shot session was inventoried live (163 animation sets; 85 model-backed; 78 non-model-backed sets named as cameras and lights; 22 model names). In addition, a script-saved binary `.dmx` copy reopened in a fresh process and reproduced the control-group semantics exactly.
   - **E4:** on this machine, the game root comes from `sys.executable`, and the active mod reported by `filesystem.valve.mod()` is the relative value `usermod`.
   - **E5:** custom MDL format census. Eight v49 golden fixtures, plus a 40-file custom corpus that is 36×v49 and 4×v48. The custom corpus contains dx80/sw VTX and PHY companions.
   - **E7:** runtime identity, repeatable script-owned logging, fresh-process verification and opened-session identity methods.
3. **E1 is narrowed only on the live side.** Every preserved live model-name value is a game-relative `models/…` string (9 of the fixture's 22 models). There is **no** serialized-string inspection, **no** second-root or donor-unavailable reopen, and **no** per-slot classification. The E1 runtime experiment remains fully necessary. It can be designed with a ready-made candidate winner observable (`gameModel.GetStudioHdr().checksum`), subject to Errata C3 qualification.
4. **E3 and E6 have no existing evidence admissible as Fully satisfies or Partially narrows.** Their Hypothesis / lead records are preserved in the ledger (S0A-E3-02, S0A-E6-02, S0A-E6-03, S0A-E6-04). The corpus contains nothing on particles, audio, VCD or video. Nothing in it evaluates the pinned .NET packages. The project's Python sidecar robustness work fails E6 admissibility by construction.
5. **No Stage 0 experiment can be deleted.** Several can be reduced (§9): profile/runtime discovery, observation-method design, fresh-process control, the custom-content MDL census stratum, and the choice of in-SFM inventory tooling.
6. **Architecture-review stop: not triggered (§11).** Nothing contradicts the E1 no-DMX-rewrite assumption. The SFM-process memory crashes found in the corpus concern a different process, language and workload, so they do not contradict E6 in-process viability.

---

## 2. Corpus and provenance statement

All seven manifest repositories were searched at their pinned HEADs. All were clean before and after the harvest. Nothing under `repos\` was modified. No checkout, fetch, SFM run or web access occurred.

| Repository | HEAD | Tracked files | Commits | Relevant content found |
|---|---|---:|---:|---|
| sfm-animation-groups-master | `0e6a1e3c…` | 1454 | 201 | Real-SFM qualification ledger (A–J, O1–O3, F1-R1…R8, F2, F3, G, I, J), preserved raw evidence (G/I/J/CPM sessions, F1-1 crash log, D1 manifest), embedded-runtime audits, production Normalizer, CPM sources and logs |
| sfm-model-scanner | `b3e4ce94…` | 241 | 60 | MDL handoff v1 + 8 golden JSON, 40-file custom corpus manifest, VTX audit, MDL version-support evidence |
| sfm-package-builder | `2e773a6a…` | 267 | 3 | Frozen authority, review inputs, M13 real-fixture test, golden-oracle test, MDL reader tests, development history |
| sfm-bring-near-lights | `8ba4de33…` | 10 | 5 | Engineering notes (live Light Kit structure, temporal behavior, crash boundaries), release provenance |
| sfm-bring-near-props | `07d626de…` | 13 | 4 | Engineering notes (notification/refresh, Undo, crash guardrails), release provenance |
| sfm-character-preset-manager | `273fa816…` | 9 | 9 | Archived handoff; superseded by AGM `cpm/` |
| remove-remove-workshop-prompt | `26994029…` | 15 | 4 | Startup-entry methods, runtime versions, verification boundary |

**Method.**
- Directory inventories of every repository.
- Per-gate case-insensitive keyword sweeps with `git grep` over all tracked files. Only bulk taxonomy/fixture data was excluded: Master content audits, the Master TXT, knowledge TSVs, sidecar fixtures/qualification copies.
- Direct reading of every relevant hit.
- History inspection: commit ranges, deleted-file search (none relevant), and branch inspection (the CPM side branch is identical to `main`).
- SHA-256 of cited artifacts. These hashes identify **today's bytes only** (Errata §3.2). Where an earlier record already pinned a hash (for example the crash log, the Checkpoint A/B scripts, the Normalizer), the current bytes match it.

**Repository-local `CLAUDE.md` files** (AGM, scanner) were read as corpus. They neither override this assignment nor authorize changes.

**Provenance character of the corpus.**
- Most AGM runtime results are **operator-relayed outputs recorded contemporaneously in `LEDGER.md`**. Raw files were archived only for G, I, J, the CPM sessions and the F1-1 crash log.
- Bring Near, RRWP, CPM and PB pre-Git history are **public-safe narrative summaries**. Their raw evidence was intentionally excluded from the repositories.

---

## 3. Gate-by-gate conclusions

### E1: Absolute paths and viability

| Clause element | Status | Evidence |
|---|---|---|
| Capture genuine sessions with model/sound/map paths | Model slots partially (live only); sound/map none | S0A-E1-01 |
| Separate serialized dictionary strings from live attribute values | Live side only. Serialized strings were never read. | S0A-E1-01, S0A-E1-02 |
| Open the exact unchanged DMX on a second root with donor paths unavailable; record the winner | **Not established** | S0A-E1-05 |
| Decide provenance / portable mapping / nonportable per slot | **Not established** | — |
| C3 observable and winner oracle | Candidate method exists (`GetStudioHdr().checksum`), unqualified | S0A-E1-03 |

**Established:** in one private multi-shot session on one machine, live `DmeGameModel` model names are game-relative, forward-slash, lowercase `models/…mdl` strings. No absolute value appeared in any preserved log.

**Not established:** anything about serialized DMX strings, other slot types, or recipient behavior. Package Builder Milestone 13 never loaded content in SFM (S0A-E1-06). **E1 remains a full runtime gate.**

### E2: Corpus and completeness

**Partially established (live objects only):**
- One real session inventoried live: 15 shots, 163 animation sets, 85 model-backed with valid root control groups, 78 not model-backed, 22 models, 21 vocabularies (S0A-E2-01).
- The non-model-backed sets are **named** as cameras and lights (S0A-E2-02).
- Live classes exercised in real runs are recorded in S0A-E2-03.
- A script-saved binary `.dmx` reopened in a fresh process and reproduced the exact control-group fingerprint (S0A-E2-05).
- CPM presets are external JSON, not session-resident (S0A-E2-06).

**Not established:**
- No class → attribute → type → reachability inventory from serialized files.
- No bins, disabled content, constraints, scripted authoring fields, overrides or light-texture slots (S0A-E2-07).
- No encoding or schema-version census: the original fixture's encoding and format string were never recorded.
- No material parameter/syntax census (Errata C5).
- No held-out split.

**Tooling narrowing:** native `CElementTreeTraversal` is unsuitable for complete in-SFM walks (S0A-E2-08).

### E3: Dependency hypotheses (particles, audio, VCD, video)

**No existing evidence reaches Partially narrows or Fully satisfies** (S0A-E3-01). Every particle/PCF, WAV/event, VCD/video and map-loader clause remains open. F10–F12 remain hypotheses exactly as the Blueprint states.

### E4: Content order and installation

**Partially established (profile identity, not precedence):**
- Game root = directory of `sys.executable` (`…\SourceFilmmaker\game\sfm.exe`) (S0A-E4-01).
- Active mod per `filesystem.valve.mod()` = relative `usermod` (S0A-E4-02).

**Leads only:**
- CPM's line-based `gameinfo.txt` parsing (S0A-E4-03).
- The Normalizer's loose-only search across all `game\*` directories (S0A-E4-04).
- M13 preconditions implying loose stock tf/hl2 files (S0A-E4-05).
- Bring Near installs to `workshop\…` but uninstalls from `usermod\…` (S0A-E4-06).
- `-sfm_startup_script` / Autoinit entry routes (S0A-E4-07).

**Not established:** any precedence (A/B roots, loose/VPK/map-pak, path IDs, conditions, language), collaboration-folder behavior, session relocation or nested references (S0A-E4-08, S0A-E4-09).

### E5: Base provider baseline and format census

**Partially established, custom stratum only:**
- Eight custom MDL v49 goldens with VVD/dx90.vtx checksum identity, no PHY/ANI, zero include-models/animblocks (S0A-E5-01).
- A 40-file custom corpus: **36×v49 and 4×v48**, with dx80 (8), sw (9) and PHY (5) companions (S0A-E5-02).
- Lead: v44/v46 files exist in a real `usermod\models` tree (S0A-E5-03).

**Not established:** clean VPK directory/chunk baseline, stock MDL census, BSP anything, stock+loose override behavior, setup cost, grouped-decision ergonomics (S0A-E5-05). The model files behind both census sources are **absent** from the corpus, so their recorded hashes cannot be re-verified here.

### E6: Parser adoption and resources

**No existing evidence reaches Partially narrows or Fully satisfies** (S0A-E6-01). No pinned package was inspected, compiled, or exercised for deferred reads, malformed input, cancellation or memory.
- Package Builder's own MDL reader has malformed-input unit tests, but no recorded results and it is not the Scene parsers (S0A-E6-02).
- The SFM 32-bit address-space crashes (S0A-E6-03) and the Python sidecar robustness work (S0A-E6-04) fail applicability.

### E7: Recipient and runtime details

**Partially established:**
- Runtime identity: Python 2.7.5 / MSC v.1600 / 32-bit, LAA `sfm.exe`, PySide/Qt4. **No build ID** (S0A-E7-01).
- Repeatable script-owned logging with hashed evidence (S0A-E7-02).
- Fresh-process verification via PID plus a QObject marker (S0A-E7-03).
- Script entry quirks: no script directory on `sys.path`, `__file__` may be unset (S0A-E7-04).
- SWIG `str`-not-`unicode` arguments (S0A-E7-05).
- Opened-session identity via `GetDocumentRoot().GetFileId()` → `g_pDataModel.GetFileName()`, which returned a lowercase basename (S0A-E7-06).

**Leads:** head-time evaluation caveat (S0A-E7-07), native-crash probe hazards (S0A-E7-08), synchronous script execution (S0A-E7-10).

**Not established:** path-length limits, autosave relationship, engine console capture, `-condebug` (S0A-E7-09).

---

## 4. Claim-level evidence ledger (summary)

The full provenance for every row (all 16 required columns) is in `STAGE0A_EVIDENCE_LEDGER.tsv`.

| ID | Gate | Classification | Claim (short) |
|---|---|---|---|
| S0A-E1-01 | E1 | Partially narrows | Live model names are game-relative `models/…` (9 values) |
| S0A-E1-02 | E1 | Hypothesis / lead | No absolute model value observed |
| S0A-E1-03 | E1 | Hypothesis / lead | `GetStudioHdr().checksum` as engine-loaded winner observable |
| S0A-E1-04 | E1 | Hypothesis / lead | Normalizer handles absolute values (code intent) |
| S0A-E1-05 | E1 | Must be reproduced | No donor-unavailable second-root reopen exists |
| S0A-E1-06 | E1 | Must be reproduced | PB M13 never loaded content in SFM |
| S0A-E2-01 | E2 | Partially narrows | Live inventory of one 15-shot session |
| S0A-E2-02 | E2 | Hypothesis / lead | Excluded sets named as cameras/lights |
| S0A-E2-03 | E2 | Partially narrows | Live DME classes exercised in real runs |
| S0A-E2-04 | E2 | Hypothesis / lead | Native Light Kit live structure |
| S0A-E2-05 | E2 | Partially narrows | Script binary save/reopen preserved control-group semantics |
| S0A-E2-06 | E2 | Partially narrows | CPM presets are external JSON |
| S0A-E2-07 | E2 | Must be reproduced | No bins/disabled/constraints/overrides evidence |
| S0A-E2-08 | E2 | Partially narrows | `CElementTreeTraversal` unsuitable |
| S0A-E2-09 | E2 | Hypothesis / lead | ~4,250-element live shot graph |
| S0A-E3-01 | E3 | Must be reproduced | No particle/audio/VCD/video evidence |
| S0A-E3-02 | E3 | Hypothesis / lead | PB install-root list (code intent) |
| S0A-E4-01 | E4 | Partially narrows | Game root from `sys.executable`; install layout |
| S0A-E4-02 | E4 | Partially narrows | `filesystem.valve.mod()` = `usermod` (relative) |
| S0A-E4-03 | E4 | Hypothesis / lead | CPM gameinfo SearchPaths parsing (code intent) |
| S0A-E4-04 | E4 | Hypothesis / lead | Normalizer loose-only model search; stock props found loose |
| S0A-E4-05 | E4 | Hypothesis / lead | M13 preconditions: loose stock tf/hl2 files |
| S0A-E4-06 | E4 | Hypothesis / lead | workshop vs usermod install conflict |
| S0A-E4-07 | E4 | Hypothesis / lead | Startup-script entry routes |
| S0A-E4-08 | E4 | Must be reproduced | No precedence/mount evidence |
| S0A-E4-09 | E4 | Must be reproduced | No relocation/nested-reference evidence |
| S0A-E5-01 | E5 | Partially narrows | 8 custom v49 goldens + companion checksum facts |
| S0A-E5-02 | E5 | Partially narrows | 40-file custom census: 36×v49 / 4×v48; companion forms |
| S0A-E5-03 | E5 | Hypothesis / lead | v44/v46/v48 in a real usermod (Audit 02A lost) |
| S0A-E5-04 | E5 | Hypothesis / lead | PB golden-oracle parity test exists, no result |
| S0A-E5-05 | E5 | Must be reproduced | No VPK baseline / stock census / BSP |
| S0A-E6-01 | E6 | Must be reproduced | No pinned-package investigation |
| S0A-E6-02 | E6 | Hypothesis / lead | PB MDL reader malformed-input tests |
| S0A-E6-03 | E6 | Hypothesis / lead | SFM 32-bit VAS crashes (not applicable to E6) |
| S0A-E6-04 | E6 | Hypothesis / lead | Python sidecar robustness (fails admissibility) |
| S0A-E7-01 | E7 | Partially narrows | Embedded runtime identity (no build ID) |
| S0A-E7-02 | E7 | Partially narrows | Repeatable script-owned logging |
| S0A-E7-03 | E7 | Partially narrows | Fresh-process verification method |
| S0A-E7-04 | E7 | Partially narrows | MAINMENU script path/`__file__` behavior |
| S0A-E7-05 | E7 | Partially narrows | SWIG `str` argument requirement |
| S0A-E7-06 | E7 | Partially narrows | Opened-session identity read; lowercase basename |
| S0A-E7-07 | E7 | Hypothesis / lead | Head-time evaluation caveat |
| S0A-E7-08 | E7 | Hypothesis / lead | Native-crash probe hazards |
| S0A-E7-09 | E7 | Must be reproduced | No path-length/autosave/console evidence |
| S0A-E7-10 | E7 | Hypothesis / lead | Synchronous script execution, no cancellation |

Count: **0 Fully satisfies · 16 Partially narrows · 20 Hypothesis / lead · 9 Must be reproduced.**

---

## 5. Contradictions and profile splits

1. **Script install root (S0A-E4-06).** Both Bring Near releases extract to `game\workshop\scripts\sfm\animset\`, but their uninstall text points to `game\usermod\scripts\sfm\animset\`. No runtime record shows which root SFM loaded the script from. This is preserved as an internal documentation conflict. It is a lead that more than one mod root may be script-searched.
2. **Two resolution procedures were never compared (S0A-E4-04 vs S0A-E1-03).**
   - The Normalizer chooses its own loose-file candidate across `game\*` directories.
   - CPM reads the engine-loaded checksum.

   No record compares the two. Either could disagree with the engine winner when shadowed copies exist.
3. **Companion-form profile split (S0A-E5-01 vs S0A-E5-02).** The 8-model golden corpus has only dx90.vtx and no PHY. The 40-model corpus has dx80/sw VTX and PHY. Both are corpus-specific; neither represents stock content.
4. **Session-name case (S0A-E7-06).** The script saved `F1_R2_NORMALIZED_DIAGNOSTIC_COPY.dmx`. Later, the datamodel reported `f1_r2_normalized_diagnostic_copy.dmx`. The on-disk name was not independently re-checked, so this is an observation about the datamodel registry, not proof of a renamed file.
5. **Session extension (S0A-E2-05).** A `.sfm`-extension DataModel save succeeded, but the file could not be reopened through normal session loading. The `.dmx` save could. The cause ("loader expects `.dmx`") was inferred by the project, not isolated.
6. **Historical failures preserved, not superseded:**
   - F1-1 crash → F1-R1…R8 attribution → F2-R1-R3 FAIL. Same-process reinvocation is not reliably sustainable in the 32-bit SFM process.
   - I-1 startup failure before I_PASS.
   - F1-R2 attempts 01/02 before 03.

   These remain evidence of runtime behavior and harness hazards. None is reinterpreted.

---

## 6. Reusable fixtures, scripts and methodologies

Every item below is reusable as **method or fixture input**. Each must still be qualified for the specific Stage 0 clause it serves; none is a pass by itself.

| Asset | Location (repo) | Reuse for | Caveat |
|---|---|---|---|
| Read-only live inventory pattern (`sfmApp.GetShots`, `shot.animationSets`, `gameModel`, root group) | AGM `checkpoint_b/Checkpoint_B_Independent_Inventory.py` (sha `5d2782bd…`) | E2 live-side capture, kept separate from the serialized side | Live objects only |
| Opened-session identity (`GetDocumentRoot().GetFileId()` → `g_pDataModel.GetFileName`) | AGM `checkpoint_g…/Checkpoint_G_Later_Vocabulary.py` | E1/E7 "exact DMX identified" | Basename case-normalized |
| Engine-loaded model checksum (`GetStudioHdr().checksum`) | AGM `cpm/baseline/…G18AN` | E1/E4 winner oracle candidate (Errata C3) | Must be qualified inside E1; only discriminates differently compiled variants |
| Fresh-process proof (PID + `main_window` QObject marker) | AGM `checkpoint_f2_r1_marker_probe/` | E1/E4/Q15 cache-masking control | Process freshness ≠ no disk caches |
| Immutable evidence discipline (write-once files, SHA-256 manifest, snapshot schedule) | AGM `checkpoint_process_attempt_guard/`, `*_pass_evidence/MANIFEST_SHA256.txt` | All Stage 0 runtime records | — |
| Exact-path, hash-pinned helper loading for MAINMENU scripts | AGM `checkpoint_i_generation_replacement/` | Any in-SFM probe | — |
| Embedded-runtime identity capture | AGM `docs/qualification/SFM_MASTER_SIDECAR_GATE1_H1_PY27_RUNTIME_AUDIT.md` | E7 profile record | Add Steam build ID next time |
| Probe hazard list (`allWidgets`, delayed `SelectDag`, deep log introspection, unicode SWIG args, head-time sync) | BNP/BNL engineering notes; AGM LEDGER | E1/E2/E7 harness design | Narrative sources |
| MDL v49 golden oracle + PB parity test | Scanner `handoff/golden/*.json`; PB `MdlGoldenOracleIntegrationTests.cs` | Stage 3 model readers; E5 custom stratum | Source MDLs absent from corpus |
| Custom-corpus MDL manifest with per-file SHA-256 | Scanner `phase1f_unseen_corpus_manifest.tsv` | E5 custom-stratum census seed; E2/E5 fixture selection | Files absent; provenance unknown |
| Qualification fixture session (15 shots, cameras, lights, rigged characters) | AGM LEDGER (B-2, D1 manifest) | E2 discovery-set candidate | Bytes, location and rights not recovered; private assets |

---

## 7. Evidence that fails admissibility, and why

| Apparent evidence | Gate it seems to address | Why it fails |
|---|---|---|
| PB Milestone 13 "37/37 criteria" using installed SFM assets | E1/E4/E5 | No SFM/HLMV load (the project's own boundary statement). The report is not in the corpus. The install was never identified. |
| Normalizer `_gate_loose_candidates` / CPM `tool_sfm_search_roots` | E4 | Code intent and project-authored resolution, not engine precedence (manifest rules 7, 10). |
| AGM sidecar reader corruption/resource tests, Python 2.7 qualification | E6 | Unrelated format, Python not .NET, no pinned package (Errata §3.3 E6 rule; assignment instruction). |
| SFM 32-bit VAS exhaustion and crashes (F1-1, F2-R1-R3) | E6 | Subject is the SFM process running Python traversal, not Package Builder's separate 64-bit .NET process. |
| Bring Near / RRWP "Observed" statements | E2/E7 | Raw evidence intentionally excluded. No dates and no SFM build. Admissible only as leads. |
| Bring Near / Props releases | E4 | A released tool is not proof that its install location was qualified (manifest rule 8). The install/uninstall paths also conflict. |
| `mdl_versions.py` citing "Version Coverage Audit 02A" | E5 | The audit record is not preserved. No file hashes. The install is not identified. |
| PB `MdlV49MetadataReaderTests`, `MdlGoldenOracleIntegrationTests` | E5/E6 | Test source only. No preserved run result. The oracle test goes Inconclusive without its MDLs. |
| LEDGER rows A-2, B-2, F1-R2, F1-R8, marker probe | E2/E4/E7 | **Partly admissible.** They are contemporaneous, specific records with pinned script hashes, but raw outputs are "not separately archived". Reuse is limited to the narrow facts recorded (Errata §3.2 items 2/5). |
| `Claude Audit.txt`, `Output.txt` | all | Review inputs. They are not runtime evidence (Blueprint baseline table). |

---

## 8. Exact remaining evidence gaps

**E1**
- The serialized form of every path-bearing slot in genuine sessions (model, sound, map, material override, light texture).
- A donor-unavailable second-root reopen of byte-identical DMX in a fresh process.
- A qualified winner observable per slot, plus negative controls.
- Per-slot classification.

**E2**
- A serialized class → attribute → type → reachability inventory across all listed strata.
- An encoding/schema-version census (binary N / keyvalues2 N; `sfm_session` version).
- Bins, disabled content, constraints, scripted authoring fields, overrides and lights.
- Load-time behavior of non-timeline content.
- Material parameter/syntax census (Errata C5).
- Discovery/held-out split.
- False-block measurement.

**E3** — every clause: embedded vs external PCF; WAV vs event playback; VCD/video provenance vs live slots; session vs map loaders.

**E4**
- Shipped SearchPaths and mount order, read from the real `gameinfo.txt`.
- A/B distinctive same-path precedence across loose, VPK and map pak.
- Path IDs, conditions, tokens, wildcards, language and startup changes.
- Collaboration folder vs usermod.
- Whether `workshop` is a mounted root.
- Session subdirectory relocation and nested references.

**E5**
- A clean entitled VPK directory/chunk SHA-256 baseline **with build identity**.
- Stock MDL and BSP version census.
- Stock+loose override behavior.
- Setup cost.
- Grouped-decision ergonomics.

**E6** — every clause, including the C2 representative graph, C6 decision evidence, and a dated security audit.

**E7**
- Recipient path-length boundaries including the extractor.
- Autosave location, naming and freshness.
- Engine console capture method (including whether `-condebug` works) with a missing-resource baseline.
- Pre-install negative-control procedure.

**Cross-cutting:** record the SFM Steam build ID and engine version for every runtime run (Errata §3.2 item 3).

---

## 9. Experiments: delete, reduce or keep

**Deleted: none.** No existing record satisfies a full frozen clause, so the Errata's "do not repeat valid evidence" rule removes no experiment.

**Reduced.** Each reduction cites the evidence that makes part of the work unnecessary. Each still requires the narrow remaining condition.

| Stage 0 work | Reduction | Evidence | Still required |
|---|---|---|---|
| E4/E7 profile discovery on the qualification machine | No separate exploratory effort is needed to work out *how* to obtain the game root (`sys.executable` directory), the active mod (`filesystem.valve.mod()`), embedded Python/Qt identity, or the script entry and harness mechanics | S0A-E4-01, S0A-E4-02, S0A-E7-01, S0A-E7-04 | The historical build/profile is incompletely identified, so the historical values are not facts that never need observing again. The next qualified runtime runs must record the actual current values together with the Steam build ID. Also required: launch options, the real `gameinfo.txt` contents, and any other recipient profile. |
| E1 observation-method design (C3) | Start from the existing candidates: engine-loaded checksum, opened-session identity, PID/marker fresh-process proof, script-owned logging | S0A-E1-03, S0A-E7-06, S0A-E7-03, S0A-E7-02 | Qualify discrimination, negative controls and material/texture/audio observables inside E1 |
| E2 live-side capture tooling | Reuse the Checkpoint B enumeration pattern. Do not re-evaluate `CElementTreeTraversal` for complete walks. | S0A-E2-01, S0A-E2-08 | The whole serialized inventory and every stratum |
| E5 custom-content MDL census stratum | Seed it from existing manifests (36×v49 / 4×v48; companion forms) instead of a fresh scan, if the same files are recovered and re-hashed to match | S0A-E5-01, S0A-E5-02 | File recovery + hash match; stock census; VPK baseline; BSP |
| E7 logging methodology | "Repeatable logging" of script-observable state is already demonstrated | S0A-E7-02 | Engine console capture and missing-resource diagnostics |
| Runtime harness hazard discovery | Skip known crashers and binding pitfalls | S0A-E7-05, S0A-E7-08 | — |

**Kept in full:**
- E1 donor-unavailable runtime test.
- E2 serialized corpus and completeness work.
- All of E3.
- E4 precedence, collaboration-folder and relocation tests.
- E5 baseline/BSP census.
- All of E6, including the C2 representative graph and C6 decision.
- E7 path-length, autosave and console capture.

---

## 10. Missing or unrecoverable provenance

| Expected evidence | Status |
|---|---|
| SFM Steam build ID / engine version for any run | **Never recorded.** `appmanifest_1840.acf` was consulted only to locate the install. |
| `testscripts.dmx` bytes, hash, encoding, location | NOT RECOVERED |
| `F1_R2_NORMALIZED_DIAGNOSTIC_COPY.dmx` bytes / directory | NOT RECOVERED. Same directory as the original, which is unknown. |
| Checkpoint A-2, B-2 raw output files | Not archived ("verbatim reported…not separately archived") |
| F1-R2 Phase 1/2, F1-R8, marker-probe result JSON | Not archived. The LEDGER F1-R2 evidence cell is still a placeholder. |
| D1-3 artifact (sha `55cd0447…`) | Not in repo; only the derived `d1_comparison_manifest.json` |
| Scanner `samples\*.mdl` and `samples\unseen\*.mdl` | Never committed. Hashes recorded only. |
| Version Coverage Audit 02A | Not preserved |
| PB Milestone 13 report and test output; M13 date | Not in corpus |
| Bring Near T-series, RRWP original real-SFM records, CPM pre-Git records | Intentionally excluded; dates NOT RECOVERED |
| Observation dates for the GATE1/GATE2 audits and the F1-1 run | Not stated. Commit dates are upper bounds only and are **not** backfilled. |

---

## 11. Architecture-review stop assessment

**STOP FOR ARCHITECTURE REVIEW: not triggered.**

**E1 no-DMX-rewrite assumption.** No preserved evidence shows an absolute or donor-specific path that SFM needs at runtime.
- The only path-bearing values observed are live, game-relative model names (S0A-E1-01).
- These are consistent with, but do not prove, portability.
- The absence of contrary evidence is not proof (S0A-E1-02). E1 must still run.

**E6 in-process parser viability assumption.** The corpus contains severe memory and address-space failures (S0A-E6-03), but they arose:
- inside SFM's own 32-bit `sfm.exe`;
- from repeated Python whole-scene traversal of live DME graphs;
- under a process already holding ~3 GB.

Package Builder's planned parsing runs in its own self-contained win-x64 .NET process, reading files, not live SFM objects. That evidence therefore does **not** materially contradict the E6 assumption. It should be cited only as a reason to keep E6's measured-budget requirement intact. It also bears on E1/E7 runtime test design (fresh processes; large sessions can exhaust SFM itself).

---

## Completion checks (performed)

- All seven repositories searched with inventories, per-gate sweeps and history inspection (§2).
- E1–E7 each explicitly concluded (§3).
- Fully satisfies claims: **none**. The provenance check is vacuously met; no claim was promoted without the Errata §3.2 fields.
- Experiment deletions: **none**. Reductions cite evidence IDs (§9).
- `git status --short` is empty for all seven repositories after writing. The output files' SHA-256 values are reported in the session.
