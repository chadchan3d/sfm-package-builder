# E1 Absolute-Path Portability Experiment Design

Revision after independent adversarial review and bounded final review, 2026-10-09. **Proposed for freeze.** Design only: no fixture creation, tooling implementation, SFM execution, experiment or architecture change is authorized or claimed by this document.

This revision supersedes the 2026-10-08 proposal (SHA-256 `99846B3392E8BFA5E361340C35DAC5A16430A531947069A3A2110162CF1C1D19`). The reviewed Claude audit has SHA-256 `08D00DDC845AFF324B7D046A6D731C02F50921B3955415FA09D0FDAEFE660CCC`. Its findings are review input. Section 15 records the design authority's individual dispositions.

## 1. Authority, evidence and scope of this revision

Inspected checkout: `chadchan3d/sfm-package-builder`, local `main` at `1e8e5f1a1c6f907dc665afad728809dfd7249918`, clean at review. These local committed authority and accepted-evidence files retain their verified hashes:

| Document under `docs/scene-package/` | SHA-256 |
|---|---|
| `SCENE_PACKAGE_INTEGRATION_BLUEPRINT_REVISION_2.md` | `64DD9981B0C4A8B59D3B11DEB0C708FF7B59B8E933D28517B96214E50976015C` |
| `REVISION_2_FREEZE_ERRATA_AND_AUTHORITY_RECORD.md` | `4C43CC7DDE185B52C506F0CE6BDC7060113D22BEA582DD2FA93294CDE3BF0099` |
| `stage0/STAGE0A_EXISTING_SFM_EVIDENCE_HARVEST.md` | `52B7E0E04AAA2E5172F741DFF5E9234C0D85D414E7B66F5368545822C60C0C3C` |
| `stage0/STAGE0A_EVIDENCE_LEDGER.tsv` | `64D71AE8DB796A93C0CEE8FD02CD6A85BA9854CAC4B4D54B8C10F3E7A82494A7` |

Precedence: Freeze Errata, then unaffected Blueprint clauses. Stage 0A supplies accepted historical evidence at its recorded scope. Neither this protocol nor the audit changes the frozen architecture.

Use the frozen labels **[PB]**, **[OLD]**, **[SRC]**, **[REC]**, **[GATE]**, **[HYP]**. **[S0A]** below means a specifically cited accepted ledger claim with its original limitations. Unlabelled prescriptions are **[REC]**, not measured facts.

New evidence has four distinct channels:

- **SERIALIZED:** sealed DMX bytes, structurally associated fields and byte spans.
- **LIVE:** raw running-SFM values and physical visual/audio observations.
- **ENVIRONMENT:** input hashes, actual filesystem/provider/process/build/configuration facts.
- **INFERENCE:** bounded conclusions linked to observations, controls and alternatives.

A live name is not proof of its serialized representation. A configuration is not proof of a winner. File SHA-256 and an engine MDL checksum are different identities.

| Authority clause | Required protocol response |
|---|---|
| Blueprint section 9 E1: genuine model/sound/map sessions; saved versus live values | Ordinary-location native authoring; three isolated sealed sessions; structural inspection |
| Section 9 E1 and section 8.2: unchanged session on another root, donor unavailable | Receiver-root transfer and candidate-specific donor-anchor checks |
| Errata C3 / section 3.4: independent winner observation, negative controls, fresh process | Calibrated A/B/absent observables; sound-chain positive control; six-row matrix |
| Errata C1 / sections 3.1-3.3: reuse evidence at actual scope | Section 2; no repeated API discovery or invented historical identities |
| Errata C13 / section 5: newly found absolute slots reopen E1 | Slot-specific reopening, including support-map references |
| Blueprint section 9 E4 | General multi-root precedence and mount-permutation study remains E4 |
| Original E1 assignment: identify winners when multiple plausible copies exist | Conditional candidate-discrimination branch in section 9; no claim of multi-copy qualification from the single-provider baseline |
| Blueprint section 11 Q02/Q38, bounded by Errata C3 | Predetermined observable outcomes; E1 does not depend on E7 console capture |

The previous proposal unnecessarily made two-provider explanation a universal E1 closure condition. This revision removes that condition. It retains an actual-winner requirement whenever competing candidates exist; a smaller baseline does not waive a necessary conditional control.

## 2. Reused evidence and historical leads

| Accepted record | Reuse | Still unresolved |
|---|---|---|
| S0A-E1-01 | Live game-relative model values observed in one session | Serialized values, current fixture and receiver behavior |
| S0A-E1-03 | Candidate `gameModel.GetStudioHdr().checksum` route | On-disk association, A/B distinction, missing-case interpretation |
| S0A-E7-06 | Document-root file ID to DataModel filename | Current return value and exact full-path/byte identity |
| S0A-E7-03 | PID plus retained QObject marker | Current freshness; disk caches require separate controls |
| S0A-E7-02 | Script-owned write-once logs and hash manifests | Current helper/output integrity; not engine-console capture |
| S0A-E4-01/02; E7-01 | Methods for executable/game root, active mod and runtime identity | Actual current values, build ID, launch configuration |
| S0A-E7-04/05/07/08 | Entry, SWIG, evaluation and introspection hazards | Exact-path helper loading and bounded safe observation |

Do not rediscover these APIs through broad live exploration. Inspect the cited helper patterns when implementation is authorized and qualify their narrow use on the new fixture. The original `testscripts.dmx` is not needed. S0A-E1-05/06 leave the donor-unavailable runtime clause open.

**[HYP]** Blueprint section 3.3 records the historical packager's installation-prefixed absolute-model filter, bare-map-name and sound-relative-path leads. These guide ordinary-location fixture selection and raw-value inspection. They do not establish how SFM serializes or loads any slot. The offset-54 heuristic is not a decoding method.

**[HYP]** S0A-E1-02/04 likewise supply no proof that absolute references are absent or portable. No build identity, date or input hash is retroactively invented.

