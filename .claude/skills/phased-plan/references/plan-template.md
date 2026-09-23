# Plan template

Fill this in. Delete the notes in `<>`. Keep the section order: an orchestrator and its sub-agents
look for these headings by name.

The three blocks under **Orchestration protocol** are meant to be copied close to verbatim. They are
the part a fresh session reads to know how to run the plan without you there to explain it.

---

```markdown
# <What is being built>

Date: <DD-MM-YYYY>. <Target and version, e.g. "Game: Valheim 1.0" or "Node 22, Postgres 16">.
Written to be run phase by phase by an orchestrator.

## Context

<What exists today. What is missing. Why this is worth doing. Three short paragraphs at most.
Name the files that hold the current behaviour, so a sub-agent can find them.>

---

## Decisions (locked, from the user)

| | |
|---|---|
| <Question that came up> | <What the user chose> |
| <Thing explicitly out of scope> | <Why, in a few words> |

<Then: which existing project rules still apply, by name.>

### <The cross-cutting rule>, in full

<State the constraint that applies to every phase, completely, with its edges. Then give it a
failure signature: "If a phase ever looks like it needs X, that is a sign the design went wrong.
Stop and report instead of doing X.">

---

## What ships

<Be concrete. A sketch of the screen, the command and its output, the shape of the file, the
endpoint and a sample response. A reader should be able to picture the finished thing.>

<Then say what is deliberately left out, so nobody adds it.>

---

## Architecture

### New files

<A tree, with one line per file saying what is in it. Name the files you want. Do not describe
them and leave the naming to the sub-agent, or two phases will invent two names for one thing.>

### Where every piece of data comes from

| What we need | Source | Cost |
|---|---|---|
| <thing> | <the exact call or file> | <cheap, or the expensive bit> |

### <Key mechanism>

<The one or two mechanisms the whole design rests on, explained in enough detail that a sub-agent
does not have to invent them. Numbered steps are good here.>

---

## Phase map

| # | Phase | Test scenario | Roughly |
|---|---|---|---|
| 0 | Foundations and a probe | `probe` | small |
| 1 | <...> | `<test name>` | medium |
| N | Optional: <...> | `<test name>` | medium |

<One line on which phases are the heart of it, and which can be dropped.>

---

## Orchestration protocol

**The orchestrator never reads source files.** It reads this plan, the last handoff entry, and each
sub-agent's report. That is what keeps its context small.

The handoff log lives at `<path, outside shipped code, e.g. .claude/handoff/<name>.md>`. Phase 0
creates it.

### Loop

1. Read the handoff log. Find the first phase not marked done.
2. Fire **one** sub-agent with the prompt template below, filled in from that phase's section.
3. Read the sub-agent's report, which is at most 15 lines.
4. Run the build yourself. Commit the phase. If the report says blocked, show the block to the user
   and stop.

### Sub-agent prompt template

```
You are implementing Phase <N> of <the thing> for <the project>.
Working directory: <absolute path>

FIRST, read these, in this order, and nothing else unless you need it:
  1. <the project's agent guide>     (rules, build, run, verify, traps)
  2. <handoff log path>              (what earlier phases built and decided)
  3. <the phase's "Reads" list>

THEN do only what Phase <N> says. Do not start the next phase.
<paste the phase's Goal, Build, Key APIs, Done-when and Watch-out-for sections verbatim>

CONTEXT FROM EARLIER PHASES (do not re-measure, it is in <probe output path>):
<the measured facts this phase needs, and any open point that belongs to a different phase
so this agent does not take it on>

RULES
- <the cross-cutting rule, in full, every time>
- Follow <the agent guide>'s writing style in any comment, doc or commit message.
- <the project's file and naming conventions>
- <what must be logged through, and what must never be used>
- <any file that must stay free of extra dependencies, and why>
- Do not change anything in <out-of-scope folder>. Read it only where "Reads" says to.
- Never report "should work". Build it and run it.

VERIFY (all must pass)
  <build command>                    -> 0 errors, 0 new warnings
  <test command> <scenario>          -> prints "<the exact success line>"
  <re-run the two or three earlier scenarios most likely to break>
  <any screenshot check: read the image yourself and say what you see>
If the test fails twice for the same reason, stop and report. Do not widen the scope.

FINISH BY
1. Appending to <handoff log path>, at most 40 lines:
     ## Phase <N> done  (<date>)
     Files added/changed: ...
     Public API the next phase will call: ...
     Decisions made: ...
     Gotchas found: ...
2. Replying with at most 15 lines: what works now, the test line, anything left open.
```

### Rules the orchestrator enforces

- One phase at a time, never two sub-agents at once. Phases build on each other.
- Every phase leaves the repo **building and green**. A phase is not done with a red test.
- No phase changes a file another phase owns unless its section says so.
- Commit after each green phase, message `<prefix>: phase <N>, <short title>`.

---

## Phases

### Phase 0, foundations and a probe

**Goal.** Make the project able to compile what the later phases need, and replace every guess in
this plan with a measured fact before any real work starts.

**Reads.** <build files, the test harness>

**Build.**
- <references, dependencies, permissions the later phases need>
- New test scenario `probe` writing `<plain text output path>`:

| Probe | Why |
|---|---|
| <the thing you guessed> | <which phase would break if the guess is wrong> |
| <a real count> | <what is sized off it> |
| <a timing of something you assumed was fast> | <what gets redesigned if it is slow> |
| <does the one risky mechanism work at all> | <what is built on top of it> |

**Done when.** `<test command> probe` passes, the output has every line above, and the handoff
records the measured numbers and anything that will need a fallback.

---

### Phase <N>, <name>

**Goal.** <One or two sentences. What is true after this phase that was not before.>

**Reads.** <exact files. If another repo is the spec, name the exact files in it.>

**Build.**
- `<path/to/file.ext>`: <what it holds>
- <behaviour, with the constants written out:>

  | | |
  |---|---|
  | <constant> | <value> |

**Key APIs.** <the functions and fields by name, so the agent does not hunt for them>

**Done when.** <A named test scenario, with assertions that have numbers in them, to a stated
tolerance. Say what the success line looks like. If there is a visual result, say that the agent
must read the image and describe it.>

**Watch out for.** <traps you already know about, one line each>

---

## Verification, end to end

Per phase, in the sub-agent:

```bash
<build command>
<test command> <scenario>      # must print <the success line>
```

By hand, once the phases are done:

```bash
<how a person launches it>
```

<Then the steps a person takes to satisfy themselves it is real. The tests prove the parts work,
they cannot say whether it feels right.>

<Then: how to check the cross-cutting rule held, as a command.>

---

## Risks

| Risk | What we do |
|---|---|
| <the design rests on X and X might not work> | Phase 0 proves it before anything is built on it. |
| <a guess in this plan is wrong> | Phase 0 measures it and records the real number in the handoff. |
| <the environment disturbs the tests> | <how it is frozen, and in which phase> |
| <a phase quietly breaks the cross-cutting rule> | Stated at the top, repeated in each prompt, checked by a guard in the last phase. |
| <the spec repo is deleted before the phase that reads it> | <the fallback source, and what it costs> |
```

---

## When the plan is finished

Mark it done at the top rather than deleting it, and rename the file so its state is obvious. Keep:

- the result and where the work landed, as a branch or a commit range,
- the final test line,
- **what the plan got wrong**, in a table: what it said, what was actually true,
- anything left open, so the next person does not have to rediscover it.

That "what the plan got wrong" table is the most useful part of a finished plan. Every row is a
thing someone believed, wrote down, and then learned was false by running it.
