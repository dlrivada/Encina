Write ONE section of `docs/engineering/PROJECT-HISTORY.md` for the Encina repository from the knowledge items listed at the end of this message. Every item was extracted from a closed GitHub issue and carries the issue number and a citation. Follow every numbered point without omitting any. Output only the Markdown section: no preamble, no closing remarks.

1. Structure, in this order, using `###` headings; omit a heading only when it has no items:
   `### Decisions` · `### Rules the project committed to` · `### Alternatives considered and rejected` · `### Things learned the hard way` · `### Changes of direction`
   Under each heading write one bullet per distinct fact, as a Markdown list item starting with `- `. When one line lists several issue numbers, cite all of them in that bullet.
2. Every bullet ends with its citation in the form `(#123)` or `(#123, #456)`. The only valid numbers are the ones written as `issue #N` in the items below (also repeated in the allowed list before the items). Never write any other number after a `#`.
3. Keep each bullet to one or two sentences, stating the fact and, when the item gives it, the reason. Do not add commentary, do not evaluate, do not soften. Use the project's terms exactly as the items spell them (package names, type names, option names).
4. Items whose relevance is "no" go under a final `### No longer applicable` heading with one bullet each, still cited; do not mix them into the sections above.
5. Do not repeat a fact in two headings; if an item could be both a decision and a rule, place it where the item's type says.
6. Style: plain English, no em dashes, no exclamation marks, no marketing language. Between 150 and 700 words depending on the number of items.
7. Stop when the section is written.

Area and items:
