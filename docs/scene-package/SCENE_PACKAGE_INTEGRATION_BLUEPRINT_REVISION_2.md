# SFM Package Builder: Model Package + Scene Package Integration Blueprint

## Revision 2 - Candidate for Review

2026-10-07. Reconciles Claude's adversarial findings F1-F29. This is a complete replacement design, not an addendum requiring Revision 1 to be followed. It is not implementation authorization. Revision 1 remains unchanged as review history.

No application code, project configuration, Git state or release artifact was changed for this revision. No application build, parser qualification, real-session corpus scan or SFM runtime test was performed. Existing code and selected upstream sources were inspected. Tests and runtime results below are required future gates, not claimed passes.

### Evidence Labels

- **[PB]** Verified in the supplied Package Builder source / matching local files.
- **[OLD]** Verified in the recovered DMX Packager source or supplied original documentation/IL; not proof of its runtime correctness.
- **[SRC]** Verified in identified upstream source/documentation. An SDK branch or .NET parser is not proof of the shipped SFM engine's behavior.
- **[REC]** Architectural recommendation. Unlabelled prescriptions in this document have this status.
- **[GATE]** Evidence required before the specified capability can be supported or the affected implementation stage can proceed.
- **[HYP]** Hypothesis, including an audit assertion not independently established. It is not an implemented dependency rule.

### Baseline and Review Scope

| Input | Identity / use |
|---|---|
| `SFM-Package-Builder-v1_u8uf1Fz.0.4-source.zip` | SHA-256 `F5E9B2BB1A1C2506A5CCBF569DDDE246B3BC7411057070C262986BDAFE71E884`. Revision 1 compared all 257 files: 255 byte matches with the local tree; only root `BUILD.md` and `README.txt` differed; no missing files. Application/test/build project/assets matched. |
| `DMX_Packager_v1.1.0_Recovered_Source.zip` | SHA-256 `978178DF6800E6D6688994CC23C00F4655CBC996F694F4CA476816126D3B4A86`. Recovered C#, original methods document and selected IL inspected. Bundled binaries were not run. |
| `Claude Audit.txt` | Findings F1-F29 are review input, not independent runtime evidence. Every finding has a disposition in Appendix A. |

Code citations are relative to the supplied trees, not private workstation locations. `Core/` means `src/SfmPackageBuilder.Core/`; `UI/` means `src/SfmPackageBuilder.WinForms/`; `OLD/` means the recovered archive root. Baseline identification does not assert that the local pre-Git workspace is the published Git checkout.

### Principal Changes Since Revision 1

Keep Model recipes flat schema 4 and shared settings schema 1. Remove the stored recent-kind hint, recent filter, Scene clone command and initial Scene changelog subsystem. Introduce a separate Scene assembly rather than putting parser packages into Core. Remove the predetermined analysis executable/IPC/Job Object design. Move Scene serialization after runtime evidence; separate mechanical build extraction from scene planning. Make absolute-path portability and corpus coverage the first evidence gates. Replace the full stock file catalog with provider-level baseline evidence. Add recipient-side verification and collision checks before public release. Stop treating speculative particle, sound-event, VCD, path-limit and autosave behaviors as established facts.

## 1. Product Definition and Recipient Contract

### 1.1 Two Distinct Jobs

**Model Package:** the existing v1.0.4 workflow for distributing compiled model families, selected materials and Extras, with release names, README modes, release/build history, saved recipes, preview and ZIP output. Its qualified discovery, rename, validation and persistence behavior remains authoritative.

**Scene Package:** share one saved Source 1 SFM session `.dmx` with its supported runtime asset dependencies, selected supporting content, and explicit recipient prerequisites. The DMX is the dependency root, not the entire job. Use the terms Model Package and Scene Package throughout the user experience.

The promise is scoped: reproduce the saved timeline/shot content covered by a qualified SFM profile, with all required supported external assets included, embedded in an included container, or verified as prerequisites. Editable non-timeline content may have separately disclosed omissions under section 4. A verified ZIP is not a guarantee of identical rendering, external script behavior or compatibility with every SFM build.

Only the saved DMX bytes are packaged. Never silently save SFM, switch to an autosave, rewrite the DMX, repair runtime asset names or modify source files. Show which saved file was analyzed; warn when it changes and require a recheck.

### 1.2 Initial Public Scope

| Area | Decision |
|---|---|
| Root input | One qualified Source 1 `sfm_session` DMX. Reject other DMX uses and Source 2 sessions deliberately. |
| Required chain | Qualified session references to models, model families/materials/textures, direct sound files, maps and their relevant dependencies. VPK lookup is part of content resolution. |
| Particles | Determine embedded definition versus external PCF semantics in Gate E3. Follow proven material/model references in supported embedded structures; do not require PCF merely because an effect was authored from one. Full external PCF parsing is not preauthorized. |
| Named sounds | Determine the actual playback source in Gate E3. If a concrete stored wave suffices, use it and classify the event name accordingly. Do not prebuild a global registration system. |
| VCD/video | No automatic dependency by extension alone. Gate E3 identifies actual runtime slots, if any. Genuine unsupported active references block; provenance strings do not. |
| Models/maps as top-level inputs | Direct MDL is already Model Package. Do not add direct BSP or arbitrary archive workflows. BSP is transitive. |
| Asset transformation | No model release renaming in Scene mode; no DMX/MDL/VMT/BSP/script rewrite or compilation. |
| Environment | Offline runtime; no Steam authentication, download service, Scanner or Python dependency. Evidence tooling is separate from the product. |

The first public release may reject some complex scenes with concrete unsupported-dependency explanations. Stage 0 must measure that restriction against ordinary real sessions before it is accepted as a useful product, not after building the UI.

### 1.3 Package Layout

Preserve required SFM-relative asset paths. Do not preserve a donor mod-folder name as an extra wrapper: virtual lookup identity is what matters. Provider identity remains in the portable report, and different bytes required at one loose destination are a blocking conflict.

```text
README.txt                                      optional, existing mode semantics
SCENE-PACKAGE.txt                               mandatory scene/install/limitations report
scene-package.json                             mandatory machine-readable verification manifest
elements/sessions/<package-key>/<original>.dmx  unchanged session bytes
models/...                                     original virtual paths
materials/...
sound/...
maps/...
scripts/...                                    only justified, qualified supporting files
```

`package-key` defaults to a valid title slug plus a short stable recipe ID. It is persisted once, not regenerated on each title edit. This prevents routine `test.dmx`/`session.dmx` collisions without relying on an arbitrary generic-name classifier. Moving the root DMX into that directory is allowed only for a profile whose session-relative references have been qualified. Otherwise retain the proven location and require explicit collision review. Do not relocate referenced nested sessions automatically.

Reserve report filenames before finalization. Extras or dependencies colliding with them are errors; no overwrite/rename behind the user's back. Imported README remains exclusively a README resolver input. The `.sfmpack` recipe is not included automatically because it can contain local source references.

### 1.4 Safe Recipient Workflow

1. Extract into an empty staging folder outside live SFM content. Read `SCENE-PACKAGE.txt`; install the named prerequisites lawfully.
2. In Package Builder, use the read-only **Verify Scene Package** utility with the extracted manifest, intended installation folder and recipient's actual content profile. Preflight checks package bytes, destination collisions and the expected winning content after installation.
3. Use the qualified installation procedure: either a collaboration folder mounted with proven precedence or an explicitly reviewed existing content root. Do not promise that an isolated folder overrides usermod until Gate E4 demonstrates it. Do not automatically modify gameinfo, merge manifests or copy files for the recipient.
4. Back up and explicitly resolve different-byte target collisions. Do not use the Model README instruction to allow all replacements. Even an intentional higher-priority override can affect other scenes: state this and provide the qualified mount-disable/reversal instructions.
5. Copy/mount manually, then run **Verify Scene Package** again in installed mode. Open the exact DMX identified in the report in a fresh SFM process and check its shots/audio.

Sender-side overlay simulation checks internal consistency against declared prerequisites only. It cannot inspect an unknown recipient's files. Recipient verification establishes file identity and qualified lookup results, not that the engine actually rendered correctly. No automatic installer, backup manager or conflict-overwrite command is in scope.

## 2. Current-State Architecture

### 2.1 Verified Reuse Map

