# Day 26 — Evaluator: Missing Pieces & Enhancement Opportunities

_Analysis date: 2026-08-04_

1. **No aggregate pass rate or suite-level summary** — The program prints a per-test verdict but never rolls the results up into a suite-level summary (e.g., "3/5 tests passed, average score 4.2"). For a lesson whose entire premise is automated evaluation, the absence of any final scorecard is a missed opportunity to show students what an evaluation report actually needs to look like to be useful in a CI pipeline. A future revision could accumulate `EvaluationResult`s into a list and print totals/averages after the loop.

2. **No non-zero exit code or CI-friendly signal on failure** — Even when every test fails, `Main` returns normally with exit code 0. A real evaluation gate (e.g., a build step that should block a deploy on regression) needs the process to fail loudly — `Environment.Exit(1)` or throwing when any test doesn't pass — which this lesson never demonstrates.

3. **No resilience around the two model calls per test case** — Neither `TargetAgent.AskQuestionAsync` nor `JudgeAgent.EvaluateAsync` has a try/catch, retry, or timeout around `GetChatMessageContentAsync`. A single transient network error or rate-limit response on any test case throws an unhandled exception and aborts the entire suite, losing results for every test case that hadn't run yet. This is especially relevant for an evaluation harness, which is exactly the kind of thing meant to run unattended and repeatedly.

4. **Judge is only ever asked to score once — no repeat-run consistency check** — LLM judges are known to be somewhat non-deterministic even at `Temperature = 0.0`. The lesson never demonstrates running the same evaluation multiple times to check score stability, which is a core concern real evaluation pipelines have to address (e.g., averaging over N judge calls, or flagging high-variance cases).

5. **Small, hardcoded, in-code test suite with no external data source** — The two `TestCase` entries are hardcoded directly in `Program.cs`. There's no mechanism to load a larger test set from a JSON/CSV file, meaning the "golden dataset" idea is only implied, not demonstrated. A revision could load `tests.json` from disk to make the suite trivially extensible without recompiling.

6. **Judge prompt has typos that could be cleaned up (`captial`, `deatils`, `trhuth`, "a string Quality Assurance Judge")** — These are carried over from the actual source and would be shipped to the model as-is. They don't functionally break the lesson (the model tolerates typos), but a polished revision should fix `"a string Quality Assurance Judge"` (missing word, reads oddly), `"key deatils"`, and `"ground trhuth"` for a cleaner reference implementation.

7. **No plugin/[KernelFunction] usage or tool-calling in the judge** — This lesson still relies entirely on prompt-based JSON output rather than showing students an alternative like Semantic Kernel's structured-output/function-calling features to enforce the schema at the SDK level rather than only via prompt instructions plus a runtime null-check. Given Day 28 introduces `[KernelFunction]`, a forward-reference or contrast here would tie the two lessons together.

8. **No unit tests exercising `JudgeAgent`'s JSON fallback path** — The defensive `?? new EvaluationResult { Passed = false, ... }` branch in `EvaluateAsync` is never exercised or demonstrated with a deliberately malformed response, so students don't get to see the safety net actually catch anything. A demo mode that forces a bad response (or a unit test with a mocked service) would make this defensive coding pattern concrete rather than theoretical.
