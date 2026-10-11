# Scene Package Modern Prior-Art Architecture Decision

Date: 2026-10-11  
Status: Architecture-authority decision, sanitized for the public repository.

## Decision

Retain Revision 2's production architecture, approve parser-first evidence work, and defer the frozen E1 runtime campaign pending input-driven protocol review.

Revision 2 and the Freeze Errata remain authoritative except for the evidence-order and scope refinements recorded here. This decision does not declare E1 passed, does not approve a smaller replacement run count, and does not make successful parser loading a proxy for recipient portability.

## Governing product

Scene Package is intended to preserve a saved editable SFM scene and package the supported external resources required for another artist to open and use it correctly.

A rendered current shot is not sufficient evidence of full editable-scene support. Supported retained clips, alternate shots, bins, and other saved editable content must be considered when their resources are part of the supported profile.

## Production responsibility chain

`immutable saved DMX -> structural decoding -> SFM semantic occurrences -> ordered Source resolution and typed dependency closure -> Scene policy/final package plan -> shared staging/ZIP verification -> recipient verification`

Responsibilities remain separated:

- the DMX reader exposes typed structure and references but does not decide dependency semantics;
- the SFM semantic extractor identifies resource-bearing occurrences and their context;
- one SFM-profiled resolver owns provider selection, shadows, and lookup trace;
- typed Source analyzers emit outgoing resource requests without choosing physical winners independently;
- the closure coordinator preserves reasons, cycles, context, and unresolved descendants;
- Scene policy owns completeness, rights, prerequisites, conflicts, and the final package plan;
- Core executes the checked plan through shared staging/archive machinery after the planned Model-parity extraction;
- recipient verification checks content, prerequisites, conflicts, and expected lookup winners without editing mounts automatically.

Keep the assembly direction `Scene -> Core`, with UI composition above both workflows. Do not turn every responsibility into a new framework or assembly.

## Decisions retained

The following remain in force:

- Model Package behavior and legacy recipe/settings compatibility remain unchanged.
- Scene DMX bytes remain unchanged; no source rewriting or automatic resave is introduced.
- Occurrence identity, virtual resource identity, provider identity, and content identity remain distinct.
- One final plan remains the sole preview/build content authority.
- Source-relative destinations, collision rejection, exact-byte archive verification, stale-input checks, and explicit gaps remain required.
- Stock decisions require provider-level evidence; folder names alone do not establish stock provenance.
- Recipient conflict and installed-winner checks remain necessary.
- Stage 4A mechanical extraction remains separate from later content-neutral adaptations and Scene behavior.
- No automatic gameinfo editing, arbitrary drive search, or compiler dependency is introduced.

## Evidence-order changes

The former universal observer-first progression is superseded as a scheduling requirement:

- P2 live-observer work is parked and is not a prerequisite.
- P4 remains paused; preserved evidence may be reused, but marker repair is not required merely to continue saved-file analysis.
- Serialized saved-DMX inspection is brought forward as a separately scoped evidence task.
- The universal P6 rehearsal/18-row validator is deferred; new tasks must validate the controls required for their own claims.
- The frozen E1 campaign is deferred, not deleted or passed. Any replacement runtime protocol requires explicit claim/control review before execution.

Ordinary recipient transfer remains a required product fact. Focused absolute/topology-sensitive tests are triggered by actual saved candidates rather than assumed universally.

## Component reuse decisions

- **KeyValues2 1.0.0:** first scratch DMX candidate and retained production candidate, subject to qualification.
- **ValveKeyValue / ValvePak:** retained candidates behind narrow adapters; they do not define SFM precedence.
- **Existing Package Builder MDL reader:** reuse already-qualified v49 facts where sufficient; do not pretend it provides complete model closure.
- **srctools:** source/reference and offline comparator; selective licensed adaptation only after review.
- **SFMPackager:** primary black-box comparator and semantic-rule inventory; not a hidden backend or code-transplant source.
- **SrcPack:** secondary comparator and edge-case inventory.
- **uSource-related binaries/source:** study only until provenance, rights, source mapping, and behavior are qualified.
- **Bespoke DMX decoder, generic prefix stripping, universal live observer:** removed from the default route unless a later named gap specifically requires a narrow diagnostic.

## Runtime evidence still required

Direct SFM evidence remains necessary for claims about:

- donor-independent recipient opening and supported edit/use behavior;
- actual provider winners and precedence;
- absolute/topology-sensitive occurrences;
- retained/omitted editable content behavior;
- resource roles whose necessity is ambiguous;
- installation/session relocation safety;
- direct sound behavior when audio support is qualified.

Use the smallest observation that distinguishes the alternatives. Parser failures, wrong mounts, or inconclusive observations are holds, not proof of nonportability.

## Immediate consequence

The next evidence route is:

1. qualify saved-file structural decoding;
2. establish independent expected dependency/provider truth for a suitable saved scene;
3. compare existing packager output against that known truth;
4. perform a controlled recipient witness;
5. investigate only observed disagreements and remaining runtime gaps;
6. make component/adoption decisions;
7. begin bounded Scene Package product integration under the retained Revision 2 boundaries.

No production code change is authorized by this decision alone.