| Current files / responsibility | Verified behavior and boundary |
|---|---|
| `Core/Domain/PackageProject.cs`, `ReleaseRecord.cs`, `BuildRecord.cs` | Model recipe; current version/changes, explicit ordered history, distinct build events. Keep unchanged. Scene does not need a dummy primary model or extracted changelog subsystem. |
| `Core/Domain/ModelEntry.cs`, `ModelReleaseName.cs`, `Expansion/ModelCompanionDetector.cs` | Primary/additional roles; exact family suffixes `.vvd`, `.dx90.vtx`, `.dx80.vtx`, `.sw.vtx`, `.phy`; persisted selection distinct from observed presence. Model release rename changes destinations, not source contents. |
| `Core/Mdl/MdlV49MetadataReader.cs`, `MdlV49Metadata.cs` | Only v49; material directories, texture/skin facts, checksum/name; bounded sections but path overload reads the whole file. Not include-model/ANI/VTX dependency closure. Reuse via bounded input; new Scene readers supply missing sections without changing model severity. |
| `Core/Discovery/MaterialSourceDiscoveryService.cs` | Model-root/configured-content-folder material folder proposals; not gameinfo or VMT resolution. Remains Model-specific. |
| `Core/Expansion/SourceExpansionService.cs`, `FolderExpander.cs`, `Planning/OverlapNormalizer.cs` | Recursive selections normalized before expansion. Reuse explicit Extras expansion, not folder-wide discovery for scene dependencies. |
| `Core/Paths/PathValidation.cs`, `DestinationPath.cs`, `PathAnchorDetector.cs` | Destination safety and source anchor helpers. Share destination validation; anchor inference is not engine content resolution. |
| `Core/Planning/PackagePlanner.cs`, `PackagePlan.cs`, `PackagePlanEntry.cs` | Ordinary destinations -> shared-file analysis -> README -> final plan with model facts. Read-only-facing arrays are not deeply immutable. Keep plan authoritative; introduce defensive immutable snapshots for new facts. |
| `Core/Validation/PackageValidator.cs`, `CollisionValidator.cs` | Mixed generic/model rules; same-source duplicate Information versus conflicting destination Error. Extract common checks under model parity; scene completeness is a separate rule set. |
| `Core/Build/PackageCheckService.cs`, `BuildCoordinator.cs`, `BuildRequest.cs` | Fresh planning/check; version/output decisions; storage, staging, archive, history, cleanup. Model recipe hardwiring; `BuildAsync` initially checks synchronously before first await. Extract execution mechanically before adding Scene. |
| `Core/Staging/StagingPlanPreflight.cs`, `CopyPlanExecutor.cs`, `StagedPackageVerifier.cs` | Physical source copy and README bytes; ordinary source size checks, exact imported README comparison. Add neutral container/generated descriptors and Scene identity checks separately. Do not call existing size checks hash verification. |
| `Core/Archive/ArchiveService.cs`, `NativeZipArchiveWriter.cs`, `ArchiveVerifier.cs` | Native ZIP, temporary verify-before-commit, exact staged bytes, explicit Replace. Verifier repeatedly scans entries with `SingleOrDefault`: quadratic lookup work. Preserve semantics, index once in a separate measured change. |
| `Core/Readme/ReadmeResolver.cs`, `ReadmeGenerator.cs`, `ReadmeContextBuilder.cs`, `ReadmeFactsFingerprint.cs` | Generated/custom/imported/none; exact customization text; fingerprint of generated output; imported bytes preserved. Reuse mode mechanics; Scene generator has different installation prose. |
| `Core/Persistence/ProjectSerializer.cs`, `ProjectMigrationService.cs`, `Domain/ProjectSchemaVersion.cs` | Flat schema 4; legacy 1-4 migration; root version checks; unknown project members rejected; atomic save. Preserve Model serialization. Scene gets a distinct versioned format and dispatching facade. |
| `Core/Persistence/ProjectSourceReferenceService.cs`, `MissingSourceRecoveryService.cs`, `Domain/PersistedSourceReference.cs` | Absolute/relative/recovery-root/remainder, valid-relative-first, grouped explicit remap and no guessing. Share representation/primitives; scene derived dependencies repaired by root/profile rescan, not deleting model entries. |
| `Core/Persistence/SettingsService.cs`, `RecentProjectsService.cs` | Settings schema 1, unknown properties ignored, future schema load returns defaults without writing; later Save can overwrite. `AddOrPromote` saves. Keep compatible optional fields at schema 1; future load fallback is not a session-wide write guard. |
| `UI/MainForm.cs:180,4097,4364` | Constructor uses `.Load().Settings`; TrackRecent can save; cards synchronously call `projectSerializer.Load`. Change card dispatch without introducing scene analysis on refresh. No stored kind hint needed. |
| `UI/MainForm.cs`, `Presentation/PackagePlanTreeBuilder.cs`, `ValidationPresenter.cs` | Launch plus five model stages; lifecycle/dirty/rename/review state; shared output tree. Extract model editor mechanically before Scene controls; preserve model layout/copy/focus behavior. |

All behavior in this table is **[PB]**; reuse/generalization prescriptions are **[REC]**. Core is `net10.0`, WinForms `net10.0-windows`; current app projects have no direct third-party NuGet references. `Directory.Build.props` enables warnings-as-errors. Existing tests use MSTest; no test count is represented as a fresh run.

### 2.2 Baseline Behaviors Not to Reintroduce or Change

Current `ReadmeConfig.IncludeControlGroupsInfo` is an obsolete compatibility field forced false by current serialization/migration. Current generated Model installation wording includes `If Windows asks to merge folders or replace files when updating, allow it.` Preserve it for Model; do not copy it into Scene. **[PB]** Older conversation requirements are not a reason to reverse accepted v1.0.4 behavior.

Model material advisories remain non-blocking as currently tested; missing optional companions alone remain non-suspicious. Source files remain read/inspect/copy only. Build history does not manufacture release history. Existing version/output decisions remain explicit. New scene strictness must never leak into Model planning.

### 2.3 Regression Anchors

Keep assertions in `PackagePlannerTests`, `ModelCompanionDetectorTests`, `MaterialSourceDiscoveryServiceTests`, `MdlV49MetadataReaderTests`, `MdlMaterialPlanningValidationTests`, `PackageValidatorTests`, `StagingBuilderTests`, `ArchiveServiceTests`, `BuildCoordinatorTests`, `ReadmeCoreTests`, `DomainAndSerializationTests`, `PersistenceServicesTests`, `ProjectCloneServiceTests`, `SourceExpansionSafetyTests`, and WinForms lifecycle/presentation tests.

Specific anchors include `ValidatorConsumesPlanFactsWithoutInvokingMdlReader`, `UnsupportedAndCorruptMetadataDoNotPreventOrdinaryPlanningOrBuildReadiness`, `NativeZipPreservesExactStagedMembershipAndBytes`, `GeneratedReadmePreviewReviewAndBuiltZipUseSameCurrentInstallRoots`, `RebuildingSameVersionAppendsBuildEventsWithoutCreatingDuplicateChangelogEntries`, and `SchemaVersionsOneThroughFourLoadSafelyAndCurrentRoundTrips`. Inspecting these tests is not running them or rendering a model in SFM.

## 3. Recovered DMX Packager Study

### 3.1 Verified Behavior

| Behavior | Concrete recovered evidence | Retain / discard |
|---|---|---|
| Game paths read by stripping quotes and matching first token exactly `Game`; special-cases `|gameinfo_path|.` | `OLD/src/DmxPackageLib/DmxPackageLib.cs`, `GetBaseSteamPath:39`, `ParseGameInfo:44` | Retain ordered mounts. Combined `game+mod` IDs are ignored; replace line parsing with ordered KV semantics. |
| First existing model; enumerates only its immediate directory and includes paths containing the basename | `ResolveModel:77`; IL METHOD 16 | Family intent useful; nonrecursive substring matching overincludes. |
| All CD-directory/texture/root material matches collected | `ResolveMaterial:103`; IL METHOD 17 | Wrong abstraction for shadowed copies; use one winner resolver. |
| Fixed VMT parameter list, line parsing and `.vtf` suffix | `ResolveVMT:143` | Dependency intent useful; no proper patch/conditional/proxy semantics. |
| Maps prefixed `maps/`, sounds `sound/`; differing first/all-match policies | `ResolveMap:173`, `ResolveSound:194`, `ResolveTexture:128` | Candidate path-form hypotheses, not proof of every SFM schema. |
| MDL seeks fixed offset 204; BSP fixed offsets 8/568, prop entities/static prop names | `ReadMDL:209`, `ReadBSP:234`, `BSPEntity.cs`, `BSPGameLump.cs` | Not complete BSP/material/pak closure; do not inherit unversioned offsets. |
| DMX seek 54, strings filtered for extensions; model path must contain installation prefix; texture key assumes next string value | `ReadDMX:287`; IL METHOD 25 | Evidence of the author's assumptions. Not structural DMX semantics; per-string exceptions swallowed. |
| Fixed DMX/BSP/MDL traversal chain; missing results can end without diagnostics | `Archive.cs`, `DMX_Packager_UI/Form1.cs:86` | Follow transitive intent, replace with explicit graph/statuses. |
| Lowercase/regex relative-path stripping and overwrite copies into a directory | `MakeRelative:365`, `PrepareForArchiving:382`; IL METHOD 27 | No ZIP creation in this implementation. Reuse PB's safe staging/native ZIP instead. |

These are **[OLD]**, not original-binary runtime qualification. `README_RECOVERY.md` describes reconstruction and inferred source details. Do not require bug-for-bug compatibility or execute the historical program during ordinary packaging.

### 3.2 Permission and Acknowledgement Correction

`OLD/original/DmxPackageLib_Methods.txt` contains an author's permission to use the DLL broadly, requesting authorship credit. Revision 1 omitted this evidence. The document names the DLL, not a clear license for reconstructed C#; this review does not expand that grant. No recovered code or binaries are selected for reuse. Acknowledge LordAardvark's historical DMX Packager in project notices/About credits as behavioral reference, without implying endorsement or claiming a formal license for the reconstruction. **[OLD]/[REC]**

### 3.3 Historical Leads for Stage 0

The hard-coded 54-byte position is consistent with an ASCII header of the form `<!-- dmx encoding binary N format sfm_session NN -->` followed by LF and NUL for one/two-digit versions. It does not independently establish the bytes, actual version numbers or tested samples. The absolute-model filter and map/sound prefixing suggest absolute model strings, bare map names and sound-relative paths. Record these as **[HYP]** test inputs. The recovered tool may itself have been wrong; neither its assumptions nor a file-type reference can replace real session fixtures.

## 4. Dependency and Completeness Model

### 4.1 Three Identities and Three Independent Decisions

Maintain reference occurrences (element/attribute/value/shot/reason), virtual asset keys (typed relative path/path-ID/map context) and resolved content identities (provider/member/size/digest/container). Do not collapse by basename. Preserve raw expressions privately for diagnostics and original spelling for output; use qualified Windows case-insensitive comparison.

Separate resolution (resolved/missing/unsupported/parse-failed/shadowed), runtime relevance (required timeline, optional editable, nonruntime, unknown), and packaging disposition (included, embedded, prerequisite, omitted-with-disclosure, unresolved). An asset can have multiple occurrences; the strongest proven required occurrence wins. Graphs/results are observed facts, not authored persisted state.

### 4.2 Reachability and Unknowns

Default promise covers all saved timeline shots and their required loading/playback dependencies, not only the current playhead. Include supported editable bin content by default when available. Disabled tracks are not automatically optional: SFM may load their resources when opening.

Only a Gate E2 rule that proves non-timeline-only reachability and no load-time requirement can downgrade a missing/unsupported edge to a required **Omit editable content / Cancel** decision. Record the specific bin/clip and lost editability in preview/report. Shared edges used by the timeline still block. DMX bytes remain unchanged; acceptance tests must prove that retained dangling authoring references do not prevent opening. Unknown reachability remains a required-analysis issue, not an omission checkbox.

Primitive numeric/bool/time/vector/matrix/color values contain no literal file path and need no path-string scan. They may select a resource-bearing branch or index; relevant known semantics must still be interpreted. Element references/arrays must be traversed; binary blobs and strings are not classified safe by omission. Unknown attributes on a **qualified nonloading authoring class** can be nonruntime, with path-looking strings surfaced for review. An unknown class is not thereby proven nonloading in SFM. Datamodel.NET's generic Element fallback describes the library, not engine behavior.

Unknown active loading behavior is unsupported. A filename-like-string heuristic can find candidates but cannot prove completeness or justify arbitrary filesystem reads. Do not require a rule for every numeric animation sample; do require coverage for potentially resource-loading structures encountered under the qualified schema.

### 4.3 Edges

