# Scene Package Blueprint Revision 2
# Freeze Errata and Authority Record

**Date:** 2026-10-07

## 1. Freeze Status and Precedence

**FROZEN WITH BOUNDED ERRATA.** No architectural blocker or need for Revision 3 was found. Revision 2 together with this record is authoritative for the product architecture, implementation blueprint and Stage 0 research plan. The exact clarifications below supersede conflicting Revision 2 wording; all unaffected Revision 2 provisions remain in force.

This record resolves the design corrections now. Its stage deadlines identify where their implementation or empirical verification must occur, not deferred decisions about what the corrections mean. C1-C3 apply before any Stage 0 activity or evidence harvest. The freeze does **not** authorize beginning those activities or product implementation in this task.

### Identified Documents

| Document | Role | SHA-256 of inspected bytes |
|---|---|---|
| `SCENE_PACKAGE_INTEGRATION_BLUEPRINT_REVISION_2.md` | Frozen base blueprint; unchanged by this task | `64DD9981B0C4A8B59D3B11DEB0C708FF7B59B8E933D28517B96214E50976015C` |
| `Claude Audit.txt` | Original F1-F29 adversarial audit supplied to this project | `3BA8EC0EA078EB77C3E8011E46FB5F5B295F38A2AF18A94DC5ABF5AC2B84758C` |
| `Output.txt` | Closure audit containing C1-C14; verdict PASS WITH NON-BLOCKING CORRECTIONS | `976914D1951DA1EBE8122641C0BFC6A2CE62BEC6AFB4760B2FE2C0A8CC00DB67` |

The closure verdict is Claude's review conclusion, not a new SFM runtime qualification result. This record adds only focused source inspection and document reconciliation. No experiments, cross-repository search, package adoption, tests, build, application changes or release operations were performed.

Use Revision 2's evidence labels: **[PB]** current Package Builder source; **[OLD]** recovered source/original archive documentation; **[SRC]** upstream evidence; **[REC]** recommendation/design contract; **[GATE]** evidence still required; **[HYP]** hypothesis. The rules below are **[REC]** unless explicitly identified otherwise. Add **[USER-PROVIDED PROVENANCE]** only to distinguish attribution supplied in this conversation from archive-derived evidence.

## 2. Authoritative Errata Table