## 3. Profile, hypotheses and initial slot scope

**P1** is the owner's identified Windows Source 1 SFM build, native `sfm_session` saves, simple active timeline references, ordinary donor content placement and controlled loose receiver content. Record actual encoding/version, session schema, active mod and build; do not presume binary 5, a schema number or `usermod` merely from historical observations.

Mandatory slot families:

1. **M:** one active model animation-set reference.
2. **S:** one active direct-file WAV clip; no named-event hypothesis.
3. **B:** the map reference required by one active shot/session.

Three sessions isolate map failure from M/S observations. This satisfies section 9 E1's explicit model/sound/map scope. A fixture is a sample of a named ordinary authoring route, not a census of ordinary sessions.

Materials, overrides, light textures, nested sessions and other classes remain E2 inventory work except when an actual absolute/topology-sensitive candidate is encountered in these fixtures. Register and qualify that slot before claiming it harmless or portable. Newly discovered E2 candidates reopen only their unproven E1 scope.

| ID | Hypothesis and required discriminator |
|---|---|
| H1 | Each tested occurrence has a structurally identifiable saved reference; a dictionary hit alone cannot satisfy it. |
| H2 | The unchanged ordinary-authored DMX uses intended A at the receiver with relevant donor locations/anchors unavailable. |
| H3 | The selected oracle distinguishes A, B and neither under the actual context. |
| H4 | Replacing A with B at the same receiver path changes the identified result while DMX/configuration remain fixed. |
| H5 | Absolute or topology-sensitive strings have a distinguishable role: provenance, portable runtime mapping, donor-bound requirement, or unresolved. |
| H6 | Before/after absence controls, process identity and support checks exclude cache, leftover-donor and observation-chain false passes. |

No hypothesis assumes search-root priority or that a missing absolute string is automatically stripped to a virtual path.

## 4. Fixtures and ordinary-location authoring

### Definitive donor placement

Use the actual installed active-mod content location, selected and verified before fixture preparation, as **D**. If it is `usermod`, record that as a current fact. Add only fresh experiment-owned files under a nonce; leave the donor's normal mount configuration unchanged. Do not author the definitive fixtures through a new off-install donor mount.

This replaces the prior custom-root donor entirely. Paired custom-root and ordinary-root saves are unnecessary for this initial gate. Even identical serialized strings would not by themselves establish equivalent mount/loader context. A separate nonstandard authoring route can be qualified later if it becomes a claimed supported route.

Use owned simple assets, ASCII names and one frozen namespace `e1_<nonce>`. Preserve compile sources/receipts and rights. Keep source assets, alternatives and evidence outside engine lookup in an unmounted private store **Q**.

| Session | Intended content layout, not assumed saved syntax | Target checkpoint | Fixed support |
|---|---|---|---|
| `e1_<nonce>_model.dmx` | `models/e1_<nonce>/probe.mdl` | Static camera at 1 second; unmistakable geometry | Known-working base map, simple material/light |
| `e1_<nonce>_sound.dmx` | `sound/e1_<nonce>/probe.wav` | Target playback at 0-4 seconds | Same base map; no other timeline audio or audible map ambience; separate unsaved positive audio control |
| `e1_<nonce>_map.dmx` | `maps/e1_<nonce>_probe.bsp` | Fixed camera at 1 second | Minimal known-working map support |

Use a **flat BSP filename**. Testing nested map names adds an unnecessary fixture assumption. Companion/sidecar names must match the actual compiled map name. Record the exact stored map/sound prefixes and extensions without forcing them to match the layout column.

### Two discriminating variants per slot

- **M-A/M-B:** one versus two conspicuous geometric pillars, same virtual model name and identical trivial rig contract. Use the same single root bone, bodygroup/skin/attachment structure; omit flex, animation and extra rig features. Use identical support-material bytes and complete matching compiled families. Prefer readily produced v49 fixtures, whose header reader is already inspected; record the actual version. E1 portability is not inherently v49-only: another engine-accepted version may use the qualified visual oracle, with no new general MDL reader. Never read its checksum using an unqualified v49 layout.
- **S-A/S-B:** same-length, same-format PCM WAVs, two low-frequency pulses versus three high-frequency pulses. Record exact sample rate/channels, amplitude, pulse count, frequency band and time windows. Start distinguishable content at the earliest practical capture-resolvable onset; one mathematically identical first sample or a fade is not a gate failure. Do not rely on a long shared silent prefix.
- **B-A/B-B:** two independently compiled simple maps with the same flat virtual name, one versus two world-geometry pillars, fixed camera coordinates and identical supporting material expectations. The discriminator must be world geometry rather than a DMX model or changed texture.

For each variant retain all required companions/sidecars, compile inputs/tool versions/hashes/options, size/SHA-256 and support inventory. Never patch checksum fields or combine compiled families.

Where a format-qualified MDL/VVD/VTX header checksum check already exists, record family consistency. Otherwise matching compiler outputs, native positive loads and a clear visual oracle are sufficient for E1; do not create general VVD/VTX inspection work here. A detected mismatch invalidates the fixture.

Natively select A through the ordinary model browser, direct-sound authoring route and map selector. Record the selection input and route. Save the three sessions in D's ordinary session location, close SFM, seal masters and calculate size/SHA-256. Record any absolute input that becomes a different serialized form. Do not manually inject an absolute reference.

All definitive runs open copies of those sealed bytes. Calibration saves, if needed, have different IDs and never replace the original fixture implicitly. No receiver edit, conversion output, autosave or resave may become the tested session.

### Support occurrences are part of preflight

Inventory the base-map slot in both M and S, including its serialized and live values. Inventory other path-bearing support fields in these small fixtures; no full E2 corpus is required. If the base map or another needed support slot carries an absolute/topology-sensitive value, apply section 5 to it before accepting M/S. A target sound/model failure caused by an unqualified support-map location is not evidence about the target.

