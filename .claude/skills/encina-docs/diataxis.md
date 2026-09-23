# Diátaxis, condensed for Encina

Diátaxis is Daniele Procida's framework for organising technical documentation around what the reader needs at the moment of reading. This file is a paraphrase written for the Encina agents; the canonical text is <https://diataxis.fr/> (licence CC BY-SA 4.0). When this file and the site disagree, the site wins; keep this file short and send readers there for depth.

## 1. The idea

Documentation serves two kinds of need, along two axes:

| Axis | One end | Other end |
|---|---|---|
| What the reader is doing | **Action** (doing something with the product) | **Cognition** (thinking about the product) |
| Where the reader is | **Acquisition** (studying, learning a skill) | **Application** (working, applying a skill they have) |

Crossing the axes gives four kinds of document, each with one job:

| | Acquisition (at study) | Application (at work) |
|---|---|---|
| **Action** | **Tutorial**: a lesson. "Can you teach me to…?" | **How-to guide**: directions. "How do I…?" |
| **Cognition** | **Explanation**: discussion. "Why…?" | **Reference**: description. "What is…?" |

A page that tries to do two of these jobs does both badly: the reader who wanted directions wades through theory, the reader who wanted to understand is pushed through steps. Most documentation problems are pages that leak from one quadrant into another.

## 2. The compass

To classify any piece of content, from one sentence to a whole page, answer two questions:

1. Does it inform **action** (practical steps) or **cognition** (knowledge)?
2. Does it serve **acquisition** (someone learning) or **application** (someone working)?

| Action or cognition | Acquisition or application | Quadrant |
|---|---|---|
| action | acquisition | tutorial |
| action | application | how-to guide |
| cognition | application | reference |
| cognition | acquisition | explanation |

Use it whenever a page feels wrong and you cannot say why. Usually the answer is that paragraphs from two quadrants sit on the same page.

## 3. The four types

### Tutorial (learning-oriented)

A tutorial is an experience under the guidance of a teacher. The learner does something and, by doing it, acquires competence. The teacher carries the responsibility for the outcome.

Do:

- State the destination at the start in terms of what the learner will *do* and *see*, not what they will "learn".
- One linear path, no choices, no alternatives. Decide everything for the learner.
- Deliver a visible result early and often, so cause and effect stay connected.
- Narrate what to expect and what to notice ("The output should look like…", "Notice that…").
- Use concrete, specific steps. Be exact about versions, paths and commands.
- Make it work every time. Test the whole sequence from a clean state before publishing; an unreliable tutorial destroys trust.
- Use "we" for the shared journey and the imperative for the steps.

Do not:

- Explain. Link to an explanation page instead; a sentence at most.
- Offer options, mention alternatives or discuss trade-offs.
- Assume the learner can fill gaps. They cannot yet.
- Write a tutorial that is really a how-to guide with a friendlier tone.

### How-to guide (goal-oriented)

A how-to guide gives directions to a competent user who has a goal and is at work. It answers a real-world question, which usually involves judgement and context, not the operation of a button.

Do:

- Name the goal in the title: "How to run the outbox processor against SQL Server".
- Assume competence. Skip what the reader already knows.
- Order the steps by how the work actually flows; keep momentum, avoid detours.
- Branch where reality branches: "If you use PostgreSQL, …; otherwise …".
- Stay on the goal. Link to reference for the full list of options and to explanation for the reasons.
- Open with "This guide shows you how to…".

Do not:

- Teach. If the reader needs to acquire a skill first, that is a tutorial.
- Aim for completeness. Usefulness for this goal is the measure.
- Explain the design; one sentence with a link is enough.

### Reference (information-oriented)

Reference describes the machinery, neutrally, so that a working user can look something up and get back to work. Its structure mirrors the structure of the product.

Do:

- Describe and only describe. State facts: what it is, what it accepts, what it returns, what it does not do, what to be careful about.
- Follow the product's own structure (package, namespace, type, member; option, default, effect).
- Use one consistent pattern for every entry of the same kind, so the reader knows where each fact sits.
- Give short examples that illustrate usage without turning into a lesson.
- Be austere. Reference is allowed to be boring.

Do not:

- Instruct or persuade. Recipes go to how-to guides, reasons go to explanation.
- Rely only on generated API docs. Generated reference is necessary but rarely sufficient: configuration, conventions, error catalogues and limits need hand-written reference too.
- Invent a structure that does not match the product.

### Explanation (understanding-oriented)

Explanation is discussion that deepens understanding. It is read at leisure, away from the keyboard, and it is the only quadrant where opinion, history and alternatives belong.

Do:

- Frame the title as "About …" or "Why …", even if the words are not written.
- Give context: the problem, the constraints, the history, the decision taken and the ones rejected.
- Make connections to other parts of the product and to ideas outside it.
- Weigh alternatives honestly; say what is better for whom and why.
- Bound the topic. Say what the page covers and what it does not.

Do not:

- Give instructions or close-up technical description. Link to those instead.
- Let it leak into the other three quadrants as asides. Collect it here.
- Treat it as optional. It is less urgent than the others, not less important.

## 4. Telling neighbours apart

**Tutorial versus how-to guide.** Both are about action. A tutorial is a supervised lesson on a practice model: safe, repeatable, one path, the teacher owns the result. A how-to guide is a surgical manual for a practitioner: real conditions, real risk, branches, the reader owns the result. A quickstart that promises a working result in minutes is a tutorial; a page titled "How to configure X for production" is a how-to guide.

**Reference versus explanation.** Both are about cognition. Reference is what you consult while working: lists, tables, signatures, facts, no story. Explanation is what you would read in an armchair: reasons, history, comparisons. A test that works: if a friend asked "tell me about the outbox pattern in Encina", the answer is explanation; if they asked "what does `OutboxOptions.BatchSize` default to", the answer is reference.

## 5. Quality

Two layers, and the second stands on the first:

- **Functional quality**: accurate, complete, consistent, useful, precise. Each is independent and each is a hard requirement. A page that fails one fails at its job.
- **Deep quality**: it flows, it anticipates the reader, it feels good to use. Diátaxis helps here by keeping each page in one rhythm, but it does not guarantee it. Craft still matters.

## 6. Working method

Do not restructure everything at once, and do not create four empty sections and wait for them to fill. Instead, in small cycles:

1. **Choose** one page, section or paragraph.
2. **Assess** it with the compass. Which quadrant is it in? Which quadrant is each paragraph in? What functional quality does it fail?
3. **Decide** one improvement: move a paragraph, split a page, rewrite a title, delete a digression, add the missing link to the neighbouring quadrant.
4. **Do it** and publish. Then pick the next thing.

Structure emerges from well-formed pieces; it is not imposed from above. The documentation is complete and usable at every step of this process, which is what makes it sustainable.

For a product with many parts, apply the four quadrants at the level where the reader's need lives: the whole product has tutorials, how-to guides, reference and explanation, and so may each substantial module. Do not force a single flat four-folder tree on a system with dozens of packages; do keep every individual page in one quadrant.

## 7. Attribution

Diátaxis was created by Daniele Procida and is published at <https://diataxis.fr/> under the Creative Commons Attribution-ShareAlike 4.0 licence. This file is a derived summary for internal use; any Encina document that reproduces Diátaxis material beyond these notes must carry the same attribution and licence.
