# Playbook: Regression Investigation

1. **Define the expected vs actual** behavior precisely (status code, value, timing, message shape).
2. **Baseline diff:** compare against git history for the affected method(s): `git diff <good-commit>..origin/main -- <path>`,
   `git log -S<symbol>` to find when a symbol changed.
3. **History:** `git log -p` the suspect file (hotspots `SportsbookRepository.cs`, `BettingRepository.cs`,
   `PlatformRepository.cs`, `IntegrationController.cs`); identify the introducing commit.
4. **Contract drift:** if shapes changed, the cause may be a sibling (odds-engine-2/platform-web) —
   verify field/key/method alignment (R-2); the catalogue/odds may have changed upstream.
5. **Variant check:** confirm you're analyzing the live project, not a superseded duplicate (TD-07).
6. **Conclude:** what changed · where · when · why · confidence; flag for verification — **don't
   auto-fix** unless asked. Run [.claude/checklists/regression-review.md](../checklists/regression-review.md).
