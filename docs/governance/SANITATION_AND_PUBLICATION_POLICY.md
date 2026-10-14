# Sanitation and Publication Policy

Status: **OPERATING BASELINE REVISION 2**

Private development material is **not publication-safe by default**. Publication approval is fail-closed and binds to exact reviewed content. Changed bytes invalidate prior sanitation approval.

## Prohibited public material

Do not publish private personal/legal identities not expressly approved as public project identities; unrelated private organization identities; passwords/API keys/tokens/credentials/cookies/authentication data; private account or machine identifiers; private absolute filesystem paths/local topology; raw private logs/evidence receipts/registry/environment captures/screenshots/session names/fixture metadata/owner content; private history backups; proprietary recovered/decompiled implementation content unless separately authorized; or third-party assets without redistribution rights.

`ChadChan3D` and `ChadChanSFM` are approved public project identities. Email addresses are publishable only when explicitly designated by the Owner as public project addresses.

## Exact publication gate

Before **commit**:
1. stage only an explicit allowlist of intended paths; avoid broad staging;
2. identify the exact staged Git tree/index content and proposed commit metadata;
3. inspect exact staged diff/bytes, not merely working-tree files;
4. scan the staged candidate with private prohibited-identity terms, secret/credential checks, private-path/topology checks, and machine/account-identifier checks;
5. perform provenance/licensing/redistribution review for newly exposed third-party or recovered material;
6. record privately scanner/tool identity, scope, configuration, results, and known limitations;
7. bind sanitation approval to exact reviewed staged-tree identity and metadata.

Missing, failed, incomplete, or ambiguous required checks = **HOLD**.

Before **push/publication/release**:
1. identify every outgoing commit not already public on the target remote;
2. inspect all newly exposed history, commit metadata, trees, tags, release notes, archives, packaged assets, and release attachments;
3. repeat/extend sanitation and rights checks over the actual outgoing/publication set;
4. bind Owner approval to exact outgoing commit/tag/release identities.

If staged/outgoing bytes or metadata change after approval, repeat the gate. Keyword scans are one control, not proof of safety.
