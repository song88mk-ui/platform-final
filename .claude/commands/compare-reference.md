# /compare-reference

Compare current code against the `/Reference` baseline snapshot to detect regressions/behavior changes.

## Rules
- `/Reference` (workspace sibling) is the **pre-AI baseline** — **read-only, never modify**.
- Prefer the **current** implementation unless explicitly comparing behavior.

## Steps
1. Locate the same file/area under `d:\Gimli\Repositories\Reference\...` and in platform-final.
2. Diff the relevant method(s); focus on behavior (status codes, branching, SQL/Mongo queries, message
   shapes), not formatting.
3. Classify each diff: intentional change · likely regression · refactor · unknown — with evidence.
4. For contract-shaped diffs (message fields, routing keys, DTOs), check cross-repo impact (R-2).

## Output
A behavior-diff report (current vs Reference) with confidence per item; flag suspected regressions for
verification (do not auto-"fix").