| Parent | Required interpretation |
|---|---|
| Session | Qualified model/map/direct-sound/material override/light-texture slots; nested session/clip references if present. Bins/timeline classified separately. |
| MDL | Material directory order, texture/skin/LOD alternatives, model companions, include models and declared external animation data; supported runtime event references where evidence shows they matter. |
| VVD/VTX | Family identity; dependency-bearing material replacements. No mesh decoder. Missing optional PHY/dx80/sw alone is not an error. |
| VMT | Patch parents/insert/replace, shader parameters, supported fallbacks/conditions/proxies; material and texture references. |
| VTF | Terminal byte payload. Engine-generated texture names are classified before lookup. |
| BSP | Texture strings, static/entity props, relevant sky/overlay/sound fields, embedded pak and supported sidecars/load mechanisms. Internal `*n` models are not MDL files. |
| Particles/sound event/VCD/video | Only edges established by E3. A string naming an authoring source is not automatically a runtime dependency. |
| Extras | Explicit selected files/folders; no speculative traversal of arbitrary documentation. |

SDK structures identify include-model/animation and BSP lump categories, not the complete SFM versions or semantics. [S-STUDIO] [S-BSP] **[SRC]**

### 4.4 Resolution and Termination

One `SourceAssetResolver` resolves typed virtual requests using a confirmed `SfmContentProfile`: ordered providers/path IDs, gameinfo contributions, relevant language/launch assumptions and map context. Parsers emit requests; they never independently pick physical files.

Preserve declared mount order and material directory order. Known higher-priority winner plus lower copies is an override, not ambiguity. Unreadable/corrupt winners do not silently fall back. Unknown mount semantics are errors. Apply map-pak precedence only as qualified against shipped SFM; Source SDK guidance is not a complete VPK oracle. [S-FS] [S-GAMEINFO]

Validate virtual references without silently sanitizing meaning. Absolute DMX strings require Gate E1 classification; arbitrary disk/UNC lookups are forbidden. Trusted selected physical roots may be absolute; this does not authorize untrusted asset paths outside them. Token/combined path-ID/wildcard/include semantics require explicit supported rules. No arbitrary drive search or first same-name file fallback.

Iterative queue, visited `(identity, parser/rule version, semantic context)` and separate per-context resolution prevent cycles and duplicated payloads. Preserve multiple reasons and alias destinations. VMT include cycles are semantic failures; DMX object cycles are normal graph structure. Two distinct embedded variants can remain inside separate unchanged BSPs. Two required loose variants needing the same destination block. Equal-byte dedup retains all rights/reason provenance.

### 4.5 Stock and Third-Party Policy

Replace Revision 1's installation-wide file catalog with a **small versioned provider baseline**. Stage 0 records clean SFM-shipped VPK directory/chunk identities, sizes and external SHA-256 baselines, SFM build/profile and measured MDL/BSP variants. Stage 2 implements matching. Only exact baseline matches qualify as the grouped **SFM base install** prerequisite. VPK CRC/hash self-consistency alone is not authenticity: modified archives can update their own checksums. A name such as `tf` or `pak01` proves neither provenance nor rights. [S-VPK]

Hash providers during explicit environment qualification; cache privately with a documented invalidation policy, not a permanent mtime-only trust claim. Revalidate required member bytes and profile state for build. Missing baseline match means unverified provider, not automatically corrupt or pirated content.

Loose files in stock folders are **unverified loose content**, handled through grouped include/prerequisite decisions; do not falsely call them user-authored. Group mounted third-party content by stable provider/root, with exact path membership review. Common stock scenes should require one confirmed base prerequisite, not hundreds of decisions. E5 must demonstrate this ergonomically and measure baseline verification cost.

Continue dependency traversal through excluded stock/third-party parents: a stock MDL can use a loose custom VMT. Do not substitute VPK identity for parsing its needed dependencies. First release has no precomputed all-stock dependency catalog.

Rights decisions are per provider/group, scoped by stable root identity and user-declared ownership/permission. Edits to bytes already covered by the same permission do not demand per-texture legal reconfirmation. Membership/provider changes require a visible affected-group review. Separately, plan/build consent and prerequisite expectations bind to exact current bytes; permission never makes stale analysis valid. Unknown ownership requires an explicit choice, not an inferred license grant. Whole BSP inclusion includes all embedded payload, disclosed for rights/size review.

## 5. Recommended Architecture

### 5.1 Assembly and Ownership Boundaries

Keep Core free of new parser NuGet dependencies. Add `src/SfmPackageBuilder.Scene/` targeting net10.0 and referencing Core. It contains Scene domain, parsing/resolution/graph/application services and recipient verification. WinForms references both and selects a workflow at the composition boundary. Core never references Scene. Use existing MSTest integration infrastructure or a focused Scene.Tests project for scene-specific dependency tests; existing Core.Tests remain parser-package-free.

Put third-party packages behind narrow Scene adapters. Library types must not escape into Core plans or UI models. This limits API/generator coupling, but does not magically isolate MSBuild assets or licensing: inspect transitive assets and retain actual MIT notices in the combined distribution. CC0 applies only to the project's own applicable code/assets.

Shared execution remains in Core; Scene application services orchestrate analysis/decisions and call it. WinForms does not become a temporary orchestrator. The in-memory document/session union belongs at the application composition boundary, not inside the model `PackageProject`.

### 5.2 Pipeline

```text
Model recipe -> existing model planner and captured model facts
Scene recipe + profile -> typed reference extraction -> resolved dependency graph
                     -> scope/rights/prerequisite decisions -> scene draft entries/facts

ordinary destinations -> shared-file analysis -> workflow README resolution
                     -> scene report/manifest when applicable -> final PackagePlan
final plan -> common + workflow checks -> explicit decisions -> shared build executor
           -> staging -> native ZIP -> exact archive verification -> successful build history
```

The graph is upstream explanation, not a second source of package contents. Final `PackagePlan` owns destinations, content descriptors and frozen validation/report facts. Preview/check/staging do not independently rediscover dependencies. Fresh checks reject changes between analysis, consent and build.

ReadmeResolver alone produces the README entry. Scene reports are generated after ordinary entries and README facts are known, without feeding back into source discovery. The manifest lists size/SHA-256 for every other included file, including generated README/report. It explicitly excludes its own bytes from the file table to avoid self-hashing recursion. No embedded ZIP hash. Manifest schema/version and self-exclusion are documented and tested.

### 5.3 Concrete Components

All proposed Scene paths are beneath `src/SfmPackageBuilder.Scene/` unless prefixed otherwise. These are bounded responsibilities, not a general plugin framework.

| Component | Input -> output; ownership / impact |
|---|---|
| `Domain/ScenePackageRecipe.cs`, `SceneAnalysisRequest.cs` | In-memory authored choices/profile snapshot -> analysis request. No serialization initially. |
| `Parsing/IDmxDocumentReader.cs`, `DatamodelDmxReader.cs`, `SfmSessionReferenceExtractor.cs`, `SfmSessionRules.cs` | Bounded stream/header -> typed structure -> occurrences/relevance. Parser handles encoding; extractor handles qualified SFM semantics. |
| `Parsing/IKeyValuesReader.cs`, `ValveKeyValuesReader.cs` | Ordered KV1 AST + diagnostics, explicit condition/escape/include settings. Shared syntax adapter for gameinfo/VMT; no search precedence inside it. |
| `Content/GameInfoReader.cs`, `SfmContentProfileBuilder.cs`, `SfmResolutionRules.cs` | Selected environment -> ordered immutable providers and evidence/unsupported findings. |
| `Content/SourceAssetResolver.cs`, `LooseContentProvider.cs`, `VpkContentProvider.cs`, `BspPakContentProvider.cs` | Typed virtual request/context -> winner/shadowed/missing; bounded member reads and portable identity. |
| `Dependencies/SceneDependencyGraph.cs`, `SceneDependencyWalker.cs`, format analyzers | Reference work queue -> complete supported graph or structured gaps. Dedup, cycles, runtime scope and cancellation. |
| `Dependencies/ModelDependencyReader.cs`, `ModelFamilyInspector.cs` | Bounded MDL/VTX/VVD facts -> materials/include-model/ANI and family findings. Current v49 material reader reused, not broadened for Model mode. |
| `Dependencies/MaterialDependencyReader.cs`, `MaterialParameterRules.cs`, `BspDependencyReader.cs`, `SourceEntityAssetRules.cs` | Typed resource facts -> dependency requests. Implement only qualified loaders. Named-event/external particle analyzers are conditional E3 work, not default files to create. |
| `Content/StockProviderBaseline.cs`, `SceneInclusionPolicy.cs`, `RecipientOverlayValidator.cs` | Matched provider evidence + graph + decisions -> dispositions and sender-side consistency result. |
| `Planning/ScenePackagePlanner.cs`, `Validation/ScenePackageValidator.cs` | Analysis snapshot -> draft/final plan; plan facts -> Scene findings. No parser invocation in validators. |
| `Core/Planning/PackagePlanFinalizer.cs`, `Core/Validation/CommonPackageValidator.cs` | Mechanical extraction of genuinely common steps, under model parity first. |
| `Core/Planning/PackageContentSource.cs`, `Core/Staging/IPackageContentReader.cs` | Neutral physical/generated/container-member descriptors -> bytes. Core physical/generated implementation; Scene VPK reader injected through interface. No fake `archive!member` filesystem paths or arbitrary stream delegates in plans. |
| `Core/Build/PackageBuildExecutor.cs`, `CheckedPackageBuild.cs` | Checked exact plan/output/decisions -> stage/archive/result. Existing BuildCoordinator remains model facade; no scene rules in ZIP service. |
| `Build/ScenePackageApplicationService.cs` | Analyze/preview/check/build decisions -> shared executor -> Scene build-event update after success. |
| `Readme/SceneReadmeGenerator.cs`, `SceneReportWriter.cs`, `SceneManifestWriter.cs` | Finalized package facts -> mode-aware README and mandatory reports. Same generated text drives provenance. |
| `Verification/ScenePackageVerifier.cs`, `SceneVerificationResult.cs` | Untrusted manifest + extracted/installed files + recipient profile -> integrity/collision/winner/prerequisite report. Read-only, no installation. |
| `Persistence/SceneProjectSerializer.cs`, `SceneSourceRecoveryService.cs` | Added only after runtime slice; authored recipe serialization and explicit root remap. No stored graph. |
| `UI/Services/PackageDocumentService.cs`, `UI/Editors/ModelPackageEditor.cs`, `ScenePackageEditor.cs` | Thin type dispatch/session composition and distinct editors. Core/Scene services own work; UI owns controls/dialogs. |

