# Scene Package Modern Prior-Art Findings

Date: 2026-10-11  
Status: Accepted research record. This document records sanitized technical findings; it is not a replacement Blueprint.

## Purpose

Scene Package research was broadened to study existing Source Filmmaker and Source-engine packaging implementations before continuing bespoke runtime instrumentation. The governing product goal remains: preserve a saved editable SFM scene and package the supported external resources another artist needs to open and use it correctly.

Raw binary-inspection notes, machine-local paths, scratch attempt records, and private runtime receipts are intentionally not published here.

## Convergent architecture

The strongest cross-implementation pattern is:

`saved DMX structural extraction -> Source-profile virtual filesystem -> typed recursive dependency closure -> Source-relative package`

This convergence is strong evidence for responsibility boundaries and implementation feasibility. It is not proof of complete SFM coverage, engine-equivalent provider precedence, or recipient portability.

## SFMPackager

Inspection of the distributed managed application established concrete structural traversal of SFM session concepts, including film clips, tracks, animation sets, models, material overrides, sounds, particles, projected lights, maps, sprites/overlays, and scene DAG relationships.

Its dependency machinery separates typed resource analysis from scene traversal and includes recursive handling for major Source resource families such as MDL, VMT, BSP, and PCF. Its virtual-filesystem layer includes physical/package providers and ordered lookup machinery.

The distributed packager copies the original DMX stream into its archive rather than requiring DMX reserialization. This supports the Package Builder design choice to keep the saved DMX immutable.

Important limits remain:

- its public application distribution is binary-first rather than an adoptable application-source implementation;
- its own documentation acknowledges absolute-path and other coverage gaps;
- provider classes do not by themselves prove SFM's exact implicit VPK discovery or precedence;
- a generated archive does not prove recipient usability or complete editable-scene coverage.

Use SFMPackager as the primary behavioral comparator and semantic-rule inventory, not as hidden production authority.

## SrcPack

SrcPack provides useful historical and edge-case evidence, but is a weaker production-architecture reference.

A prior hypothesis that its `StripAbsolutePath` behavior might solve absolute-path portability was rejected. Static inspection showed that behavior is used for missing-file reporting, not runtime resolution, DMX rewriting, or portability.

Its DMX extraction strategy is substantially more heuristic than the desired production reader, including string/extension-oriented paths. Treat SrcPack as a secondary comparator and practical edge-case source, not the structural decoder or provider-winner oracle.

## KeyValues2 / Datamodel.NET

Revision 2 had already selected KeyValues2 as a qualification candidate. The prior-art review strengthens the case for exercising that candidate early instead of building bespoke DMX decoding infrastructure.

Stable KeyValues2 1.0.0 remains the first candidate. Package version, assembly version, Git revision, and any DLL bundled by another application must remain distinct identities.

No production adoption follows merely from successful loading of one file; fidelity, malformed-input behavior, resource bounds, dependency propagation, notices, and integration behavior remain qualification work.

## srctools and other Source-format implementations

TeamSpen210/srctools provides mature, inspectable examples of Source search-path handling and recursive resource analysis. It is valuable as:

- a rule/reference source;
- an offline differential comparator;
- a source of selectively adaptable algorithms where licensing and branch assumptions are reviewed.

It is not an oracle for the shipped SFM build, and no Python runtime dependency is implied for Package Builder.

Related uSource implementations are useful for format and virtual-filesystem design study. Bundled binaries are not eligible for direct production adoption without exact provenance, source mapping, API, behavior, and reuse-rights qualification.

## What this research changes

The research does not overturn Revision 2's core production decomposition. It changes the evidence route:

- do saved-file decoding before general live-SFM observation;
- use prior-art semantic branches as hypotheses and seeds rather than rediscovering every resource route from scratch;
- compare real package outputs before building more generalized native observation infrastructure;
- reserve direct SFM experiments for claims that actually concern runtime resolution, recipient behavior, or editability.

## What remains open

The following remain empirical requirements:

- representative saved-session coverage, including retained/editable content outside the active timeline;
- SFM provider precedence and implicit package/container discovery;
- absolute and topology-sensitive saved references when such candidates are actually present;
- complete typed dependency closure for the supported profile;
- stock/provider provenance and rights decisions;
- recipient collision/winner verification;
- donor-independent recipient opening and supported edit/use behavior;
- parser and format-reader safety/resource qualification.

No existing packager is treated as ground truth. Known fixture construction, qualified format facts, and controlled SFM outcomes remain the basis for scored comparisons.