## 5. Donor unavailability and receiver arrangement

### Relative references

For a proven ordinary game-relative slot, use one receiver content root **R**, physically different from D, with the same content-relative asset and session layout. The receiver DMX must have a different absolute path. Only R contains the target candidate during receiver tests.

A same-engine-installation test can qualify this relative case if every original experiment-owned donor target/family is absent, no alternative provider contains it, and the occurrence-wide eligibility rule below is satisfied. D's ordinary root may remain installed/mounted for fixed support; report that fact. This is not an absolute-anchor portability test.

**Occurrence-wide eligibility:** a relative runtime field does not by itself qualify an occurrence for a same-installation portability claim. Every absolute or topology-sensitive value structurally associated with that occurrence must have been shown non-runtime in the tested context by a section 9D discriminator. If any such value has a runtime or unresolved role, any portability claim for the occurrence requires the anchor-independent receiver conditions below for all associated candidates not shown non-runtime, together with the applicable positive/negative controls; otherwise record an evidence hold. Record the associations and discriminator evidence explicitly. Mere co-location in the file is not structural association, and a same-installation success is not a non-runtime discriminator.

This condition also governs the narrower claim "fixture portable; candidate role unresolved." Select the qualifying receiver environment before the baseline where possible. If the issue is discovered later, repeat only the affected receiver rows/controls whose environment no longer supports the claim; one isolated positive run does not replace the required controls. The unconditional baseline remains 18 runs. Additional receiver work is conditional on the discovered candidate; same-installation observations can be retained without a portability conclusion.

Prepare a reversible receiver launch/content configuration with an explicit R lookup route or an independently prepared same-build receiver installation. Back up and hash configuration first, preserve unrelated declarations, and record actual active-mod/launch identity. Do not assume a switch or mount took effect. Positive rows verify access; a failure triggers the independent control in section 9. No broad E4 mount experiment is needed.

Keep receiver configuration and session path fixed throughout its baseline matrix. Place A, B or no target at the same R location between fresh processes. There is no mandatory second receiver root.

### Absolute and topology-sensitive candidates

File-not-found alone is insufficient when resolution could depend on a still-present donor installation/mod/search-root anchor. For each candidate, record:

- exact serialized spelling and structural role; raw value is never overwritten by normalization;
- normalized comparison form plus actual alias/reparse/drive/UNC mappings;
- donor installation, active-mod, provider and relevant session-directory anchors;
- receiver installation, active-mod and effective search roots, including wildcard expansion and relevant containers;
- exact equality, ancestor/descendant overlap and aliases between the meaningful donor and receiver anchors;
- target existence/read outcome and whether the old anchor remains present, mounted, configured or aliased.

These are component-aware **semantic anchors**, not every character prefix. A common drive letter, filesystem root or generic parent such as a library directory does not automatically invalidate a run. Conversely, case differences, alternate spellings or a junction do not establish independence.

To claim **portable absolute mapping**, the old absolute target and the meaningful donor-specific anchor must be unavailable and not recognized through a receiver mount/configured alias. An installation-prefixed candidate therefore needs a same-build receiver at a genuinely different absolute installation location, with the old install anchor unavailable in that receiver environment. A different computer using the same full Steam path does not qualify. Merely removing the target under a still-mounted old active mod qualifies neither anchor independence nor a general mapping.

If the single installation cannot meet this condition without disrupting working content, use an isolated receiver machine or separately prepared installation with the old anchor unavailable. Do not rename the owner's working installation, remove stock files or fabricate a donor-path junction. Until that environment exists, record **evidence hold: donor anchor still available**, not pass or nonportable.

Some directory ancestry is necessarily shared on one host. If the candidate's relevant mapping anchor is unknown, do not guess that a conveniently removed suffix was the anchor: keep absolute-mapping classification unresolved and use the isolated environment or a narrowly qualified discriminator.

Also classify unusual measured forms: drive-relative paths, root-relative paths without a drive, dot segments, UNC/device forms, and mod/game-directory-qualified relative paths. They are **topology-sensitive candidates**, not automatically absolute, unsafe, nonportable or equivalent to normal virtual names. Bind any demonstrated behavior to the measured form/context; do not sanitize it into a passing fixture. E4 can later generalize lookup rules.

### Before and after every receiver row

1. Verify the session copy equals the sealed master and that the full path opened is the intended one.
2. Verify original target families are absent, and for applicable candidates verify donor-anchor independence as above. Access denied is not physical absence.
3. Inventory exact candidate/support hashes, mount/configuration identities and all plausible providers for the fresh namespace. Check relevant container membership where necessary. A nonce is a convenience, not proof of uniqueness.
4. Record aliases and the absence of Q or any donor backup from lookup. Leave recoverable copies outside engine reach.
5. Record no prior SFM process still running, plus cache/output state observable without assuming particular cache filenames.

Only experiment-owned namespace files are quarantined/restored. Never remove D's whole ordinary content root. Whole-original-installation absence, when required, is supplied by the receiver environment rather than destructive edits to the donor.

## 6. Serialized-byte observation without E6 adoption

Inspect the actual header, encoding/version and session format/version before choosing an observation method. The requirement is a verified association of **element/class -> attribute/type -> value -> original byte evidence**. It is not a requirement to adopt a particular library.

Admissible tools, each pinned/hashed and narrowly qualified:

- a read-only dumper using the candidate KeyValues2 1.0.0 when it works on the observed file;
- a bounded record walker limited to that observed encoding;
- manual structural/token-span inspection for small textual DMX;
- an identified bundled DMX converter, if actually present and its behavior qualified, producing a separate inspection copy only as a cross-check.

No converter availability, command syntax or faithful conversion is assumed. Converter output is never the definitive runtime input or sole evidence for original string spelling. A generic decoder need not expose byte offsets itself: an independent record check can supply them.