Use actual read-only backing/defensive copies for new analysis/plan snapshots. Every substantial service accepts cancellation where work can be long. Do not proliferate wrappers for trivial strings; preserve typed virtual/content identities where they prevent concrete mistakes.

### 5.4 Execution, Safety and Performance

**No mandatory analysis host before Stage 0.** Default qualified design is one background analysis task in-process, with cancellation, bounded readers/graph, streamed payloads and no UI access. Stop scheduling new work on cancellation; do not claim hard preemption of an uncooperative library. Only one active scene scan per session; stale results are discarded by revision.

Gate E6 measures eager and deferred parsing, table allocation, malformed lengths, memory, latency and cancellation across real large sessions. Lazy decoding can defer both allocation and exceptions; keep streams alive for traversal and catch errors at access as well as initial load. It is not a security boundary. Community files are untrusted even when selected by their sender.

Do not install arbitrary guessed limits from Revision 1 (256 MiB, 500,000 elements, 1 GiB, 120 seconds). E6 records observed range, adversarial tests, headroom, checked-count bounds and explicit supported budgets. Limit hits report incomplete analysis. If decoder allocation/cancellation cannot be bounded acceptably, block shipping and propose measured parser changes or isolation for approval. A second EXE/IPC/Job Object is a contingency, not an item a coder may silently add.

Scene plans capture SHA-256 of dependency-bearing files and selected payloads; revalidate inputs, container/profile state and excluded prerequisites before publishing. Same-size mutations and newly appearing higher-priority overrides invalidate analysis. Content reads verify staged identity; archive verifier still compares exact bytes against staging. Preserve model source checks unless a separately gated change is justified.

Index normalized ZIP entries once; explicitly reject duplicate case-equivalent normalized entries, then use O(1)-average lookup per staged file while keeping exact byte comparison, cancellation and diagnostics. This changes lookup complexity, not ZIP format or replacement rules. Qualify it separately from mechanical pipeline extraction.

### 5.5 Recipient Manifest Consumer

Manifest v1 contains: package/profile/scene identity, root DMX destination, supported scope and omissions, every non-manifest included path/size/SHA-256, prerequisite provider identity plus needed virtual member identities, context-dependent expected winners, install assumptions and tool/ruleset version. No donor absolute paths, secret profile data or private logs. The report is a projection of these same plan facts.

The verifier first validates schema, bounds, safe paths, duplicate keys/paths and package hashes. It never trusts manifest paths to access arbitrary disk locations. Pre-install mode compares proposed destinations with existing files (absent/same/different) and simulates the specified overlay against the recipient profile. Installed mode resolves required virtual paths under the real profile, including map contexts, and compares expected bytes; a correct file in a shadowed lower root is a failure. Prerequisite/provider mismatch remains distinct from missing included content.

Results: Package bytes verified; recipient conflicts; missing/mismatched prerequisites; wrong winning asset; unsupported profile; or supported file/lookup checks passed. Reports never say SFM rendered successfully. Unsigned manifests provide internal consistency, not trusted authorship. The verifier is a first-public-release gate, not an indefinitely deferred consumer or a new general-purpose installer.

## 6. Data and Persistence Design

### 6.1 Compatibility Contract

| Format | Read/write policy |
|---|---|
| Existing Model schemas 1-4 | Existing `ProjectSerializer` migration/load. Save Model as flat schema 4 exactly as now. No added `packageKind`, no nested payload, no new Model schema. |
| New Scene schema 5 | Root `schemaVersion: 5`, `packageKind: scene`, Scene payload. No nested legacy Model marker. Old v1.0.4 rejects the future root version before interpreting payload. |
| Unsupported/conflicting type/version | Structured failure; no default empty project and no silent overwrite. |

The application document facade dispatches on declared root version/type, not whether a Models list happens to be populated. Legacy aliases accepted by the current migration path remain supported. A schema-4 file containing Scene discriminator/fields is invalid, not an implicit conversion. Future Model evolution is a separately reviewed change; do not reserve fictitious schemas now.

Scene wire schema is finalized and tested in Stage 7 **after** working runtime evidence and recipient verification. Early stages use in-memory recipes only. This blueprint establishes storage boundaries now, not an early frozen field set requiring speculative migrations.

### 6.2 Scene Authored State

Persist a stable recipe/package ID; scene source reference and proven destination; title/current version/current change note; author/website/usage/additional resources/credits; archive filename; README config/custom provenance; explicit Extras; selected gameinfo/profile rule/language/launch intent and explicitly chosen roots; grouped permission/include/prerequisite choices; editable-content omission decisions; successful build events.

Do not introduce Scene accumulated changelog or Start New Version in the first release. A current version/change note and separate `BuildRecord` history support rebuild decisions without modifying `PackageProject` release operations. Keep all Model history controls and semantics unchanged. No Scene New From Current command initially.

Persisted group decisions refer to stable provider/root and scope, not thousands of per-file permission records. Prerequisite evidence retains exact required identities and current build consent remains revision/digest-bound. Root replacement or changed membership requires re-evaluation. No binary parse graph, runtime flags, detected/missing state, cache paths, VPK offsets or current readiness in the recipe.

Machine defaults remain outside `.sfmpack`; selected project-specific content environment is authored intent, not an exported machine configuration. Reports/ZIP do not include those local source paths. Keep generated/custom/imported/none semantics and exact imported/custom bytes. Scene provenance uses exactly the authoritative Scene generated text/facts; legacy custom text with no baseline is not stale.

### 6.3 Source Recovery and Dirty State

Reuse `PersistedSourceReference` absolute/relative/recovery-root/remainder fields for DMX, gameinfo/explicit roots, Extras and imported README. Relative forms are computed against the `.sfmpack` file location. Valid stored resolution and rescans are observations, not authored edits. An explicit locate/root remap, permission/scope decision, metadata/customization or archive-name edit is dirty. Cancellation/failed Open preserves the active document.

Scene missing-source recovery groups by former root. Locate the root DMX or leave the recipe incomplete; remapping content roots must rescan and show changed winners. Do not persist derived dependencies as dozens of editable SourceEntries or offer Remove From Package for a required scene asset. Existing Extra/README locate/remove behavior is reusable.

### 6.4 Settings and Recents

Keep shared settings at **schema 1** with optional Scene preferences such as LastSceneDirectory/last selected profile hint. Existing v1.0.4 ignores unknown properties while retaining its known settings. If the old app later saves, it may discard the new optional preferences; that bounded loss is acceptable and documented. Do not make new preferences the only copy of scene recipe decisions.

The new application must preserve unknown optional JSON members on its own read-modify-save path, including the envelope where applicable, with explicit known-value precedence. Preserve load status so a genuinely unsupported future settings file suppresses incidental writes (recents/directory tracking); explicit reset/save requires a deliberate decision. These are narrow settings changes in Stage 7. They cannot retroactively fix v1.0.4's handling of a hypothetical future schema, which is why this feature does not bump the shared schema.

No new persistent recent-kind field and no recent filter. Keep one chronological recent list with kind derived from successful document dispatch. `ReadRecentProjectCardMetadata` must route through cheap document metadata/load services. Reading recents or a recipe must not run DMX parsing, gameinfo expansion, VPK indexing, dependency analysis or runtime verification. At most the already-established explicit-source resolution probes occur; preferably cards use a bounded JSON-only projection. Missing/unknown documents display unavailable kind without repeatedly prompting. Full validation occurs on Open/Analyze, not every card refresh.

## 7. UX Blueprint

Keep the current launch composition/assets and left actions/right recents. Add **New Model Package** and **New Scene Package**, retain Open and return-to-current behavior. Recents show Model Package / Scene Package labels in one ordered area.

One document session owns dirty/save path, output folder, analysis revision and build state. Switching kind is New/Open with Save / Don't Save / Cancel, never in-place conversion. Host the unchanged five-stage Model editor and a separate Scene editor. Extract shared panels only where controls/behavior genuinely match; no giant kind-switch in every model handler.

Scene journey:

1. **Scene & Content:** choose saved DMX; confirm environment/profile; see support/saved-file status; Analyze explicitly. No expensive scan for every path keystroke.
2. **Dependencies:** Included, Embedded, Prerequisites, Needs attention; reasons and selected provider visible; group rights/stock decisions; distinguish timeline requirements from proven editable-only omissions. Show changed winners after root recovery.
3. **Package Details:** current title/version/note, reusable creator/credits fields, README preview/customization and optional Extras. No Scene changelog or clone UI in the first release.
4. **Review & Build:** actual ZIP tree, separate prerequisites/omissions, build check, output decisions/progress/cancel/result. Never display embedded/prerequisite assets as loose ZIP leaves.

Build invalidates stale facts and rechecks exact content. Review preserves stable destination selection/expansion/top item within a document and resets across New/Open. Same-version rebuild checks use Scene build history; explicit output Replace/Choose Another Name/Cancel remain application-layer decisions. Successful build reports archive verification, not recipient runtime success.

Expose **Verify Scene Package** as a read-only utility, not another packaging mode or mandatory stage for Model users. It takes extracted manifest + recipient profile + target folder, supports pre-install and installed checks, and returns grouped conflicts/requirements. No auto-overwrite buttons.

| Condition | Severity / action |
|---|---|
| Missing/unsupported required timeline or load-time edge; unknown active loading behavior; unreadable winner; unsafe path | Error; block. |
| Proven editable-only missing edge | Explicit omission decision + warning/report; no claim of full editability. |
| Known nonruntime provenance/authoring field | Detail/Information; no fabricated missing file. |
| Qualified authoring string that merely resembles a path | Non-blocking review warning when appropriate; not proof of an external dependency. |
| Unknown runtime shader/loader whose closure is unproven | Error despite probable matching files; discovery is not completeness. |
| Unreviewed group rights/prerequisite choice | Required application decision, not bypassed by warning policy. |
| Verified stock prerequisite/shadowed or embedded resource | Information plus recipient requirements. |
| Custom README potentially stale | Existing Information finding only; no text mutation or build block. |
| Same-size source change, changed winner/consent or output collision | Rescan/redecision or existing explicit output policy; no blind continuation. |

## 8. Parsing and SFM Resolution Strategy

### 8.1 Libraries and Build Policy

Candidate pins remain KeyValues2 **1.0.0**, ValveKeyValue **0.71.0.528**, ValvePak **5.0.2.177**, subject to E6 inspecting the actual package contents/API/source mapping. Do not float to a prerelease because it exists. No packages were installed in this task. Current master documentation is not evidence that every API exists in a pinned artifact.