| Finding | Disposition | Exact authoritative clarification | Must be resolved before |
|---|---|---|---|
| **C1 - Existing evidence** | ACCEPT | Existing evidence may fully satisfy or partially narrow E1-E7 under section 3 of this record. Do not repeat an experiment solely because it predates Scene Package. Map each reused claim to its exact gate clause and preserve actual provenance, limitations and historical status. Missing fields reduce admissible scope; they do not automatically invalidate the whole item. | **Before evidence harvest or Stage 0.** Apply to every harvested item and gate closure. |
| **C2 - E6 / Stage 1 circularity** | ACCEPT | Stage 0/E6 uses a throwaway representative Core <- Scene <- WinForms/test project-reference graph outside the production tree. It tests the adopted package artifacts, SDK, target frameworks, generator assets, warnings-as-errors and publish/runtime propagation. It need not contain application functionality. Stage 1 recreates and verifies these behaviors in the actual solution; the representative result does not certify that future real graph. | **Before E6 tooling begins.** Actual-graph verification is required before Stage 1 completes. |
| **C3 - E1 observation** | ACCEPT | E1 must define an observable success/failure and winner oracle before running or admitting evidence. Use distinctive runtime/visual/audio variants and donor-unavailable negative controls where sufficient. If logs or engine traces are necessary, qualify that narrow capture method inside E1. E1 does not depend on E7 or assume `-condebug` works. E7 later qualifies broader logging/automation. | **Before E1 evidence is accepted or a new E1 experiment is designed.** |
| **C4 - Usefulness timing** | ACCEPT | Stage 3's usefulness result covers only its implemented model/material/session-extraction subgraph; absent map/audio functionality is reported as not yet evaluated, not a false block or a full-profile pass. Stage 6 must remeasure the complete first-public supported profile against held-out sessions before Stage 7 persistence/UI work. The owner fixes acceptance thresholds and corpus scope at Stage 0 exit as specified in section 4.1. Stage 10 repeats the full qualification after integration. | Thresholds: **Stage 0 exit**. Subgraph qualification: Stage 3 exit. Full-profile usefulness: **Stage 6 exit, before Stage 7**. |
| **C5 - VMT severity** | ACCEPT | The decision table in section 4.2 is authoritative for unmatched unknown strings, probable VTF/VMT edges and `UnsupportedMaterialSyntax`. Unknown does not automatically mean a required missing texture. Probable matches do not prove arbitrary runtime closure. Extend E2 with a material parameter/syntax census and use E6 for parser-tolerance qualification. This closes F14's remaining design ambiguity. | Policy applies now; census scoped before **E2 work**; rules/fixtures qualified before **Stage 3 material analysis is accepted**. |
| **C6 - Failed in-process qualification** | ACCEPT | If E6 demonstrates unacceptable in-process allocation, failure containment or cancellation, or lacks necessary safety evidence, **Stage 1 stops pending architecture approval**. Do not implement the default host model and postpone the decision to release. No worker EXE/IPC is implicitly authorized by that failure. Shipping remains a backstop gate, not the first decision point. | **Before Stage 1 begins.** |
| **C7 - Stable provider identity** | ACCEPT | Define logical root IDs independently of absolute paths/content hashes. Stage 2 owns the identity/mount/provider contracts in section 4.3; Stage 5 uses them for in-memory group decisions. Physical remap preserves a logical ID only through explicit reassociation and re-evaluation. Identity is not proof that permission or baseline trust transfers. Stage 7 serializes the established IDs/contracts. | Identity contract implemented before **Stage 2 resolver/baseline work is accepted**, and before **Stage 5 decisions**. |
| **C8 - Additive settings only** | ACCEPT | Schema-1 evolution adds optional members only. Preserve names, casing policy, JSON types, nesting, accepted enum representations and semantics of every member known to v1.0.4, including `CreatorDefaults`, `RecentProjectEntry` and their existing children. Do not put new enum values in an old field the old reader cannot interpret. New typed data belongs in new optional members. Test old-reader/read-modify-save interoperability, not just ignoring one unknown field. | **Before Stage 7 settings changes**, with regression tests passing at its exit. |
| **C9 - Recent cards vs Open** | ACCEPT | Card projection is display-only. Share root version/kind discrimination with Open, never infer kind from filename or partial payload. Successful kind/version labels require the same pure JSON/schema/payload acceptance checks used by Open, without source probing or analysis; otherwise show unavailable/unverified metadata. File access/source resolution can still change between card display and Open, which must validate again. Section 4.4 defines the boundary. | **Before Stage 7 recent-card changes**. |
| **C10 - Schema discrimination** | ACCEPT | Presence of the exact `packageKind` field selects the typed-document branch. Only `schemaVersion: 5` plus `packageKind: scene` is accepted for the new Scene format. Absence selects only existing flat Model schemas 1-4 through their current codec. Never infer Model from absence alone at a future version. Scene versions must remain greater than 4, the maximum accepted by v1.0.4. Section 4.4 specifies conflicting/unknown handling. | **Before Stage 7 serializer implementation**; no early production schema work is added. |
| **C11 - Indexed ZIP behavior** | ACCEPT | Explicit rejection of case-equivalent/normalized duplicate ZIP names intentionally changes Model-visible failure behavior: the existing uncaught `InvalidOperationException` becomes a structured archive verification failure. Exact byte/membership checks remain. Stage 4B must include a Model regression test for this correction, separate from Stage 4A mechanical extraction. | **Before Stage 4B archive-index change is accepted**. |
| **C12 - README boundary** | ACCEPT | Stage 4B owns the Core README resolution generalization, under exact Model README parity. Retain the existing Model facade and add a mode-neutral resolution path as described in section 4.5. Core does not reference the Scene generator. Stage 5 supplies Scene-generated content through the established boundary. This is not deferred to an unowned Core change in Stage 5. | **Stage 4B**, before Stage 5 Scene README integration. |
| **C13 - Precision** | ACCEPT | In Revision 2 section 1.3, the diagram's `elements/sessions/<package-key>/` placement is **conditional on E4 relocation safety**; otherwise use the qualified existing relative placement and collision review. Stage 1 creates adapter interfaces and qualification scaffolding only, not production parser bodies; actual parsing/resolution requires E4 and the relevant gates before Stages 2/3. Every additional absolute-path-bearing runtime candidate found by E2 goes through E1's classification/observation method. | These clarifications apply **before Stage 0**; enforce placement at Stage 5 and adapter-only scope at Stage 1. |
| **C14 - Attribution / filenames** | ACCEPT IN PART | Correct attribution provenance and separate the two audit filenames. The original assignment explicitly attributed the published binaries to LordAardvark; the supplied archive does not independently establish that name. Label retained attribution as user-provided provenance, not an archive-derived fact. The unusual `v1_u8uf1Fz.0.4` source filename is the actual supplied filename, not a transcription error, and remains in the input identity ledger. Use a clean display label separately; do not invent or rename an input. Section 4.6 is authoritative. | **At freeze** for evidence labels/filename references; review exact public acknowledgement before **Stage 10 notices/About delivery**. |

