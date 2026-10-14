# Workspace Policy

Status: **OPERATING BASELINE REVISION 2**

The canonical development checkout is the durable Git working tree created from the accepted public repository commit. A separate high-volume scratch workspace may hold builds, fixtures, temporary staging, generated packages, caches, extracted comparison material, large runtime/evidence artifacts, and experimental tooling.

Scratch is disposable **only after required evidence has been durably retained elsewhere**.

Before deleting/recycling scratch content, verify a durable private copy of every required receipt, failed/HOLD result, admission/reuse decision, unique runtime observation, ownership/recovery record, and evidence needed to interpret a Ledger claim. The durable copy must have a stable content identity and be referenced by opaque evidence ID where needed.

No authoritative source, unique Blueprint change, Ledger checkpoint, or required evidence may exist only in scratch at cleanup time. Promotion from scratch into the repository requires explicit provenance and sanitation review.