For every tested/support candidate, retain element ID/class, exact attribute/type/index, reference links to its root/shot/clip, raw string/code units, and duplicate occurrences. Keep a separate dictionary/string inventory to locate candidates, without treating it as semantic extraction. Force relevant deferred attributes to be read.

Independently check original token spans for text; for binary check the corresponding element/attribute/type records and string-table index/inline value against raw spans using a layout identified for that encoding. Checking the same decoder twice is not independence. A native-authored calibration save or read-only live Element Viewer comparison may corroborate association but cannot replace the original-byte check.

Hash inputs before/after every inspection. No save/serialize call touches them. No offset-54 scan, regex association or heuristic next-string rule is admissible.

A KeyValues2 failure disqualifies that method for the fixture; choose another narrow qualified method. E1 is held only if **no available method establishes the required serialized evidence**, not because E6 has not adopted the candidate package. A successful tool run likewise does not adopt it.

Retain exact package/tool/source identity and failures. Under Errata section 3.3, such evidence may later narrow an E6 claim at its actual scope. It cannot certify E6's production graph, generators, malformed-resource safety, cancellation, security or publish behavior. Claude's blanket proposal that E1 output can never be E6 evidence is rejected in favor of the frozen claim-level reuse rule.

## 7. Observables and their qualification

### Fresh process and session identity

At each launch record executable/module hashes and available engine versions, Steam build ID, OS, actual active mod, launch command, gameinfo/configuration hashes and embedded-runtime identity. Missing human-readable version information is marked missing; executable/module identities are not invented labels.

Record PID and creation time. First invocation of the bounded MAINMENU helper must find the experiment QObject marker absent and install it on the known main window; subsequent observations find the same marker. Close the process before file changes. This qualifies freshness, not absence of disk caches.

Read the known DataModel document-root/file-ID filename. Log its raw value. Also capture the exact File > Open full-path selection externally and hash that file. If DataModel yields only a lowercase basename, require unique available fixture basenames and preserve the physical full-path selection. No autosave/recent substitution or hash inference from a basename.

The helper observes only known fixture elements. It does not repair paths, reload resources, save the document or choose candidate files on disk and call them engine winners. Use explicit helper paths/hashes; avoid deep traversal, broad widget enumeration and unqualified delayed API calls.

Capture the paired serialized and live values for each occurrence at D0 and every receiver row, explicitly including R1. A value changed at load is a mapping lead. A live value remaining unchanged does not prove no internal mapping, and a changed value does not identify the loaded file without the winner oracle.

### Model

The geometry signature can satisfy the E1 model clause when A/B/absence and environment controls make it unambiguous. It need not wait for a working checksum API.

Qualify `GetStudioHdr().checksum` separately before relying on it. **[PB]** the existing `Core/Mdl/MdlV49MetadataReader.cs` checks IDST/v49 and reads the signed 32-bit header field at offset 8. Use that inspected layout only for matching v49 fixtures. Record raw bits, signed/unsigned form and whole-file SHA-256 for each MDL. Distinct file hashes do not imply distinct MDL checksum values.

Read the actual test gameModel's header result/error after physical evaluation in D0, R1, R2 and the absence rows. Qualification requires distinguishable A/B header values associated with the corresponding disk bytes and geometry; normalize signedness by bit pattern. Record missing behavior without prescribing an invented sentinel.

If the checksum is unavailable or non-discriminating, label it unqualified and use the predeclared independently qualified geometry method. If it **contradicts** geometry, do not discard the inconvenient observation: hold the row, examine family integrity/method association, and resolve the conflict before admitting either claim. No new general companion/version inspector is required for E1.

### Sound and same-process positive control

Use actual target timeline playback at the frozen interval, with loopback or physical recording. Freeze capture device/gain, pulse counts, bands, windows, tolerances and an explicit inconclusive category before results. Fixture-generated source samples supply the initial signature; donor and receiver positive captures validate the observation chain. Do not claim waveform byte identity across devices.

**Every sound row includes a same-process positive control after the target interval, in the same continuous recording.** Play one identified, hash-recorded non-target stock sound through a narrowly qualified engine preview/play action. It is outside the experiment namespace, remains installed, and is never saved into the sealed session. Record the action and a distinct nonoverlapping control window.

Before interpreting silence, verify that the actual target clip is active, correctly timed, unmuted and at its fixed gain, that timeline playback traversed it, and that the control was heard. A preview control proves only the shared engine/output/capture chain; it cannot excuse a muted or unevaluated target track. Use UI observation or bounded read-only fields, not a new whole-scene probe. If the chosen action bypasses the engine/output path under test, choose a more appropriate engine action or hold the row.

A valid sound negative has neither A nor B in the target window **and** has the control in its control window with valid target evaluation. Positive control alone is not target success. If session failure prevents these checks, record the exact loading failure separately; silence is not an audio negative.

Known cache files may be recorded if independently identified. Snapshot relevant observed writes and retain deltas, including unrecognized files. Do not invent cache paths, build a cache census or delete caches to make a row pass. A/B distinction, fresh processes and both absence controls remain mandatory.

### Map

Identify actual world geometry from the expected camera at the fixed time. A/B must differ independently of session model entities, textures or thumbnails. Record the loaded session/context and a screenshot/video. Absence must show neither signature; an identified map-load failure can be a valid negative without a fictional BSP checksum API. Keep support and camera coordinates fixed.

### Other discovered candidates

An absolute override/light-texture candidate requires a narrow fixture and A/B/absent visual oracle for that slot; MDL checksum cannot identify its texture. Unknown schemas are measured before fixture-specific tooling. No E2 corpus work is imported into E1.

A narrow trace is conditional when a direct observable cannot distinguish remaining alternatives. Pin the method and prove what its events mean using positive and absent controls. An attempted file open is not loaded/rendered identity. Broader console capture and `-condebug` qualification remain E7.

## 8. Final mandatory matrix and why it is minimal

