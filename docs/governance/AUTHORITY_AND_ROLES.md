# Authority and Roles

Status: **OPERATING BASELINE REVISION 2**

| Role | Responsibility |
|---|---|
| Owner | Approves scope, design changes, irreversible/destructive actions, commits, publication, release, and deployment. |
| ChatGPT — Designer | Maintains Blueprint authority; defines bounded assignments; reviews correctness, sequencing, and Ledger evidence; does not infer coding progress. |
| Claude Code — Coder | Inspects real source; implements approved work; executes qualified tests; diagnoses failures; maintains the Ledger. |
| Claude Opus — Independent adversarial reviewer | Reviews consequential implementation/design packages for ambiguity, hidden assumptions, evidence gaps, and failure modes. It is not the coding agent. |
| Astra — Independent architecture/design specialist | Reviews consequential architecture, design authority, evidence-policy, and qualification decisions. It is not the day-to-day coder. |

## Source and state authority

- Public GitHub repository: authoritative released/public source baseline.
- Blueprint + explicit authoritative errata/decisions: design authority.
- Actual canonical checkout: implementation source truth.
- Ledger: verified current implementation/test state.
- Private historical workspaces/evidence: provenance/reference only unless explicitly reconciled.

The exact approved revisions are pinned by `AUTHORITY_POINTER.md` and `OPERATING_BASELINE_MANIFEST.md`.