KeyValues2's versioned page advertises net9.0, a generator dependency, generic element loading and deferred attributes. These are adoption inputs, not proof of SFM schema behavior or safety. [S-DMX] Keep all third-party public types inside Scene adapters. Evaluate generator asset flow by inspecting restore assets and generated output in Scene, WinForms and test consumers; narrow analyzer/build/buildTransitive exposure without excluding runtime assemblies needed in publish. Do not assume `PrivateAssets=all` solves transitive runtime packaging. [S-NUGET]

Retain warnings-as-errors. Use pinned SDK/dependency lockfiles and the repository's deterministic restore policy (`NuGetAudit=false`) for reproducible build gates, then run a **separate required security-audit gate** with audit enabled and reviewed transitive vulnerability output. A vulnerability is reviewed/remediated, not hidden; unavailable advisory network leaves the security gate incomplete. Do not globally suppress generator/compiler warnings. Documentation must distinguish build reproducibility from release security review. [S-AUDIT]

ValveKeyValue sits behind `IKeyValuesReader`; explicit SFM profile conditions replace host defaults. Upstream options derive defaults from the host OS. [S-KVOPTIONS] Qualify leniency using malformed VMTs actually accepted by SFM; no blanket claim that every Source malformed input is tolerated. A supported tolerance rule is narrow, diagnostic and dependency-preserving. No silent truncation or regex rescue. If accepted runtime syntax cannot be analyzed completely, report UnsupportedMaterialSyntax with the sample, not a false assertion that SFM itself rejects it. SDK error paths are comparative evidence only. [S-KV]

Use ValvePak only for bounded container access, never for deciding mount order or rights. Self-checks verify integrity, not a stock origin. LibBSP alternatives from Revision 1 remain unselected: a narrow bounded dependency-lump reader avoids unrelated geometry APIs; qualify actual SFM variants before writing it. No Python/Blender/legacy DLL runtime dependency.

### 8.2 DMX Interpretation

Read encoding and session schema separately; candidate binary 5/keyvalues2 1 support must be established from real files, with other encodings admitted only by fixtures. E1 portability is first; E2 inventories a representative corpus before semantic coding.

The semantic registry maps profile + element class + attribute + value type to role, path form, reachability and evidence fixture. Include graph arrays/stubs/cycles, timeline/shot links, authoring bins, overrides/light cookies and any embedded effect structures actually present. Header/string-dictionary scans are not the semantic parser. Never round-trip-write a parsed scene for packaging.

Absolute path classification has three outcomes: proven ignored provenance (preserve bytes, no lookup); proven SFM-resolved portable alias (apply that exact qualified mapping and verify byte-identical DMX on another root); genuine donor-absolute runtime dependency (unsupported under the no-rewrite contract). No unrestricted prefix stripping. If ordinary sessions require rewriting, stop the product gate and propose a separate requirement change; do not build the rest on a knowingly unportable root format.

### 8.3 MDL/Materials

Measure required stock/custom MDL versions before choosing supported layouts. Current PB v49 material facts are reusable; v48 and v49 dependency sections need independent offsets/fixtures. Read include-model, declared animation filename, geometry presence/required family metadata, applicable material/LOD alternatives, and proven runtime event resources. Inspect checksum identity of selected VVD/VTX winners; preserve resolver precedence. No invented optional-companion warnings or mesh decoding.

VMT patch/conditions/proxies require structural parsing plus supported semantics. Include all qualified resource-bearing renderer alternatives where pruning would omit a needed frame/skin. Generated textures are terminal engine resources; a map sky name leads to face-material requests, then each material's real texture parameters.

For an unknown string parameter, a constrained candidate lookup may discover an existing VTF/VMT and create a **probable edge**. Record why, traverse safely, apply rights/collision rules and conservative size disclosure. This is not harmless by definition and does not prove that unmatched parameters or a custom proxy have no other dependencies. A known shader's unfamiliar passive property may be qualified; arbitrary runtime name synthesis remains unsupported.

### 8.4 Sounds, Particles, VCD and Video

F10/F11/F12 remain hypotheses. E3 independently tests each, using original authoring resources present/absent and saved session variants. Do not infer current behavior from the old packager's WAV-only handling.

If embedded particle data suffices, traverse its qualified material/model references and do not require an original PCF. If SFM sound playback uses concrete stored wave paths, event labels do not trigger registration machinery. If VCD import is baked, preserve its path as provenance, not a required file. Conversely, proven external runtime dependencies need supported closure/loading semantics or explicit unsupported status. External PCF/VCD/video/general sound-registration implementations are deferred unless gate evidence establishes their necessity and bounded scope. Map-entity sound/particle loaders are a separate context from session sound clips.

### 8.5 BSP and VPK

Inspect bounded versioned dependency lumps, not geometry: entities, texture strings/tables, static-prop dictionary, embedded ZIP and supported sidecars. Qualify map/prop versions and compressed/external lumps; no invented v20-only assertion about all SFM maps. Unknown dependency-bearing layouts block the affected map, not silently empty its graph.

Embedded content is a virtual provider and remains inside the original BSP. Recurse into packed material/model dependencies and apply qualified map context to all relevant session edges. Preserve whole BSP bytes; disclose incidental embedded content and rights granularity. Bound ZIP/VPK counts, decompression and paths; detect case-equivalent duplicate names and corrupt chunks. Do not unpack every VPK in preview.

### 8.6 Path Length and Saved-File Freshness

F19's Source MAX_PATH claim and F20's autosave naming/timestamp behavior are not verified. E7 tests both before specialized warnings are implemented. Long-path awareness of PB does not establish SFM or extractor support. Do not calculate a guaranteed runtime threshold from an assumed default install prefix. Recipient verification can report actual target path lengths against an empirically supported profile, with uncertainty labelled.

Always state that saved DMX bytes, not unsaved editor memory, are used. If E7 proves a same-session autosave convention, a newer file is a non-blocking freshness hint, never automatic replacement or proof of unsaved content. Timestamp anomalies, backups and user-defined autosave scripts must not make it a correctness gate.

## 9. Stage 0 Evidence Gates and Risks

Stage 0 is measurement before product implementation. Future throwaway inventory/fuzz/probe tooling is permitted outside the product tree when that stage is authorized. Record tool version/hash and exact inputs privately; publish only rights-cleared fixtures and sanitized outputs. It must not silently become a shipping Scanner dependency. This documentation task does not authorize creating those tools now.

| Gate | Evidence required and resulting decision |
|---|---|
| **E1 - Absolute paths / viability FIRST** | Capture genuine sessions with model/sound/map paths; separate serialized dictionary strings from live attribute values. Open exact unchanged DMX on a second installation/root with donor paths unavailable; record actual winner behavior. Decide provenance/portable mapping/nonportable per slot. Ordinary workflows failing no-rewrite portability stop implementation expansion. |
| **E2 - Corpus and completeness** | Inventory class -> attribute -> type -> sanitized samples/reachability across minimal, rigged character, constraints/control groups/presets, scripted authoring fields, multi-shot/bins/disabled content, overrides/lights, particles/audio/map and large recorded sessions. Include supported historical/current SFM builds where available and multiple independently authored projects. Separate discovery and held-out qualification cases. Measure false blocks and unknown resource behavior, not just successful parsing. Freeze nonloading-class and optional-bin rules only from evidence. |
| **E3 - Dependency hypotheses** | Test embedded particles after removing PCF; event-labeled clips with WAV/script separately absent; imported VCD/video provenance versus live external slots. Map loaders tested separately. Record outcomes that add or remove parser work. Absence in one minimal file does not prove global absence. |
| **E4 - Content order and installation** | Distinctive same-path files in A/B roots, loose/VPK/map pak, path IDs/conditions/tokens/wildcards/language/startup changes. Prove or reject collaboration-folder precedence over usermod using supported tooling. Confirm session subdirectory relocation and nested references. |
| **E5 - Base provider baseline / format census** | Clean entitled SFM VPK directory/chunk SHA-256 baseline with build identity, matched members and MDL/BSP version census. Measure setup cost and stock+loose override behavior; demonstrate grouped decisions for stock-heavy scenes. No installation-wide per-file dependency catalog. |
| **E6 - Parser adoption / resources** | Inspect exact pinned packages/generators/notices, compile under warnings-as-errors, validate deferred reads/failures, conditions/leniency, malformed allocations/EOF loops, cancellation and peak memory across corpus. Freeze defensible budgets; require separate architecture approval if in-process safety is insufficient. Record security audit status separately. |
| **E7 - Recipient/runtime details** | Measure path length including recipient prefix/extractor, autosave relationship, and supported console capture (test `-condebug`, not assumed). Produce repeatable logging and pre-install negative controls. |

No Scene parser implementation starts before E1 and the core E2/E4 profile are viable. Feature-specific E3/E5/E6 results determine the supported slice before its stage. A missing available historical build narrows the support statement; do not invent evidence. A gate failure calls for review of scope, not coding a speculative bypass.

Principal remaining risks: Model regressions during extraction; unsupported old/new Source variants; active custom scripts synthesizing assets; huge/malformed data; profile changes mid-build; whole-container redistribution; sender-private strings retained in unchanged DMX; and recipient override effects on other scenes. Mitigate with parity, bounded profile support, digest checks, group rights review, a source-DMX privacy warning and read-only recipient verification. Hash equality never proves legal rights or pixel equivalence.

## 10. Bounded Implementation Sequence

Every stage requires separate review. File names are proposed responsibilities, not code already created. Do not weaken existing tests. Where a prerequisite gate fails, stop that capability and return evidence rather than silently broadening scope.

### Stage 0 - Evidence, Portability and Library Qualification

- **Objective:** execute E1 first, then E2-E7 to define a useful qualified profile and resource budgets.
- **Files/work:** evidence outside product tree; `docs/scene/SUPPORTED_PROFILE.md`, `CORPUS_MANIFEST.md`, `PARSER_ADOPTION.md`, provider-baseline metadata after privacy/rights review. No production changes.
- **Dependencies:** authorized access to real SFM and redistributable or private fixtures.
- **Tests:** positive/negative runtime path cases, held-out corpus extraction oracles, precedence probes, package safety measurements, signed-off dependency/security inventory.
- **Untouched:** entire existing application, Model workflow, saved files and release artifacts.
- **Done:** gates have concrete results, an accepted first-slice profile and no unresolved ordinary-scene portability blocker. Particle/event/VCD/path/autosave findings are recorded without being assumed.