Prepare ordinary-location fixtures, serialized evidence, A/B identities, root arrangements and frozen oracle rules before admitting runs. Receiver rows use the exact D0 DMX hash at a different absolute session location, fixed R configuration, unchanged support and verified donor absence appropriate to the slot.

For each of M, S and B, run this sequence in fresh processes:

| Row | Original donor target | Target at R's same virtual path | Required observation | Why the row remains |
|---|---|---|---|---|
| D0 | A available in ordinary donor location | Receiver unused | A on reopening the sealed save | Establishes that the actual saved fixture and authoring route work, not merely unsaved editor state |
| R0 | Unavailable | None | Neither A nor B; valid observation/support state | Detects leftover donor, unintended provider or preexisting cache before receiver installation |
| R1 | Unavailable | A | Intended A | Direct unchanged-session recipient-portability test |
| R2 | Unavailable | B | B rather than cached A | Qualifies the discriminator and proves responsiveness to changed bytes at the same receiver location |
| R3 | Unavailable | None | Neither A nor B after warm successful loads | Detects persistent cache or hidden-copy success that R0 alone cannot exclude |
| R4 | Unavailable | A restored | A again | Brackets the post-warm negative with working content, checks repeatability and rejects an environmental failure masquerading as absence |

**Final unconditional mandatory count: 18 fresh-process recorded runs, six per slot.** This count excludes native authoring, fixture compilation, static inspection, optional redundant repeats and conditional diagnostics. Conditional rows become mandatory when their trigger occurs; 18 is not a cap on the work needed to qualify a problematic slot.

Ordinary UI authoring may require additional process starts; record the actual total rather than representing 18 as the total operator launches. The donor baseline must be a fresh reopen after sealing, so it cannot be folded into unsaved authoring.

The absent -> A -> B -> absent -> A receiver sequence retains every independent cache/identity control. A/B rows double as direct-oracle qualification where successful. The sound positive control adds an action in each existing sound process, not another row. Receiver native reachability is conditional on failure, as specified next.

### Why the old second-root rows are removed

The old R3 proved RB only to support the old R4/R5 dual-provider/swap experiment. After removing that general precedence test, reinterpreting old R3 as mandatory session-versus-provider decoupling would add a different research question. E1 already has a distinct donor and receiver; the freeze does not require a second receiver provider or a claim that the session may live independently of all asset roots.

No pure E1 clause requires a universal lookup-order model. Removing RB and provider swapping narrows the claim, not the observation controls. General ordered roots, path IDs, VPK/map-pak overrides and safe installation arrangements remain E4.

The original request to identify the winner **when multiple plausible copies exist** remains enforced by section 9. The baseline deliberately has exactly one candidate provider (or none); it does not report the multi-copy case as tested. An ambiguous absolute mapping or an actual competing provider cannot pass by pointing to this smaller matrix.

## 9. Conditional controls and causal classification

### A. Independent receiver reachability, before calling failure nonportable

Trigger: any receiver positive row fails, returns the wrong candidate or cannot qualify its intended observer despite a valid donor baseline.

In a separate fresh process, keep donor availability, R bytes, receiver profile, map/load context and configuration equal to the failing row. Without opening the sealed session, use a normal native action to request the **same logical runtime resource** through the same relevant loader context:

- model: native model selection/new animation set in an unsaved scratch session;
- sound: native direct-file timeline playback in an unsaved scratch session; preview alone is only corroboration if its lookup path differs;
- map: native map selection/load using the same intended map identifier.

Observe the same A/B signature. Never browse directly to an absolute disk file in a way that bypasses the intended resolver and call that a valid virtual-path control. Retain the exact request/route, active context and configuration. Do not save the scratch session into the fixture or update the frozen DMX.

A matching native success establishes placement for that resource/context. Native failure means **environment/fixture not established**, not nonportable. If the native route uses a materially different loader/path-ID context or this cannot be established, the result is insufficient and remains unresolved. Test only the failing candidate/root/context; do not add a broad root census.

Each distinct failed placement needing diagnosis adds at least one fresh-process control. Success does not alone prove donor dependence: the unchanged-DMX and causal controls below must still discriminate it.

### B. Absolute-target restoration

Trigger: an ordinary sealed session fails receiver positives under valid environment/support, a qualified observer and successful independent receiver reachability.

Keep the receiver DMX/configuration/candidates fixed. In three fresh-process conditions:

1. Confirm failure with relevant donor location/anchor absent.
2. Restore only the experiment-owned candidate's necessary asset unit at the original absolute location, leaving it unmounted and without altering receiver lookup configuration. Observe whether the intended result returns.
3. Remove it again and verify the failure returns.

A preceding failed matrix row may supply step 1 only if its recorded conditions are identical. Record all restored files and anchor changes. If restoring a parent directory also supplies a possible mapping anchor, the result demonstrates a **donor-location dependency**, not necessarily a direct open of the original target. Use a narrowly qualified access discriminator if the stronger direct-file claim matters.

Restoration must not resurrect unrelated stock content or remount D. If it cannot be done safely in an isolated receiver environment, hold the causal claim. A donor success plus receiver failure alone is insufficient.

An ordinary no-rewrite contradiction requires these controlled results with relevant support functioning. It is a claim about the tested context, not a proof that every imaginable configuration would fail. Submit that evidence for architecture review rather than inventing a rewrite or silently excluding the ordinary workflow.

### C. Multiple plausible candidates or uncertain alias mapping

Trigger: preflight finds multiple possible suppliers, or a saved value admits multiple plausible qualified mappings that cannot be distinguished by the baseline.

First use the controlled namespace/single-provider arrangement to qualify portability and the oracles, without claiming production conflict resolution. If competition is part of the slot claim being evaluated or cannot be removed without changing the meaningful context:

