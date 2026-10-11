# Step A: KeyValues2 1.0.0 Existing-File Assessment

Date: 2026-10-11  
Status: Accepted completed parser assessment. Sanitized public evidence record.

## Result

Exact stable KeyValues2 1.0.0 successfully loaded an existing genuine saved SFM session with header:

`<!-- dmx encoding binary 5 format sfm_session 20 -->`

The accepted input was 9,281 bytes with SHA-256:

`FDBFB248E5B48CA73E3682E5E022058AC99F2B3BD928F24C0E3A6019E8F4BE19`

The original and isolated working copy matched before and after inspection. No Save, serialization, conversion, mutation, SFM action, packager execution, or repository change occurred.

## Exact decoder artifacts

The final assessment used manually supplied official NuGet packages through an isolated offline restore:

| Package | Version | SHA-256 |
|---|---:|---|
| KeyValues2 | 1.0.0 | `8AE3A569692FEA6A1B974FAB1EE3716BA9554E49248D990A520EF0743A197FA2` |
| KeyValues2.ElementFactoryGenerator | 0.2.1 | `DEB0800D8EA14C2EF22CB594FCDF1C34A25A9D9E5638E909CF1AA0B12868F8B1` |

The loaded library assembly reports `Datamodel.NET, Version=1.2.0.0`; its DLL SHA-256 is:

`FBE391610B8A12C3845D90FFE96CBF72A4F4178A95FE5747096E105B555D9C59`

Package version, assembly version, and package-declared source revision are separate identities.

## Structural parse

The generic/untyped decoder loaded the file and all reported attributes/collection values were accessed before disposal.

Observed graph totals:

- 58 elements;
- 58 distinct persisted element IDs;
- 23 element classes;
- 395 attributes;
- 299 inspected collection items;
- 76 non-null graph-reference occurrences;
- no dangling non-null reference target observed;
- zero final decode/access errors.

The decoded structure included a coherent saved path from the session's active film through the film track and shot to an animation set and game model. The scene DAG also referenced the same model element.

This is coherent structural evidence from one decoder. It is not independent fidelity corroboration.

## Resource-bearing candidates observed

The saved decode contained these relevant structured values:

| SFM structure | Attribute | Raw decoded value | Current interpretation |
|---|---|---|---|
| `DmeGameModel` | `modelName` | `models/e1_287c8296/probe.mdl` | Relative saved model occurrence |
| active `DmeFilmClip` | `mapname` | `stage.bsp` | Relative saved map-name candidate |
| `DmePresetGroupInfo` | `filenameBase` | `models\e1_287c8296\probe` | Model-associated saved value; runtime vs authoring-only role unresolved |

The custom model is therefore actually present in the saved session data rather than inferred from a screenshot or raw suffix scan.

No nonempty direct material-resource occurrence, projected-light resource, sound-resource clip, or particle-resource element was observed in this particular input. Sound tracks existed with no child clips.

All observed path-like candidates in this file were relative. This is input-specific and does not settle absolute/topology-sensitive behavior for other sessions.

## Fidelity boundary

Independent structural fidelity remains unverified.

No already-available independent decoder was suitable for a quick structural cross-check, and repeated loading through the same library was not counted as independent evidence.

A small selected-tuple gate remains for later work where scored comparison depends on exact associations, especially:

- active film / shot relationship;
- shot / animation-set / game-model relationship;
- `modelName`;
- `mapname`;
- `filenameBase` only if its role is scored.

Failure to obtain an independent check does not erase the useful parse inventory, but semantic claims that depend on an unchecked association must retain that limitation.

## What Step A establishes

Step A establishes:

- exact KeyValues2 1.0.0 can parse this genuine binary-5 / sfm_session-20 file;
- the saved file contains a structurally associated custom model occurrence and map-name candidate;
- reported graph/value access completed without a final decoder/access error;
- the input bytes remained unchanged.

Step A does **not** establish:

- production adoption of KeyValues2;
- complete DMX corpus fidelity;
- parser safety/resource qualification;
- SFM provider precedence;
- runtime necessity of every saved string;
- complete model/material/map closure;
- recipient portability;
- E1, E2, E4, or E6 completion.

## Comparison readiness

This saved scene is useful for the first bounded model/material comparison, but was not ready for a scored comparison at the end of Step A because provider identities and the expected transitive dependency closure had not yet been established.

The next evidence task is therefore to construct an independent expected-closure/provider dossier for this exact saved scene before running any packager.