### Stage 1 - Scene Assembly and In-Memory Contracts

- **Objective:** establish Scene -> Core dependency direction, immutable analysis/recipe identities and narrow parser adapters; no Scene wire format yet.
- **Files:** Scene project, Domain/analysis contracts, adapter interfaces, Scene tests; solution membership and reviewed dependency pins/lockfiles/notices.
- **Dependencies:** E1/E2/E6 adopted profile and package decisions.
- **Tests:** warnings-as-errors builds, no parser packages/generated artifacts in Core/Core.Tests, runtime dependency publish smoke, cancellation/revision contract, immutable snapshots.
- **Untouched:** Model serialization/settings/UI/planner/build pipeline.
- **Done:** package assets and types stay behind Scene boundaries; no worker EXE, IPC or speculative history framework.

### Stage 2 - Content Resolution and Base Provider Qualification

- **Objective:** deterministic virtual-path winner and grouped baseline prerequisites.
- **Files:** Scene Content/KV adapters/profile/providers/resolver/StockProviderBaseline and focused tests.
- **Dependencies:** Stage 1, E4/E5.
- **Tests:** duplicate Game keys/combined IDs, constrained includes/tokens, fixed conditions, A/B order, loose/VPK chunks/preload, corrupt winner, shadow trace, forged-but-self-consistent stock archive rejection, stock model with loose material override; setup-time/memory measurement.
- **Untouched:** Model automatic material discovery, package writer, persistence/UI.
- **Done:** winner bytes match SFM oracles; provider origin is not inferred from names/CRC; common stock setup is grouped and usable.

### Stage 3 - Typed Scene and Model/Material Closure

- **Objective:** generate inspectable dependency graph for the first slice, not a ZIP.
- **Files:** DMX reader/extractor/semantic registry, graph/walker, family/MDL/VTX/VMT analyzers and supported E3 embedded-slot extractors.
- **Dependencies:** Stages 1-2 and relevant format/semantics gates.
- **Tests:** corpus/header/absolute slots, numeric vs resource fields, bins/disabled reachability, cycles, include-model/ANI/family checks, patch/leniency/conditions/probable material edges, malformed/deferred reads, large-session cancellation.
- **Untouched:** existing Model readers' public semantics and warning policy, UI, serialization, staging/ZIP.
- **Done:** required graph matches expected fixtures, unknown active loading behavior cannot become Complete, held-out normal sessions meet the accepted usefulness gate.

### Stage 4A - Mechanical Shared Pipeline Extraction ONLY

- **Objective:** factor model build/finalization/common validation mechanics before any Scene planner consumes them.
- **Files:** Core PackageBuildExecutor/CheckedPackageBuild, common validator/finalizer and existing model coordinator/planner facades only where extraction requires.
- **Dependencies:** established Core contracts; accepted baseline fixtures. Stage 3 may finish independently but no Scene behavior enters this change.
- **Tests:** exact Model destinations/README/validation severities, version/output decisions, cancellation, source safety, failure cleanup, history ordering; full suite and Model physical sanity check.
- **Untouched:** scene behavior, content descriptors, archive lookup optimization, UI layout and serializers.
- **Done:** model parity proven. Do not bundle features or performance changes into the extraction diff.

### Stage 4B - Shared Content and Verification Adaptations

- **Objective:** bounded neutral container/generated content, Scene digest support and linear ZIP lookup.
- **Files:** Core content descriptors/reader interface, staging preflight/copy/storage/verifier records; Scene VPK reader implementation; ArchiveVerifier entry index as its own reviewable change.
- **Dependencies:** 4A parity and Stage 2 provider contracts.
- **Tests:** physical/generated behavior parity, VPK member reads, duplicate normalized ZIP names, exact byte checks, same-size mutation, cancellation, large entry-count scaling without brittle wall-time assertions.
- **Untouched:** Model discovery/README wording, native ZIP replacement contract, Scene UI/persistence.
- **Done:** generic execution supports actual new inputs; indexing changes lookup work without weakening verification.

### Stage 5 - First Runtime Scene Slice

- **Objective:** in-memory session recipe -> exact ZIP -> recipient opens one custom-model/material scene with no undeclared external requirements. No production scene editor or saved schema yet.
- **Files:** Scene planner/validator/application service, mode-aware Scene README/report/manifest writers, first verifier services and integration fixtures. Reuse Core executor.
- **Dependencies:** Stages 0-4; an inspected map or a genuinely dependency-free minimal fixture, not an invented precomputed stock dependency catalog. Add only the narrow qualified map evidence/reader needed for the fixture; full map coverage is Stage 6.
- **Tests:** final plan/preview/README/ZIP parity, exact original DMX bytes, permissions/scope decisions, all README modes/provenance, stale-input/consent rejection, separate build events; negative recipient control then post-install reopen with log/winner/shot assertions.
- **Untouched:** Model UX/serialization/changelog, release assets; no generic scene persistence framework.
- **Done:** objective Q-runtime checks pass with donor paths unavailable. This is an internal limited milestone, not a claim of complete public Scene support.

### Stage 6 - Supported Maps, Audio and Recipient Verification

- **Objective:** finish the accepted dependency profile and actual recipient collision/winner checking before committing saved choices.
- **Files:** BSP/embedded/sidecar rules, direct audio extraction, only E3-justified extra analyzers, overlay/manifest/verifier services, map/audio/conflict fixtures.
- **Dependencies:** Stage 5 plus E3/E4/E7 results. No automatic global manifest editor.
- **Tests:** packed VMT -> external VTF, map context across shots, sound playback with proven stored fields, particle/provenance cases, identical/different recipient target collisions, wrong higher-priority winner, malformed/untrusted manifest, namespace/root-relative session behavior.
- **Untouched:** Model workflow and current recipe schema.
- **Done:** supported recipient pre/post checks are actionable; all supported runtime fixtures pass and unsupported classes are reported accurately. Remaining unknowns do not become silent omissions.

### Stage 7 - Scene Persistence and Compatible Settings/Recents

- **Objective:** freeze actual authored choices demonstrated by Stages 5-6 and save/reopen them without analysis on load.
- **Files:** Scene schema-5 serializer/source recovery; application document dispatch; optional schema-1 settings fields/unknown-member retention/load-status guard; recent-card document projection.
- **Dependencies:** runtime slice and stable decision/report contracts. This replaces Revision 1's early persistence stage.
- **Tests:** unchanged flat Model schema4 writes read by legacy codec; old 1-4 loads; Scene round trips; future/conflicting versions rejected; old settings reader/writer interop preserving known values; future-settings incidental-write protection; relative/absolute/group remap; zero analyzer/VPK calls from recents; dirty-state semantics.
- **Untouched:** Model ProjectSerializer format, release-history methods, model data fields and scene parsing rules.
- **Done:** both kinds reopen predictably; settings downgrade costs only new optional preferences; no observed graph in JSON or unintended Model migration.

### Stage 8 - Mechanical Model Editor Extraction

- **Objective:** document-aware shell with the existing Model editor unchanged.
- **Files:** MainForm/session ownership, ModelPackageEditor, minimal common panels; lifecycle/presentation tests.
- **Dependencies:** Stage 7 dispatch and existing Core application facades.
- **Tests:** all Model five-stage/rename/focus/dirty/recovery/recents/review behaviors; physical model A-E and baseline layout comparison.
- **Untouched:** Scene controls, model layout/copy/assets, parser/business policy.
- **Done:** mechanical move has parity evidence. Do not mix new scene controls into this change.

### Stage 9 - Scene UX and Verification Utility

- **Objective:** expose working services through the four-stage Scene journey and read-only recipient utility.
- **Files:** ScenePackageEditor/dependency presenter/profile chooser, launch actions/kind labels, grouped recovery/decisions, verifier dialog; shared Review/Build integration.
- **Dependencies:** all service/persistence gates and Stage 8 parity.
- **Tests:** background responsiveness/cancel/stale-result handling, grouped decisions, actual winner explanations, editable omission disclosure, save/reopen/remap, output/version decisions, custom README ownership, pre/post verifier UI; no per-file modal storms.
- **Untouched:** Model feature set, start assets except new required actions, ZIP format, release artifacts. No recents filter/Scene clone/changelog.
- **Done:** both workflows function through actual UI without bypass paths around completeness checks.

### Stage 10 - Adversarial Qualification and Distribution

- **Objective:** qualify the supported public profile with independent review and clean recipient tests.
- **Files:** qualification records, notices/BUILD/release docs and package inclusion of Scene dependencies; fixes only for demonstrated defects.
- **Dependencies:** all prior gates.
- **Tests:** matrix in section 11, vulnerability review, clean self-contained win-x64 publish, offline both-workflow use, Model regression and physical 100/125/150% DPI; negative controls and SFM console/shot/audio checks.
- **Untouched:** old v1.0.4 release bytes/tags; no publication without separate authorization.
- **Done:** no missing/skipped required evidence presented as pass, no recipient conflicts hidden by sender simulation, no claim of pixel identity. Version the expansion separately from 1.0.4.

## 11. Qualification Plan

Use existing MSTest projects plus focused Scene tests. Real assets stay private/environment-supplied unless rights allow redistribution. Every fixture manifest records origin/rights, SFM build, inputs/digests, ordered provider labels, schema/layout/profile, expected occurrences/winners/graph/dispositions and runtime oracle. Do not commit personal paths or generated private session inventories.