1. Register each plausible supplier/path and give distinguishable complete A/B bytes; do not erase an inconvenient legitimate provider from the record.
2. Demonstrate each candidate alone works in its relevant location/context in fresh processes.
3. Restore the competing arrangement and observe the actual winner with unchanged DMX in a fresh process.

For two previously unqualified positions, this is up to three required rows (A alone, B alone, both), reusing a baseline row only when all relevant conditions match. Do not add a reverse-root swap or other permutations by default. More than two candidates needs a discriminating fixture/method or an evidence hold, not an assumed first-match rule.

The result identifies the winner for that occurrence/environment only. An unexpected but identified B is a wrong winner for intended A, not success; investigate or narrow the installation condition. An unidentifiable or inconsistent winner remains unresolved for that context. General explanation of all priority orders belongs to E4. E1 cannot claim that unknown precedence has been qualified.

### D. Provenance role and extra forms

A successful receiver run is not enough to distinguish an ignored absolute breadcrumb from a remapped runtime request. Use an independently native-authored calibration pair holding the actual runtime reference constant while changing the suspected authoring field, or a qualified narrow trace plus structural evidence. Keep calibration files distinct from the definitive unchanged session. If there is no safe discriminator, **fixture portable; candidate role unresolved** is permitted only when anchor-independent receiver evidence satisfies section 5 for every associated candidate not shown non-runtime and the applicable winner/negative controls pass. If the evidence comes only from a same-installation receiver with a relevant donor anchor still available, record **observed same-installation load; portability on evidence hold; candidate role unresolved**. Neither result proves the candidate ignored or establishes a mapping rule.

A newly encountered absolute/topology-sensitive slot or genuinely different native serialized form receives its own baseline/conditional controls as needed. No mandatory pair of custom-root and ordinary-root authoring saves is imposed. Do not reinterpret injected synthetic paths as ordinary-workflow evidence.

## 10. Acceptance and per-occurrence classifications

Bind every result to the session hash, structural occurrence/class/attribute/type, authoring route, SFM build, support context, physical configuration and observer version. Do not promote one result to an extension-wide rule.

| Classification | Required evidence | Permitted conclusion |
|---|---|---|
| Qualified ordinary relative runtime reference | Typed original value, D0/R1/R4 A, R2 B, valid R0/R3 absence and support, original targets absent, section 5 occurrence-wide eligibility satisfied | Unchanged-session portability in that relative form/profile; no relative-only resolution inference while an associated candidate's role remains unresolved |
| Provenance-only absolute/topology-sensitive value | Structural role and independent nonloading/authoring controls; functioning explained runtime path; relevant candidate absence | Ignore for dependency lookup only within the qualified semantic rule |
| Qualified portable absolute mapping | Exact original value, all relevant donor anchors unavailable/unmapped, discriminated receiver winner and negatives, isolated actual mapping | Only that observed mapping/context; no universal prefix stripping |
| Fixture portable; associated candidate role unresolved | Section 5 anchor-independent receiver conditions for every associated candidate not shown non-runtime, with qualified intended winner and applicable positive/negative controls | Narrow fixture portability only; no provenance-only classification, ignored-dependency rule or inferred mapping for the unresolved candidate |
| Genuinely donor-bound runtime requirement | Valid independent receiver placement and support, failed unchanged-session positives, causal restoration/removal | Unsupported under no-rewrite in that context; ordinary case triggers architecture review |
| Unresolved | Uncertain fields, anchors, support, mapping or observer; contradictory controls | Retain observations and exact gap; no omission rule or portability pass |

Record role and portability separately where needed. The narrow fixture-portable row resolves the portability observation only; the candidate's semantic classification remains open. A same-installation success with an associated unresolved absolute/topology-sensitive value does not qualify for that row and remains an evidence hold under section 5. Root-relative/dot-segment/mod-qualified forms retain their exact categories; they are not quietly folded into “ordinary relative.”

Initial M/S/B qualification requires all 18 baseline rows valid, successful intended A positives, qualified A/B/absence discrimination, complete serialized/support records and every triggered conditional control resolved for the claimed scope. A successfully demonstrated negative is not product failure. A crash, silent audio chain or generic model appearance is not a negative/positive pass.

If no absolute form appears, report **not observed in these fixtures**, not impossible, provenance-only or mapped. Material/light and wider corpus scope remain explicitly not tested. New E2 absolute candidates reopen only their scope. No Scene parser or subsequent stage is authorized by this document.

## 11. Evidence files and privacy

Use private write-once attempt directories outside all mounted content and repositories. Freeze the protocol/hash, row registry, A/B references, expected outcomes, audio classification rules and support contracts before execution. Failed attempts remain preserved.

| Record | Required content |
|---|---|
| `authority.json` | Repository checkpoint; authority, Stage 0A, audit and protocol hashes; reused claim IDs; execution authorization |
| `profile.json` | Build/module/OS/runtime identities; actual launch, active mod, configuration hashes, context |
| `fixtures.tsv` | Asset/support IDs, variants, roles, logical/private physical locations, size/hash, rights and compilation receipts |
| `sessions.tsv` | Master/copy identity, native authoring route/location, actual encoding/schema, checkpoints and supports |
| `serialized-slots.jsonl` | Raw element/attribute/type/value, links, byte spans/indices, independent association check, candidate category |
| `anchor-checks.json` | Donor/receiver semantic anchors, component overlaps, aliases, mount membership, target/anchor absence and limits |
| `run-register.tsv` | Slot, row, attempt, expected comparison, validity/outcome, conditional trigger and retry links |
| `runs/<slot>/<row>/<attempt>/preflight.json` | Input/configuration/provider hashes, absence/anchor checks, prior process exit, known cache state |
| `.../live.jsonl` | PID/start time/marker, raw document identity, paired serialized/live occurrence values, checksum results/errors, target playback state |
| `.../physical.*` | Original capture and operator record; sound target and positive-control windows; actual observed signature |
| `.../postflight.json` | Exit, original-byte recheck, donor checks, support/configuration hashes, observed filesystem/cache deltas |
| `classifications.tsv` | Per-occurrence role/form/portability, winner identity/provider or unknown, mapping scope, supporting/contrary runs |
| `E1_RESULT.md` | Actual row totals, per-clause scope, failures/holds, E2 reopen list, architecture-stop assessment |
| `MANIFEST_SHA256.txt` | Paths/sizes/hashes of sealed evidence; self excluded; its hash in a separate receipt |

