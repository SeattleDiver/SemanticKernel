# Day 26 — Evaluator

## What this project builds

A small console app that implements the **LLM-as-a-judge** pattern: a
`TargetAgent` (a simple customer-support bot) answers a fixed set of test
questions, and a separate `JudgeAgent` independently scores each answer
against a known "ground truth" using a rubric-driven prompt. For each test
case the program prints a pass/fail verdict, a numeric score, and the
judge's written reasoning. There is no UI or persistence — it is a single
run through an in-memory test suite, structured the same way a real
regression-testing harness for prompts or model changes would be.

## Why it matters in the series

Earlier days in this series build agents that talk *to* a user. This lesson
repurposes the exact same building block — a `Kernel` plus an
`IChatCompletionService` — to grade another agent's output instead of
conversing with anyone. It is the series' first look at automated,
criteria-based quality control for AI output, a concern that becomes
essential once agents are shipped and need to be regression-tested the way
any other code does.

## Core concepts taught

- **LLM-as-a-Judge** — a second AI agent whose entire job is to evaluate the
  first agent's output, not to converse with a user.
- **Rubric-driven prompting** — the judge's prompt encodes an explicit
  numeric rubric (5/3/1) and a concrete pass rule, turning a subjective "is
  this good?" judgment into a repeatable, criteria-based one.
- **Structured JSON output** — `GeminiPromptExecutionSettings.ResponseMimeType
  = "application/json"` constrains the judge's response to valid JSON that
  can be deserialized straight into a strongly-typed `EvaluationResult`.
- **Temperature as a reliability control** — the target agent uses a higher
  temperature (0.4) for natural answers, while the judge uses 0.0 to make
  scoring as deterministic as possible — the two settings are chosen
  deliberately for opposite roles.
- **Test cases as data** — a `TestCase` record and a small array of them
  model the seed of a "golden dataset," the same idea behind any automated
  test suite.
- **Defensive JSON deserialization** — the judge's response is not trusted
  blindly; a malformed or unexpected response is turned into a safe,
  explanatory failure result instead of being allowed to take down the run.

## Cleanup notes

- **Fixed:** `JudgeAgent.EvaluateAsync` called `JsonSerializer.Deserialize`
  on the judge's raw response with only a `?? new EvaluationResult { ... }`
  fallback. That fallback only catches the case where deserialization
  *succeeds* but returns `null` (e.g. the model literally returned the text
  `null`) — it does nothing for the far more likely failure mode of the
  model returning JSON that doesn't match the expected shape, is truncated,
  or is otherwise malformed, which makes `Deserialize` throw a
  `JsonException`. Uncaught, that exception would abort the whole test
  suite on whichever test case happened to trigger it, discarding every
  result gathered so far — a bad experience for a lesson whose entire point
  is a graceful, informative pass/fail report. Added a `try`/`catch
  (JsonException)` around the deserialization so a bad judge response now
  produces the same safe `Passed = false, Reasoning = "Failed to parse
  JSON"` result the code already intended, instead of crashing. The docx's
  Complete Code listing and Code Walkthrough (Step 6) were updated to match.
- **Left as enhancement opportunities, not defects:** the prior analysis
  (`missing.md`) also flagged the lack of a suite-level pass-rate summary, a
  non-zero exit code on failure, retry/resilience around the network calls
  to the model, repeat-run consistency checks for the judge, an external
  test-data file, `[KernelFunction]`-based structured output, and unit tests
  for the JSON fallback path. These are all reasonable production-hardening
  or scale-up ideas, but none of them block a student from building,
  running, and understanding this lesson as written, so they were
  intentionally left alone.