## 3. Stage 0 Evidence-Admissibility Contract

### 3.1 Governing Rule

**Reuse valid evidence, not historical confidence. Stage 0 must not repeat an experiment solely because valid evidence predates Scene Package. Missing provenance narrows the claim that can be reused; it does not automatically make the evidence worthless.**

E1-E7 describe facts that must be established, not mandatory new experiments. A future evidence-harvest task begins by mapping available records to those facts. It may close a gate from existing qualified evidence, isolate only a missing condition for a new experiment, or retain a lead without overstating it. No such harvest is performed in this task.

### 3.2 Minimum Provenance for Full Reuse

For each claim, the evidence ledger must identify:

1. **Claim and gate clause:** the exact assertion being supported, including positive/negative outcomes and the scope of assets/versions/contexts. A whole project labelled qualified is not a claim-level mapping.
2. **Recoverable evidence location:** source repository/path/ref, archive/member, report, test case or runtime record; record its available stable identity and original observation date when known. Do not invent a commit for pre-Git work or backfill a missing date as if historical.
3. **Relevant subject identity:** the actual SFM build/profile and input bytes/digests for runtime or parsing claims; exact package/version/artifact/source mapping and SDK/reference graph for package claims. Capture the mount order/map context only where it affects the claim. An irrelevant field is marked not applicable, not treated as mandatory ceremony.
4. **Method and conditions:** what was actually executed or inspected, input preparation, enabled content/roots, donor availability and negative controls where required, and how the observed outcome answers the claim. Preserve whether evidence was static, automated or physical runtime.
5. **Observed result:** actual output/diagnostic, test assertions, measured bytes/winners, recording or sufficiently specific contemporaneous runtime record, including failure/skips and contrary cases. Planned acceptance steps are not results.
6. **Applicability and limitations:** which current conditions match the historical ones, what has changed or is unknown, and why any equivalence claim is justified.

Full reuse needs these facts at the granularity necessary to rule out plausible alternative explanations. It does not require every older record to use the new fixture-manifest fields. A missing tool hash can be harmless to a directly preserved output fact yet material to a claim about which parser produced it. Record that distinction instead of applying a universal rejection rule.

A hash calculated now identifies today's retained bytes. It proves the old experiment used those bytes only if a credible evidence chain links them to that experiment; do not backdate the new hash. Cross-linked logs, immutable archives and exact test artifacts can supply missing documentation. Unsupported inference cannot.

### 3.3 Classification for Evidence Harvest