No winner ID may be inferred solely from disk hashes. Link observed signatures to candidate byte identities through calibrated controls.

Keep raw paths, identities and private content in private evidence. A later sanitized report uses D/R/Q labels, has its own hash and cites the private manifest hash for identity linkage; it does not publish private locations or imply raw bytes are public. Apply standing privacy/rights review before any later publication. Nothing is committed or published in this task.

Where practical, give a second capture classifier the sealed A/B reference signatures but hide the row placement. This is optional bias reduction, not a new mandatory blinded-study infrastructure or extra SFM run. Retain disagreements.

## 12. Failure, retry and architecture-stop rules

- Use PID/start time plus marker for every row; no same-process reopen substitute. Record disk caches separately. Do not flush/delete caches to get a pass.
- R0 checks pre-install absence; R2 challenges cached A; R3 checks post-warm absence; R4 restores a working condition. If an absence row retains A/B, stop admission for that slot and locate the extra provider/cache/substitution. Preserve all failed evidence.
- Sound negatives require the same-process positive control and verified target evaluation. Failure of either is invalid observation, not asset absence.
- Changed session/asset/configuration/support hashes invalidate dependent comparisons. Seal a new revision and rerun the affected rows; never just update expected hashes.
- A missing helper output, crash or timeout is an invalid/failed attempt. No arbitrary time limit proves nonportability.
- A package/observer failure calls for a narrow alternative or evidence hold. Changing observers requires appropriate requalification; unresolved contradictions cannot be dropped.
- One exact-condition confirmation is allowed after preflight for an unexplained result. Continued ambiguity ends as unresolved; do not repeatedly vary hidden conditions until something passes.
- Restore backed-up configuration and experiment-owned placements after completion/stop; verify originals. Do not remove ordinary roots or alter unrelated files.

Emit **STOP FOR ARCHITECTURE REVIEW** when qualified evidence shows an ordinary authoring workflow requires a donor-specific runtime location despite a valid independent receiver placement, or that a necessary resolution can only be achieved by DMX/live-reference editing, a recreated donor alias, incompatible destination identity or a state requirement outside the frozen contract.

A failure repaired by correcting an invalid mount is not such evidence. A conditional control needing an unavailable isolated installation is an evidence hold. Do not silently call an inconvenient ordinary case exceptional, and do not claim universal nonportability from one unqualified receiver failure.

This design review provides no qualified contrary runtime evidence. It therefore does not modify or stop the frozen architecture.

## 13. Bounded later implementation and physical steps

After design approval and separate execution authorization, 6.1 Sol needs only:

1. External manifest/preflight/sealing utilities, component-aware anchor checks and bounded provider inventories. Changes to experiment files require checked absolute targets and recoverable copies. No arbitrary install search/cleanup.
2. One qualified read-only serialized observation route with independent original-byte association, not a shipping Scene parser or E6 adoption.
3. A small MAINMENU helper using accepted logging, marker, session and optional checksum routes. It records only fixture state, including explicit serialized/live pair IDs.
4. Fixture sources/compile receipts and physical capture setup, including the qualified same-process audio control. No application rebuild or recovered-packager execution.
5. An evidence checker for row coverage, triggered conditional controls, input invariance, anchors and observation completeness. It cannot infer runtime success from file presence or empty logs.

Operator actions:

1. Identify the real installation/active mod and ordinary donor locations; approve reversible experiment file/configuration preparation.
2. Author/save M/S/B using native ordinary routes; close and seal. Confirm base-map/support occurrences are inspected.
3. Run D0 by fresh reopen. Prepare R after classifying the saved forms and meeting the applicable donor-target/anchor conditions.
4. For each receiver row, review preflight, start a fresh identified process, open the exact full DMX path, evaluate its shot/time and capture the defined signature.
5. In every S row, record target playback state and then the unsaved non-target positive control in that process/capture.
6. Close without saving; wait for exit; seal captures and postflight before changing candidates.
7. On a failure or ambiguity execute only the triggered, recorded conditional branch; preserve all invalid results.
8. Restore the original environment and review actual scope, totals, holds and stop assessment.

No E2 corpus, E4 precedence engine, E6 production package qualification, E7 console/caches project, product dependency graph or application integration is authorized.

## 14. What E1 will and will not establish

Successful rows qualify unchanged-session portability and observed winner identity for the three measured ordinary M/S/B forms and profile, with support and donor/cache controls. They may qualify the checksum observation and specific absolute mappings; those results are separately labelled.

They do not establish all real-session schemas, absence of absolute paths everywhere, complete transitive dependencies, arbitrary multi-root priority, general session relocation, future parser safety or identical rendering on every recipient. Multi-copy cases are qualified only if the corresponding conditional branch ran. E2 discoveries still require their own E1 follow-up.

## 15. Audit disposition

“ACCEPT WITH MODIFICATION” identifies both the sound concern and the narrower or more rigorous replacement. No audit proposal becomes authority merely because it reduces run count.

