# Implementation Ledger Contract

Status: **OPERATING BASELINE REVISION 2**

The Ledger is the authoritative concise record of current implementation/test state. Claude Code maintains it; the Designer reviews it before the next bounded assignment.

Every checkpoint must contain:

1. **Repository identity:** repository, branch, exact HEAD, clean/dirty status, relevant uncommitted/untracked work, and a content identity for relevant dirty/untracked inputs (per-file hashes or immutable private patch/snapshot identity).
2. **Design/operating authority:** accepted repository baseline commit, exact Blueprint/errata/decision blob identities, exact operating-contract revision, and any approved design change not yet incorporated.
3. **Current milestone:** assignment, completion criterion, and status (`NOT STARTED`, `ACTIVE`, `PASS`, `HOLD`, `FAIL`, `DESIGN REVIEW REQUIRED`).
4. **Implemented state:** exact files materially changed and concise behavior implemented; untested code is not qualified.
5. **Evidence:** commands/tests/inspection actually performed; outcomes including failures/skips; relevant toolchain/runtime identity; exact source/dirty-state identity tested; durable evidence/receipt identity. Public Ledger entries use opaque evidence IDs where raw private details would leak.
6. **Failures/uncertainty:** failure/HOLD conditions, untested behavior, limitations, contradictory evidence, and durable references for every required failed/HOLD result.
7. **Sanitation/publication:** private-only material status, exact reviewed publication-candidate identity when applicable, and commit/push authorization status.
8. **Transfer admission:** accepted baseline branch/commit and checkout state, approved operating-contract revision, build/test capability actually verified, optional-test gaps/skips, outstanding private operational ownership/HOLDs by opaque evidence ID, and transfer-admission status.
9. **Next bounded action:** exactly one smallest next action unless an approved milestone contains an ordered sequence.

## Rules

Compact/replace stale current-status prose without deleting historical results or durable evidence references. Do not convert inference into verified state, runtime claims from static inspection, or remote/commit claims without Git proof. Historical evidence remains historical unless applicability is explicitly admitted. Required receipts/failures/admission decisions must have verified durable private retention before scratch cleanup.