| Outcome | Admission rule | Ledger / subsequent action |
|---|---|---|
| **Fully satisfies** | Identity, method, conditions and result establish the exact gate clause for the selected supported profile; material uncertainty is absent or bounded by demonstrated equivalence. | Cite the reused record, original scope/date and any newly established links. Close that clause as **satisfied by reused evidence**, not rerun. A complete gate closes only when all its required clauses are covered. |
| **Partially narrows** | Evidence establishes a subset, variant, boundary or independent fact, but one or more required conditions/results are undocumented or different. | State the proven subset and exact remaining gap. Design only the missing test. Example: parsing proves a typed absolute string exists; it does not prove recipient portability. |
| **Hypothesis / lead** | Narrative, code intent, a recollection, filenames, parser documentation or weakly identified observations suggest a fact without establishing it. | Preserve as a lead with provenance. It may guide fixture choice; it cannot mark a required runtime clause passed. |
| **Must be reproduced** | The exact required fact remains unresolved and is material: evidence is unrecoverable, contradictory, identifies different relevant bytes/profile without demonstrated equivalence, or lacks an indispensable control/observable. | Reproduce the smallest unresolved condition under the qualified method. Retain any useful old evidence; do not erase it or rerun unrelated proven work. |

Conflicts are recorded, not settled by selecting the more convenient report. A newer result does not automatically supersede an older result from a different supported profile. Classify each claim at its actual scope; narrow the support profile or perform a targeted discriminator when needed.

For **E6**, claims about a pinned package's APIs, generator flow, allocations or runtime propagation require the pinned artifact, not just current `master` or a nearby version. Existing tests against that exact artifact/reference topology can satisfy representative-graph clauses. Stage 1 must still verify the actual newly assembled solution graph. Dependency security-advisory evidence is time-sensitive: preserve its historical date; release-time review must be current as Revision 2 requires.

A reused SFM runtime result remains a historical runtime result; a prior automated build remains a prior automated build. Neither is reported as a new run. Final integrated software and release qualification still run against the actual implementation: evidence reuse eliminates duplicated research, not tests of new code.

### 3.4 E1 Observation Independence

E1's purpose is to establish whether the unchanged DMX can use the intended asset when donor absolute locations are unavailable. For each slot:

- Identify the serialized reference and exact session/asset bytes; record the receiver's relevant environment and absence of the donor location.
- Choose a distinctive fixture result or qualify the narrow logging/trace method needed to distinguish possible winners. Merely opening without crashing or seeing a generic model is insufficient.
- Include the relevant negative control and a fresh-process condition where engine caches could hide resolution. Logs are optional only when the direct observable actually discriminates the claim.
- Record the outcome as provenance-only, qualified portable mapping or genuinely nonportable runtime dependency, or leave it unresolved if the observable is ambiguous.

These requirements apply equally to reused evidence. E7 can later standardize broader console capture; it is not an E1 prerequisite. Any new absolute slot discovered by E2 reopens only that slot's unproven E1 clause, not every already-qualified slot.

## 4. Exact Design Clarifications

### 4.1 Full-Profile Usefulness

At Stage 0 exit the owner approves the corpus strata, holdout boundary, supported-profile scope and numerical acceptance criteria: allowed false-block rate, maximum unresolved ordinary-session cases, group-decision burden and any agreed analysis-time/memory envelope. Record denominators and the treatment of intentionally unsupported cases. Do not invent values in this freeze or allow implementers to change them after seeing held-out results.

Stage 3 reports model/material subgraph coverage and its false blocks separately from as-yet unimplemented maps/audio. Stage 6 applies the accepted thresholds to the complete supported profile on held-out sessions before persistence/UI work proceeds. Newly identified unsupported cases remain visible; do not relabel failures out of the denominator after testing. Materially changing scope or thresholds requires owner review of the evidence, not an automatic new architecture cycle. Stage 10 validates the integrated application again.

### 4.2 VMT Decision Table: Closure of F14

This table supersedes ambiguous generalizations in Revision 2 sections 7 and 8. A **qualified shader** means its relevant parameter/loading behavior is covered by evidence, not merely that its name appears in a registry. E2 inventories encountered unknown parameter names/value types, known shader/proxy contexts, candidate hits/misses and syntax anomalies; E6 establishes any supported syntax tolerance against SFM evidence.

