# HitTheKit website

Static, dependency-free public website for HitTheKit. The production entry point
is `index.html`; styles and behavior live under `assets/`.

Run a local preview from the repository root:

```sh
python3 -m http.server 4173 --directory website
```

Then open `http://127.0.0.1:4173/`.

Before publishing under a custom domain, update the canonical URL, Open Graph
image URL, `robots.txt`, and `sitemap.xml`.

The public repository deploys this directory through
`.github/workflows/pages.yml`. The private historical archive must keep public
deployment disabled.
