# Authoritative Scene Package Design Pointer

Status: **OPERATING BASELINE REVISION 2**

Repository: `chadchan3d/sfm-package-builder`  
Accepted public branch: `main`  
Accepted public baseline commit for this transfer revision: `8503b0c3dc5e429424acb42f7b706809dd1de75f`

## Governing design authority

1. `docs/scene-package/SCENE_PACKAGE_INTEGRATION_BLUEPRINT_REVISION_2.md`
   - frozen base Blueprint;
   - SHA-256 recorded by Freeze Errata: `64DD9981B0C4A8B59D3B11DEB0C708FF7B59B8E933D28517B96214E50976015C`;
   - Git blob at accepted baseline: `3eb85679510c4e5ec333bbceb6eedccc3fc7a21b`.
2. `docs/scene-package/REVISION_2_FREEZE_ERRATA_AND_AUTHORITY_RECORD.md`
   - bounded authoritative errata;
   - SHA-256 recorded by project authority: `4C43CC7DDE185B52C506F0CE6BDC7060113D22BEA582DD2FA93294CDE3BF0099`;
   - Git blob at accepted baseline: `085499b2c3e88e95d82023732ee8b5859ea095b1`.
3. `docs/scene-package/decisions/MODERN_PRIOR_ART_ARCHITECTURE_DECISION.md`
   - later bounded architecture-authority decision;
   - Git blob at accepted baseline: `d0e6704e25a64d219dc7363be38ffbc180a7ff8c`.

## Precedence

Explicit later bounded architecture decisions govern only clauses they expressly supersede; Freeze Errata governs its stated corrections; Revision 2 governs all unaffected design.

`docs/scene-package/CURRENT_STATUS.md` is a sanitized public checkpoint, not a substitute for the live implementation Ledger.

## Change control

This pointer is a defining operating contract. A change to accepted authority identity, precedence, or governing documents requires Designer proposal, Astra review, independent Claude Opus adversarial review, and explicit Owner approval before adoption/commit.