| Case | Authoritative severity / behavior |
|---|---|
| Recognized required texture/material parameter, target missing | **Error**, blocking for required timeline/load-time content. No downgrade because another unknown parameter happens to resolve. Existing qualified editable-only omission rules still apply where independently proven. |
| Unmatched unknown string on a qualified shader, with no evidence of external loading by that parameter or an attached unqualified proxy | **Warning**, non-blocking: unrecognized parameter; dependency meaning not established. Record the parameter/rule context locally; do not manufacture a missing-file node. This is a qualified-profile compatibility policy, not proof from a failed lookup. |
| Same string in a context with evidence of resource loading, dynamic naming or otherwise unqualified active shader/proxy behavior | **Error**, blocking unsupported dependency semantics until qualified. Explicit evidence of required loading overrides the generic unknown-parameter warning. A missing candidate is not the only reason for the error. |
| Unknown string resolves through constrained lookup to an existing VTF or VMT | Create a **probable edge** with **Information** explaining conservative inclusion. Include/traverse it by default under normal rights, collision and budget rules. The edge itself is not a missing-file warning and does not certify other unknown behavior. A group permission decision remains mandatory where applicable. |
| Two plausible candidates of different kinds, with no qualified discriminator | Retain both as probable candidates if safe/finite and disclose conservative inclusion; never arbitrarily choose one and call it proven. Normal conflict/rights/size rules apply. If candidate closure cannot be established, use the next row. |
| Probable candidate has unresolved required descendants or cannot itself be analyzed | **Error** for the conservative closure unless qualification establishes that the initiating property is nonloading, in which case reclassify/drop that speculative edge with its evidence and retain the appropriate unknown-property warning. No user Ignore button converts unresolved probable closure into proven completeness. |
| Unfamiliar syntax supported by an evidence-backed bounded tolerance rule | Analyze using that rule, preserve all source bytes and emit a **Warning** describing the compatibility interpretation. Qualified dependency extraction must remain complete; never silently truncate. |
| `UnsupportedMaterialSyntax`, including syntax demonstrably accepted by SFM but not safely analyzable by the tool | **Error** for required material analysis. Say that Package Builder cannot establish dependencies, **not** that the material is invalid in SFM. Report its impact in false-block measurement and supply the fixture to qualification. Do not downgrade merely because SFM renders on the donor machine. |

Known non-resource properties remain nonruntime detail/Information, not repeated warnings. Ordinary safety errors, destination conflicts and required rights decisions retain their own status; a probable edge's Information severity cannot override them. Unknown parameters are not a license to scan arbitrary absolute locations. Full-profile usefulness gates expose whether these bounded policies still reject too many accepted community assets; do not quietly waive those gates.

### 4.3 Logical Root, Mount and Content Identity

Stage 2 establishes these separate identities in memory, using simple typed IDs/records rather than a general identity framework:

- **LogicalRootId:** an opaque stable ID assigned when a content root is accepted into a recipe/profile. It is not derived from an absolute path, display label or digest. A retained root keeps its ID across rescans; an explicitly accepted locate/remap rebinds its physical source reference. A freshly added unrelated root receives a new ID. Independently created recipes need not assign matching IDs to the same physical folder.
- **Mount identity:** a stable mount ID associated with logical root, path-ID semantics and provider selector (loose root or relative container selector). Search priority is a separate ordered property, not the ID. Repeated mount declarations can share a logical root while retaining distinct mount/context positions.
- **Content identity / baseline evidence:** current virtual member, selected provider/container, size/digests and matching trusted baseline. These are observations. They change when bytes or winners change; a stable logical ID never substitutes for revalidation.
- **Decision group identity:** root ID plus explicit permission/prerequisite scope. Stage 5 binds its in-memory decisions here, while exact plan consent uses content/profile revisions. Remapping re-evaluates membership, origin evidence and affected decisions. Permission for a replaced root is not automatically inherited because its ID was retained.

New roots discovered by wildcard expansion are matched by their declared profile/mount selector and re-evaluated against the physical binding; disappearing/reappearing ambiguous roots require review, not basename guessing. Automatic valid stored relative/absolute resolution keeps the authored ID and does not mark dirty, but does not waive winner/baseline checks.