| ID | Automated assertion | Runtime/physical gate |
|---|---|---|
| Q01 | Real binary/text DMX typed-reference parity, unknown header/schema failures | Same genuine saved session opens; exact saved bytes identified |
| Q02 | Absolute slot classification never generic prefix stripping | Second-root donor-unavailable playback/open; E1 viability |
| Q03 | Corpus class/type registry; numeric data not path scanned; unknown active loaders flagged | Held-out rigs/constraints/control groups/presets/scripted authoring sessions |
| Q04 | Timeline vs bins classification; shared required edges retain severity | Remove bin-only asset and verify load unaffected before permitting omission |
| Q05 | One custom model closure + exact suffix selection | Recipient custom model visible with donor trees unavailable |
| Q06 | Geometry/animation-only families, checksum mismatch, optional PHY | Legitimate families load; no invented optional warning |
| Q07 | Include-model/ANI/skins/LOD/material alternatives | Exercise corresponding animation/skin/bodygroup/LOD |
| Q08 | VMT patch/conditions/leniency/cycles/probable edges | Fixtures accepted by SFM yield expected textures; malformed unknowns honest |
| Q09 | DMX overrides/light textures use typed slots | Correct override/light appearance |
| Q10 | Particle embedded/external cases reflect E3, not file suffix | With and without original PCF; operators' material/model content |
| Q11 | Direct wave vs event metadata per E3 | Event/script/WAV independently absent/present; fresh playback |
| Q12 | VCD/video provenance vs actual runtime slots | Baked session with original authoring input unavailable |
| Q13 | BSP texture/entity/staticprop/sky/sidecar graph | Custom map, props and textures load |
| Q14 | Packed VMT/model external edges; no loose embedded duplication | Map pak overrides session-origin assets as qualified |
| Q15 | Multi-shot/map context conflicts handled | Switch shots/maps with fresh process to avoid cached winner masking |
| Q16 | Ordered Game/combined IDs/includes/tokens/conditions | Distinctive A/B root reversal oracle |
| Q17 | Loose/VPK/chunk/preload/corrupt provider behavior | Actual SFM precedence and mount rules |
| Q18 | Stock VPK trusted baseline vs self-consistent impostor | Clean base installation matches; stock parent/custom loose descendant |
| Q19 | Grouped rights stable across covered edits; membership review | Stock-heavy review is few group choices, not per-file prompts |
| Q20 | Repeated references/aliases/cycles deterministic | Shared-content session remains usable |
| Q21 | Missing/unreadable winner not replaced by lower copy | Useful missing diagnostics, no false full portability |
| Q22 | Unknown shader/runtime code is not certified by probable textures | Qualified custom-shader cases or deliberate supported-profile rejection |
| Q23 | Bounds, EOF loops/deferred exceptions, zip bombs, unsafe paths | No malicious fixture needs to be run in SFM |
| Q24 | Unicode/spaces/case/separator semantics and containment | Advertised SFM filename encodings verified |
| Q25 | Same-size edit/new higher override/container change invalidates facts | No separate runtime test necessary |
| Q26 | Final plan/report/README/custom/imported/none exactness | Human-readable installation scope; Model wording not inherited |
| Q27 | ZIP indexed membership, duplicates, exact bytes, linear lookup work | Large-scene package inspection |
| Q28 | In-process cancellation/task faults/revision discard and measured budgets | UI responsive under representative largest sessions |
| Q29 | Model v1-4 load/save4; Scene5 dispatch; no analysis on cards/load | Reopen kinds with old/new application as applicable |
| Q30 | Settings1 old reader/writer interop; unknown retention and future write guard | Old app use does not erase known settings/recents/creator defaults |
| Q31 | Source relative/absolute/recovery-root preservation, explicit remap dirty | Group root recovery and changed-winner review |
| Q32 | Model history unchanged; Scene current note/build events only | Explicit same-version and output decisions |
| Q33 | Namespaced scene destination/root-relative safety | Existing same-name session not overwritten unnoticed |
| Q34 | Manifest bounds/hash/self-exclusion, no arbitrary reads | Pre-install conflicts and post-install actual winner/prerequisite report |
| Q35 | Existing destination same/different, higher-root collisions, context variants | Verify unrelated recipient scene impact warning and safe manual procedure |
| Q36 | Path-budget/autosave behavior only if E7 qualified | Boundary path lengths; save/autosave freshness cases |
| Q37 | Full untouched Model expected results / byte parity | Model A-E, rename, dirty/recovery/recents/review physical pass |
| Q38 | Runtime fixture expected-winner/hash/log assertion set | Negative pre-install control, post-install all shots/audio/light checks |
| Q39 | Complete dependency/notices/self-contained publish; offline restore policy recorded | Launch and both workflows offline; no worker accidentally shipped |
| Q40 | Layout/tab/review-state assertions | True 100%,125%,150% Windows DPI, 1920x1080 and smaller usable window |

### Objective Runtime Evidence

For each supported fixture, define expected shot/time intervals, model/material IDs, light effects/audio segments and representative visual checkpoints before running. Use distinctive same-path variants for all precedence tests. Capture a clean-start SFM console using E7's verified method; `-condebug` is only a candidate until demonstrated. Compare missing-resource diagnostics against an explicit baseline/allowlist; retain raw evidence privately and summarize new errors.

On a clean recipient with donor custom paths unavailable, run the scene **before** installing and record the expected missing custom resources (or distinctive wrong winner in precedence cases). Then install exactly the package/prerequisites, restart and rerun. Stock-only cases use prerequisite removal/controlled profile mismatch as their negative control, not a fabricated claim that stock should fail. Passing requires expected file winners, disappearance of targeted missing diagnostics and the fixture's visual/audio/shot checkpoints. Clean logs alone or visible geometry alone are insufficient. Optional image comparison aids review; no cross-hardware pixel identity promise.

### Automated Build and Security Commands

At implementation gates, follow current BUILD.md's isolated artifact-directory policy. After lockfiles are deliberately generated/reviewed, use locked restore. Illustrative gate sequence (replace artifact directory with that stage's unique path):

```powershell
dotnet restore .\SfmPackageBuilder.sln --locked-mode -p:NuGetAudit=false --artifacts-path .\work\artifacts-scene-gate
dotnet test .\SfmPackageBuilder.sln -c Release --no-restore --artifacts-path .\work\artifacts-scene-gate
dotnet build .\SfmPackageBuilder.sln -c Release --no-restore --artifacts-path .\work\artifacts-scene-gate
```

Run a separate network-enabled audit restore in a separate artifacts directory with `NuGetAudit=true` and `NuGetAuditMode=all`, preserving warnings-as-errors and retaining findings. Audit-feed unavailability is an incomplete security gate, not proof of safety. Exact CLI compatibility is confirmed against the selected .NET 10 SDK in E6. Record actual test counts/skips and current audit evidence; never carry old results forward as reruns.

## 12. Remaining Questions and Removed Systems

Only empirical unknowns remain: absolute runtime path behavior; real-session schema/reachability and effect/audio/provenance semantics; shipped SFM mount/installation rules; actual stock/model/map variants; parser safety/performance; path/autosave/logging details; and rights to distribute test inputs. Each maps to E1-E7. None is delegated as an arbitrary coding preference.

Removed from the proposed first-release design: Model schema-5 envelope; settings schema bump; stored recent-kind hint/filter; full stock file/dependency catalog; mandatory analysis host/IPC/Job Objects; Scene New From Current; Scene accumulated changelog/ReleaseHistoryOperations extraction; unconditional named-event registration and PCF/VCD/video parsers; guessed file/element/time/memory/MAX_PATH thresholds. Conditional features require the named gate outcome, not a coder's convenience.

Retained: distinct workflows, central ordered resolver, immutable analysis facts and final plan truth, no source rewriting, no transitive pruning at stock parents, strict required-runtime completeness with qualified editable-only exceptions, safe staging/native ZIP and explicit decisions, mode-aware README ownership, recoverable authored source references, sender consistency plus actual recipient verification. Implementation begins only after review of Revision 2.

## Appendix A. Audit Reconciliation

Each numbered finding is reconciled individually. ACCEPT IN PART states the boundary; DEFER entries do not turn hypotheses into supported facts. Sections/stages below are Revision 2 references.

### F1 - ACCEPT

The downgrade overwrite path is real: `SettingsService.Load` returns defaults for future versions, MainForm discards status at line 180, and recent/directory writes can call Save. The passive-load test never guaranteed session-wide preservation. Keep settings schema 1 with optional fields, accepting that old saves discard only new preferences. New code preserves unknown optional fields and blocks incidental saves after genuinely unsupported future loads. Sections 2.1/6.4; Stage 7 includes legacy-reader/writer regression tests.

### F2 - ACCEPT

The current migration reads root schema before payload, and the serializer rejects unknown model fields. Model can remain flat schema 4; Scene5 already gives old v1.0.4 the intended future-schema rejection. Remove nested Model envelope, needless migration and Model Save As warning. Explicit version/type dispatch remains. Section 6.1 and Stage 7.

### F3 - ACCEPT

`ReadRecentProjectCardMetadata:4364` synchronously loads current files. A persistent kind hint duplicates available document metadata and can become stale. Remove it/filter; use dispatch/cheap JSON projection, with no scene/profile analysis on refresh or load. Missing cards remain nonmodal. Sections 6.4/7; Q29 explicitly asserts no analyzer/provider enumeration calls.

### F4 - ACCEPT

The original methods document grants broad DLL use with credit. Correct the historical record and plan an acknowledgement. Do not infer that its wording unambiguously licenses recovered C#, and no legacy implementation is selected for reuse. Section 3.2 supplies the precise local evidence; distribution notices retain separate dependency licenses.

### F5 - ACCEPT IN PART

Accept nonrecursive model-directory enumeration, exact `Game` token limitation, and the header/path patterns as useful hypotheses. Reject the stronger inference that offset 54 proves authentic tested session headers/versions or correct absolute-path behavior: no corresponding session corpus accompanies the recovery. The arithmetic is consistent, not unique provenance. Section 3.3 feeds these leads into E1/E2 without promoting them to verified SFM rules.

### F6 - ACCEPT IN PART

Warnings-as-errors, existing NuGetAudit=false commands and generator dependency are verified risks. Make package asset-flow/warning qualification explicit; keep reproducible restore policy and add a separate required current security audit. A security advisory blocking release is not itself a defect to suppress. A prerelease's existence does not prove a specific API break or justify adoption. Section 8.1/E6/Q39; no global warning suppression or floating packages.

### F7 - ACCEPT

Absolute path portability is a product-viability gate, not merely another supported-format detail. E1 is first and requires unchanged DMX on a different root with donor paths absent. Define breadcrumb, proven portable engine mapping and genuine nonportable outcomes. If ordinary sessions need rewriting, stop for product review under the no-rewrite invariant instead of coding a universal prefix-stripper. Sections 1/8.2/9.

### F8 - ACCEPT IN PART

Replace the minimal-session gate with an inventory and held-out real-session corpus, including authoring metadata/rigs/constraints/bins/large animation. Primitive values need no literal-path rule but can still control resource selection. Unknown strings on proven nonloading classes can be non-blocking; unknown class names alone are not proof. Datamodel.NET fallback is library behavior, not an SFM engine guarantee. Sections 4.2/8.2/E2 explicitly constrain completeness without either blanket rejection or blanket acceptance.

### F9 - ACCEPT IN PART

Add editable-only omission decisions and reports when runtime/load independence is established. Do not assume every bin or disabled track is unused: retained DMX may load it, and another timeline occurrence can require it. E2 provides reachability/load evidence; unknown or required edges still block. Scope becomes supported timeline fidelity with separately disclosed editable omissions, never an unqualified full-session promise. Sections 1.1/4.2/7; Q04.