| Finding | Disposition | Reason | Design change |
|---|---|---|---|
| **F1: ordinary versus off-install donor authoring** | ACCEPT WITH MODIFICATION | Custom-only authoring cannot support the ordinary-workflow claim. A paired custom save is unnecessary, and equal strings alone would not prove context equivalence. | Replace the custom donor with actual ordinary active-mod authoring for all definitive fixtures; record current location/route. No mandatory paired authoring. |
| **F2: donor-file absence versus donor-prefix dependence** | ACCEPT WITH MODIFICATION | A present donor mapping anchor can produce false portability. However “any prefix” would reject shared drives/generic parents and is not a semantic rule. | Check normalized component-aware install/mod/provider/session anchors and aliases; require relevant anchor independence for absolute mapping. Different-machine/same-install-path is insufficient. |
| **F3: R4/R5 precedence work and retained R3** | ACCEPT WITH MODIFICATION | Full provider swapping belongs to E4. The proposed retained second-receiver-root row loses its prerequisite purpose once that work is removed. Existing user requirement for actual competing-copy winners still applies when that condition exists. | Remove unconditional RB and all second-provider rows; derive 18-run baseline. Add bounded conditional candidate discrimination without general precedence explanation. Do not adopt Claude's 21-run matrix. |
| **F4: independent receiver placement** | ACCEPT WITH MODIFICATION | Receiver failure cannot prove nonportability if only the sealed DMX ever tests the placement. A browser/preview that bypasses the actual lookup would also be insufficient. | Conditional fresh-process native same-resource/same-context control, with ordinary resolver route; require it plus causal controls before nonportable classification. |
| **F5: same-process audio positive control** | ACCEPT WITH MODIFICATION | Another process's playback cannot validate a silent current capture. A preview positive alone also does not prove the target timeline was unmuted/evaluated. | Same-process engine control in every sound capture plus target-track/time/gain verification; no extra baseline row. |
| **F6: nested BSP assumption** | ACCEPT | Nested map handling is unnecessary for E1 and currently unqualified. | Flat nonce-bearing BSP and matching sidecar names. |
| **F7: KeyValues2 dependency and evidence use** | ACCEPT WITH MODIFICATION | A particular package must not gate E1. The assertion that no generic deserializer reports offsets is unnecessarily categorical. Blanket exclusion of E1 facts from E6 conflicts with Errata's claim-level reuse contract. | Package-agnostic read-only methods, independently checked byte associations; candidate failure triggers alternate method, not E6 prerequisite. Preserve results as narrowly reusable evidence without claiming adoption. |
| **N1: paired serialized/live values** | ACCEPT WITH MODIFICATION | Pairing helps identify transformations; it does not prove winner identity or absence of internal mapping. | Explicit per-occurrence pairs at D0 and all receiver rows, with separate winner observations. |
| **N2: topology-sensitive relative forms** | ACCEPT WITH MODIFICATION | Such forms can preserve machine/topology dependence, but must not all be labelled absolute or assumed nonportable. | Separate measured categories, anchor checks and per-slot qualification without broad path-normalization work. |
| **N3a: matched rig/interface** | ACCEPT WITH MODIFICATION | Stored rig differences could confound an asset winner. Requiring rich rig features creates unnecessary fixture work. | Identical trivial single-bone contract; omit optional flex/animation/attachments rather than adding them. |
| **N3b: independent VVD/VTX checksum checks** | ACCEPT WITH MODIFICATION | Useful fixture consistency when layouts/tools are already qualified; not necessary to implement new dependency inspection to identify a geometric winner. | Use existing qualified checks when available, preserve matching compile outputs/native positives, and hold any detected mismatch. General companion-reader work stays outside E1. |
| **N3c: actual MDL version instead of v49 gate** | ACCEPT WITH MODIFICATION | E1 viability is not version-49-specific; current inspected checksum layout is. | Record actual version; prefer easy v49 fixtures, allow another valid fixture with qualified geometry and no unqualified checksum read. |
| **N4: visual oracle sufficiency** | ACCEPT WITH MODIFICATION | C3 permits distinctive direct observation. Checksum absence need not block that route; contradiction must be resolved, not ignored. | Geometry can qualify E1 independently; checksum has its own status; conflicting observations hold the row. |
| **N5a: different first audio frame** | ACCEPT WITH MODIFICATION | Early distinguishability helps; exact first-sample inequality is not a meaningful universal capture requirement. | Early resolvable onset with predefined windows; no long shared silent prefix. |
| **N5b: expected sound-cache filenames** | DEFER TO LATER GATE | No cache layout is established here. A mandatory filename inventory adds unrelated engine research and risks invented facts. | Retain actual write deltas/known files and strong cache negatives. Investigate only if a control fails; broad runtime-cache work is not E1. |
| **N5c: pulse/inconclusive rule** | ACCEPT | Predeclared classification prevents post-hoc success thresholds. | Freeze count/band/window/tolerance and inconclusive rules before runs, validated against reference captures. |
| **N6: base-map support slot** | ACCEPT | An absolute or broken support map can confound M/S outcomes. | Explicit serialized/live support occurrence and applicable anchor qualification; support failure holds target inference. |
| **N7a: manifest digest in sanitized report** | ACCEPT | Gives stable evidence linkage without publishing private raw content. | Cite the private manifest hash in a separately hashed privacy-reviewed report. |
| **N7b: blinded capture classification** | ACCEPT WITH MODIFICATION | Reduces observer expectation where practical, without requiring a new study framework. | Optional second classifier sees A/B references but not placement; preserve disagreements; no mandatory runs. |
| **Final review: unresolved associated value can invalidate same-installation portability** | ACCEPT | A relative field and missing donor asset do not exclude resolution through an associated value's still-present donor anchor. Errata section 3.4 requires evidence that discriminates that alternative. | Section 5 adds occurrence-wide eligibility; section 9D restricts the narrow fixture-portable result to qualified anchor-independent evidence when roles remain unresolved; section 10 makes both limits explicit. Baseline count and architecture-stop rules are unchanged. |

The final mandatory baseline is **18 recorded fresh-process runs**, with each row's independent purpose in section 8 and conditional additions explicitly defined in section 9. The reduction removes a separate precedence question; it does not remove donor absence, oracle discrimination, warm-cache negatives, receiver restoration or the current-process audio control.

E1 DESIGN READY TO FREEZE