Stage 7 persists the IDs, accepted profile selectors and established source references. It does not invent a new identity model or serialize current graph/missing state. Machine profile caches are not the only copy of authored root associations.

### 4.4 Document Dispatch and Card Projection

The exact root discriminator contract is:

| Root state | Result |
|---|---|
| No `packageKind`; supported legacy Model schema 1-4 | Route to the existing Model codec, including its established schema alias handling, migration and strict payload validation. Save remains flat schema 4. |
| Exact `packageKind: scene`; canonical integer `schemaVersion: 5` | Route to the Scene codec and validate its complete known payload. No nested Model schema or inferred Model state. |
| `packageKind` present with null/unknown/model value, or Scene kind with schema <=4 | Structured incompatible/unsupported document failure. Do not ignore the discriminator or fall back to Model. |
| No `packageKind` at schema 5 or later; unknown typed schema/kind | Structured missing-discriminator/unsupported-schema failure as applicable; no implicit interpretation. |
| Conflicting version aliases or discriminator spellings, or ambiguous repeated discriminator/version keys | Structured invalid-header failure. Preserve legitimate unambiguous legacy alias input; do not silently select the convenient header. |

Schema 5 is the sole accepted new Scene version in this design. Future format evolution needs explicit migration rules; every Scene version must remain above v1.0.4's supported maximum of 4. The presence discriminator is necessary but not sufficient for successful load.

Recent cards obtain display fields without filesystem dependency resolution. Share header discrimination and the pure JSON/payload acceptance path with Open (the existing Model `Deserialize` rather than `Load` where appropriate, plus the Scene equivalent). Do not duplicate a permissive mini-validator. Files rejected by pure document validation show unavailable kind/version rather than appearing successfully loaded. Raw filename/last-known display name can still be shown as unverified fallback. Neither successful display nor source existence at refresh guarantees a later Open; Open rechecks current bytes, access and its normal source resolution.

This bounds C9 without allowing recent refresh to parse DMX/gameinfo, index VPKs or probe every derived dependency. Performance optimization may cache a validated metadata result keyed to the actual document snapshot, but must not weaken acceptance or turn missing/corrupt cards into modal recovery flows.

### 4.5 README Generalization and ZIP Failure Correction

**[PB]** `Core/Readme/ReadmeResolver.cs` currently owns a concrete `ReadmeGenerator` and exposes `Resolve(PackageProject, ReadmeContext)`. `ReadmeGenerator` is sealed. A scene-owned generator cannot simply be passed to that existing signature.

**Stage 4B contract:** retain the Model public facade and existing generator. Factor mode selection/imported/custom handling through a neutral path taking `ReadmeConfig` plus workflow-generated text when Generated mode requires it. The Model facade supplies text from its current generator; the Scene caller supplies its own generated text at Stage 5. Do not instantiate a fake `PackageProject` or reference Scene from Core. The exact API shape can be a small overload/value argument; no plugin hierarchy is required.

ReadmeResolver remains the sole mode/content resolver; the planner/finalizer materializes its result as the one allowed README package entry. Do not imply the present resolver itself constructs `PackagePlanEntry`. Generated-to-custom uses the exact same generated text and fingerprint route; custom text remains untouched and imported byte-copy behavior remains unchanged. Parity gates include CRLF/text bytes, optional sections/install roots, all modes/failure results, preview-review-build equivalence, provenance and imported staging bytes. No new UI copy is part of this extraction.

**[PB]** In `ArchiveVerifier.VerifyAsync`, `SingleOrDefault` can throw `InvalidOperationException` for duplicate matching ZIP entries, and the existing exception filter does not catch it. The Stage 4B normalized index must instead produce a structured failed verification before copying/comparing ambiguous entries. This is an intentional Model-visible robustness correction, not strictly behavior-identical extraction. Add a Model/archive regression with two case-equivalent names, assert no uncaught exception/no false success, and keep missing/unexpected/member-byte/cancellation assertions. Stage 4A remains mechanical and does not include this fix.

### 4.6 Attribution and Exact Filenames