### F10 - DEFER TO EVIDENCE GATE

No independent fixture/runtime evidence here establishes that SFM always embeds sufficient particle definitions. Remove Revision 1's blanket particle/PCF blocker and classify per E3. Test PCF removal, embedded operators and external material/model dependencies. Confirmed embedded structures become typed extraction work; true unsupported active loaders remain errors. Section 8.4/Q10. No unconditional external PCF subsystem is scheduled.

### F11 - DEFER TO EVIDENCE GATE

The old WAV-only path and audit's recollection do not prove current playback uses stored concrete waves. E3 removes wave and event script independently. If the wave suffices, event metadata triggers no registration work; otherwise bound any genuine loader requirement separately. Session clips and BSP entity events must not be conflated. Sections 1.2/8.4/Q11; named-event machinery is removed from the default plan pending evidence.

### F12 - DEFER TO EVIDENCE GATE

Baked VCD and absence of external-video slots are unverified hypotheses. E3 tests representative imported and genuinely external cases. Do not block on `.vcd`/video strings alone, and do not claim universal absence from one corpus sample. Qualified provenance is ignored for dependency purposes; actual unsupported runtime slots remain visible. Section 8.4/Q12; no unconditional VCD/video parser.

### F13 - ACCEPT IN PART

Add narrow KV adapter, explicit profile conditions and a real SFM-accepted malformed-material corpus. Upstream KV options independently confirm host-derived defaults; broad claims about community-file tolerance are not runtime-proven here. Implement only evidence-backed bounded leniency, not general error swallowing; inability to analyze accepted syntax remains an honest unsupported-analysis result. Pin packages rather than infer instability from release frequency. Section 8.1/E6/Q08.

### F14 - ACCEPT IN PART

Constrained lookup of unknown string values may add probable VTF/VMT edges with reasons. Extra bytes are not automatically harmless: rights, size, collisions and further dependencies matter. Probable matches cannot certify arbitrary shaders/proxies or make unmatched active loading behavior safe. Section 8.3 applies ordinary traversal and review to candidates and preserves required completeness rules.

### F15 - DEFER TO EVIDENCE GATE

The isolated-folder precedence promise was not verified. Remove it as guaranteed installation guidance; E4 measures supported mount ordering above usermod and impacts on existing scenes. Enable that procedure only when proven; otherwise use explicit conflict-reviewed installation or report unsupported recipient arrangement. Sender simulation alone does not solve this. Sections 1.4/9/Q16/Q35.

### F16 - ACCEPT IN PART

Replace the missing/expensive full stock catalog deliverable with an explicit Stage 0/2 provider-baseline workflow, grouped base prerequisite and MDL/BSP census. However VerifyFileChecksums/self-consistency does not authenticate Valve stock. Require independent clean provider/chunk baselines; loose files are unverified, not presumed authored custom. Continue parsing needed excluded-parent edges. Sections 4.5/E5/Stage 2/Q18-Q19; measure usability and hashing cost.

### F17 - ACCEPT

Sender simulation cannot inspect actual recipient files or protect other scenes. Add read-only manifest consumer for pre-install collision/proposed-winner checking and post-install actual-winner verification; require it before public release. Include hashes/sizes for all non-self package entries and needed prerequisite identities, with explicit manifest self-exclusion. No blanket replacement prose or automatic installer. Sections 1.4/5.5/7; Stages 5-6/9 and Q34-Q35.

### F18 - ACCEPT IN PART

Same-named sessions are a real destination-collision risk, but a generic-name warning is incomplete. Default to a stable package namespace when session-relative semantics are qualified, then check every actual recipient destination regardless of its name. If relocation is unsafe, retain the proven path and disclose conflicts. No internal DMX rewrite or claim that a namespace solves asset-path collisions. Sections 1.3/5.5/Q33.

### F19 - DEFER TO EVIDENCE GATE

PB's long-path manifest does not establish SFM's limit; an assumed MAX_PATH budget plus default install prefix would be an invented guarantee. E7 tests engine/extractor boundaries using actual prefixes and multibyte names. Add measured profile warnings only after evidence. Recipient verification can then evaluate its chosen root; no Model destination-policy change now. Section 8.6/Q36.

### F20 - DEFER TO EVIDENCE GATE

Autosave location/name/freshness semantics were not independently established by primary source or runtime in this review. Retain a general saved-DMX-only notice now. E7 can qualify a non-blocking newer-autosave hint; never package another file automatically or treat a timestamp as proof of unsaved changes. Section 8.6/Q36.

### F21 - ACCEPT IN PART

The out-of-process host is **not justified as a mandatory design before Stage 0 evidence**. Remove executable/IPC/Job Objects and guessed limits; default to bounded background in-process analysis after E6. Do not accept "own files" or lazy loading as safety proofs: current upstream code still allocates count-driven tables, and deferred failures occur during traversal. If qualification cannot bound risk, stop and seek a measured isolation/parser decision before shipping. Section 5.4/E6/Q28.

### F22 - ACCEPT

Put Scene analysis/parsers/packages in a separate Scene assembly referencing Core, with no reverse reference and narrow public contracts. WinForms composes services; it does not orchestrate dependencies itself. Verify generator assets and runtime dependencies rather than assuming the assembly split alone contains them. Licenses remain per component. Sections 5.1/5.3/8.1; Stage 1.

### F23 - ACCEPT IN PART

Group redistribution intent by provider/root rather than per file digest, avoiding legal reconfirmation on ordinary covered texture edits. Do not weaken exact-byte prerequisite verification, changed-membership review or revision-bound build consent. Permission scope and observed content identity are different concerns. Sections 4.5/6.2; Q19/Q25.

### F24 - ACCEPT IN PART

Remove initial recent filter/kind persistence, Scene New From Current and Scene accumulated changelog. Retain current Scene version/note and separate build history, existing Model history untouched; no ReleaseHistoryOperations extraction. Named-event work remains conditional on F11 evidence, not permanently rejected before testing. Sections 6/7/12 and Stage 7.

### F25 - ACCEPT

`ArchiveVerifier.VerifyAsync` runs normalized `archive.Entries.SingleOrDefault` for every staged file: quadratic lookup work is directly verified. Specify one normalized index, explicit duplicate rejection and unchanged byte verification/cancellation semantics, with large-entry tests. Do not promise wholly unchanged implementation or mix optimization into mechanical extraction. Section 5.4/Stage 4B/Q27.

### F26 - ACCEPT

Move Scene schema/persistence/settings/recents work after the working runtime slice and recipient decisions stabilize (Stage 7). Early stages need only in-memory Scene contracts. With Model unchanged, even full file dispatch can wait until then; a speculative early serializer skeleton brings no user value. Section 6/Stages 1,5-7; no migrations for unreleased speculative schemas.

### F27 - ACCEPT

Split extraction from scene implementation. Stage 4A is Model-only mechanical common-executor/validator/finalizer work under parity. Stage 4B separately adapts content and verification. Stage 5 adds the Scene planner/service. The archive optimization is independently reviewable, not hidden in extraction. Section 10 defines untouched areas and gates for each.

### F28 - ACCEPT

Stage 0 may use throwaway inventory/fuzz/runtime-oracle tooling outside the product tree when authorized, with versioned evidence and privacy/rights controls. It creates no product Scanner dependency and need not be shipped. This reconciliation still performs documentation only; it does not begin that tooling or a prototype. Section 9/Stage 0.

### F29 - ACCEPT

Replace subjective runtime success with predetermined file-winner/shot/time/audio/visual assertions, console evidence and clean-recipient negative controls. Confirm the actual logging mechanism before relying on `-condebug`; baseline unrelated warnings rather than requiring imaginary zero-noise logs. Stock-only and precedence fixtures need appropriate different negative controls. Section 11/Q38; Stages 0,5-6,10.

## Appendix B. Primary Sources and Evidence Limits

Local source references above are the audit basis; hyperlinks below support upstream claims only. Moving branches must be pinned to the adopted package/source identity during E6. New evidence was not inferred from search snippets or forum recollections. F10-F12/F19-F20 remain gates after research, not verified rules.

[S-FS]: https://github.com/ValveSoftware/source-sdk-2013/blob/master/src/public/filesystem.h
[S-GAMEINFO]: https://github.com/ValveSoftware/source-sdk-2013/blob/master/src/public/filesystem_init.cpp
[S-STUDIO]: https://github.com/ValveSoftware/source-sdk-2013/blob/master/src/public/studio.h
[S-BSP]: https://github.com/ValveSoftware/source-sdk-2013/blob/master/src/public/bspfile.h
[S-DMX]: https://www.nuget.org/packages/KeyValues2/1.0.0
[S-DMXCODE]: https://raw.githubusercontent.com/ValveResourceFormat/Datamodel.NET/master/Datamodel.NET/Codecs/Binary.cs
[S-KVOPTIONS]: https://raw.githubusercontent.com/ValveResourceFormat/ValveKeyValue/master/ValveKeyValue/ValveKeyValue/KVSerializerOptions.cs
[S-KV]: https://github.com/ValveSoftware/source-sdk-2013/blob/master/src/tier1/KeyValues.cpp
[S-VPK]: https://github.com/ValveResourceFormat/ValvePak/blob/master/README.md
[S-NUGET]: https://learn.microsoft.com/en-us/nuget/consume-packages/package-references-in-project-files
[S-AUDIT]: https://learn.microsoft.com/en-us/nuget/concepts/auditing-packages

- [Source filesystem interface][S-FS] and [gameinfo loader][S-GAMEINFO]: ordered provider/path-ID concepts; not a substitute for SFM mount experiments.
- [Studio structures][S-STUDIO] and [BSP structures][S-BSP]: dependency-bearing structures and branch versions, not guaranteed SFM layouts.
- [KeyValues2 1.0.0 metadata][S-DMX]: framework/generator/lazy-loading claims. [Current binary codec][S-DMXCODE]: deferred traversal and count-driven allocations; current master is not a verified pin.
- [ValveKeyValue options][S-KVOPTIONS] and [Valve KeyValues source][S-KV]: explicit options and engine error paths; runtime leniency still fixture-qualified.
- [ValvePak documentation][S-VPK]: member/chunk access and checksum methods; integrity is distinct from external stock provenance.
- [NuGet asset controls][S-NUGET] and [NuGet security auditing][S-AUDIT]: basis for explicit package propagation and separate deterministic-build/security gates.

**Review status:** F1-F29 reconciled; no application implementation begun. Revision 2 is the candidate implementation authority only after review and approval.
