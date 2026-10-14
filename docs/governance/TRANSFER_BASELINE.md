# Claude Code Transfer Baseline

Status: **OPERATING BASELINE REVISION 2**

Repository: `chadchan3d/sfm-package-builder`

Original published v1.0.4 commit: `e3caabce45192419e97dd9c6b9ece68fad2cf246`  
Accepted transfer baseline commit: `8503b0c3dc5e429424acb42f7b706809dd1de75f`  
Accepted branch: `main`

## Remote movement rule

Migration must observe current remote `main` HEAD but must **not automatically adopt a newer commit**. If remote `main` differs, record the observed HEAD, report the delta, and obtain explicit Designer/Owner acceptance before changing the transfer baseline.

## Checkout safety

Never reset, overwrite, clean, or replace an existing destination merely to make it match the accepted baseline. If the intended destination exists, inspect it read-only first and record branch, HEAD, dirty/untracked state, and remotes. HOLD on unexpected state.

## Development provenance

Historical construction/publication workspaces are private provenance/reference sources, not replacements for GitHub authority. The canonical checkout must be created or admitted from accepted Git authority, then compared read-only with private provenance.

## Migration completion criterion

Before Claude Code becomes active coder: canonical checkout identity/dirty state are recorded; accepted baseline is satisfied; approved operating-contract revision is pinned; Model Package baseline build/test capability is verified; optional/conditional gaps are recorded; required private development aids are identified without silent promotion into Git; outstanding operational ownership/HOLDs are recorded privately and in the initial Ledger by opaque ID; transfer admission is `PASS`.