The original user assignment identifies the recovered program as reconstructed from **LordAardvark's published 2016 binaries**. This is evidence available to this project as **[USER-PROVIDED PROVENANCE]**, not independent archive authorship verification. The recovered archive's methods document does contain broad DLL-use permission with a credit request **[OLD]**; it does not itself establish that personal name or a clear recovered-source license.

Accordingly, Revision 2 section 3.2 and Appendix A F4 must be read as: historical attribution to LordAardvark is supplied by the project owner; archive documentation independently supports the DLL permission wording only. Before public About/notices delivery, retain that provenance in the evidence ledger. If no independent attribution source is added, either explicitly label the acknowledgement as owner-supplied historical attribution or use the neutral tool title. Never cite the archive as proving the name, use template assembly metadata as authorship evidence, imply endorsement, or expand the DLL grant to reconstructed source. No recovered code/binaries are selected for redistribution.

Filename correction is a **distinction**, not a rename:

- Display label: **SFM Package Builder v1.0.4 source archive**. Exact received filename remains `SFM-Package-Builder-v1_u8uf1Fz.0.4-source.zip`, verified present; its Revision 2 hash identifies the source input. Do not replace it with a cleaner but unreceived filename in provenance records.
- `Claude Audit.txt` is the actual original F1-F29 input to this project, verified present. References in Revision 2 to that audit remain correct for that role.
- `Output.txt` is the actual new C1-C14 closure audit. This freeze record cites it for closure, not as a replacement filename for the historical F audit.

## 5. Remaining Empirical E1-E7 Questions

These are unresolved by **this documentation task**. A future authorized evidence harvest may satisfy them wholly or partly; no assertion is made that existing project evidence lacks the answers.

| Gate | Facts still requiring admissible evidence, not another design decision |
|---|---|
| **E1** | Which actual session slots contain absolute references; whether unchanged DMX uses them as provenance, portable engine-resolved mappings or donor-specific lookups; observables and donor-unavailable controls sufficient to prove each outcome. |
| **E2** | Supported session schemas and encountered class/attribute/reachability distributions across the real corpus; bins/disabled/load-time behavior; material parameter/syntax census; held-out subgraph and eventual full-profile false-block/decision-burden measurements. Newly discovered absolute slots require E1 classification. |
| **E3** | Embedded versus external particle requirements; concrete-wave versus event-registration playback; baked VCD/video provenance versus actual external loading, including distinctions between session and map loaders. |
| **E4** | Shipped SFM loose/VPK/map-pak/search-path/condition precedence; supported collaboration-folder placement; actual winning identities and effects on other scenes; safe session subdirectory/nested-reference relocation. |
| **E5** | Clean SFM VPK/provider baseline identities, real MDL/BSP variant census, loose override behavior, baseline verification cost and grouped stock-heavy usability. |
| **E6** | Exact pinned package/generator/reference-flow behavior in representative then actual graphs; runtime dependencies/notices/security state; accepted syntax tolerance; eager/deferred allocation, malformed-input behavior, cancellation and defensible budgets sufficient for in-process analysis. |
| **E7** | Runtime/extractor path boundaries at actual recipient roots; autosave conventions and freshness meaning; broader reliable console capture/log interpretation for later gates. E1 qualifies its own necessary observation method independently. |

The owner-approved Stage 0 usefulness thresholds are an exit acceptance action based on evidence, not an unresolved architecture. Later integrated software still requires its implementation tests and physical qualification; none is waived by freeze.

## 6. Authority Declaration and Stop Record

**Revision 2 together with the Freeze Errata is authoritative. No further architectural design cycle is required before Stage 0 evidence work.**

This authority covers the product architecture, implementation blueprint and Stage 0 research plan, including evidence admissibility. Apply this record first where it clarifies or overrides Revision 2. A future empirical result that contradicts a governing invariant, particularly E1 no-rewrite portability or E6 in-process viability, must stop the affected work for explicit review; freeze does not authorize silently redesigning around that result.

All C1-C14 design dispositions are resolved. No Revision 3 was created. Revision 2 was not rewritten. This task ends with this companion record: no application source changes, Stage 0 experiments, cross-repository evidence harvest or implementation have begun.
