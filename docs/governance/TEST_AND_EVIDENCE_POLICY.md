# Test and Evidence Authority Rules

Status: **OPERATING BASELINE REVISION 2**

Keep static inspection, automated tests, qualified runtime experiments, owner/operator observations, third-party/reference behavior, and inference/hypothesis distinct.

A result supports only the claim its controls and observations distinguish. Record subject identity, relevant environment/profile, method, controls, observed result, applicable source/dirty-state identity, relevant toolchain/runtime identity, outcomes/skips, and limitations.

Preserve every required failed and HOLD result and its durable evidence reference. Ledger compaction may replace stale current-status prose but may not delete or obscure the historical result/evidence identity. Do not overwrite failed receipts, silently relax acceptance criteria, repair protected owner state without authority, or call inconclusive evidence a product failure.

Historical evidence may be reused only when applicability is established under governing design authority; reuse never becomes a new execution.

Before native/runtime transitions define owned files/directories, protected owner state, expected writes, restoration/recovery behavior, and STOP/HOLD conditions.

Raw private receipts may remain outside Git, but required evidence must be copied to durable private storage and verified before scratch cleanup. Public records use opaque evidence IDs where necessary.
