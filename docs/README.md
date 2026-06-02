# Gamestack documentation

A [DocFX](https://dotnet.github.io/docfx/) site combining hand-written conceptual articles
(`articles/`) with an API reference generated from the projects' XML `///` comments.

## Build

Install the tool once:

```bash
dotnet tool install -g docfx
```

Then, from the repo root:

```bash
# Generate the API metadata (api/*.yml) from the source projects, then build the site:
docfx docs/docfx.json

# …or build and serve locally with live reload:
docfx docs/docfx.json --serve
```

The static site is emitted to `docs/_site`. Generated output (`docs/_site`, `docs/api/*.yml`) is
git-ignored — only the config and conceptual `.md` files are committed.

## Wiring into CI (later)

A CI job can run `dotnet tool restore` + `docfx docs/docfx.json` and publish `docs/_site` (e.g. to
GitHub Pages). Not yet configured.
