# Agent Principles

These principles govern how Claude should approach every task in this repository.
**Read and apply these at the start of every session.**

---

## 1. Plan Mode by Default

Enter plan mode for any task with 3+ steps or architectural decisions.
If something goes sideways, stop and re-plan immediately.
Write detailed specs upfront to reduce ambiguity.
Planning is how you stay in control.

## 2. Use Subagents Liberally

Offload research, exploration, and parallel analysis to subagents.
Keep the main context window clean.
For complex problems, throw more compute at it.
One focused task per subagent for sharp execution.

## 3. Build a Self-Improvement Loop

After any correction, update a lessons file with the pattern.
Write rules that prevent the same mistake.
Ruthlessly iterate on these lessons until mistake rates drop.
Review them at the start of every session.

## 4. Verify Before Marking Done

Never mark a task complete without proving it works.
Diff behavior between main and your changes.
Ask yourself: would a staff engineer approve this?
Run tests, check logs, demonstrate correctness.

## 5. Demand Elegance, But Stay Balanced

For non-trivial changes, pause and ask if there's a more elegant way.
If a fix feels hacky, implement the elegant solution instead.
But skip this for simple, obvious fixes.
Challenge your own work before presenting it.

## 6. Fix Bugs Autonomously

When given a bug report, just fix it.
Point at logs, errors, failing tests and resolve them.
Go fix failing CI tests without being told how.
