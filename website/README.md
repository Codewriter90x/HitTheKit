# HitTheKit website

Static, dependency-free public website for HitTheKit. The Italian production
entry point is `index.html`; the English entry point is `en/index.html`. Styles
and behavior are shared under `assets/`.

Run a local preview from the repository root:

```sh
python3 -m http.server 4173 --directory website
```

Then open `http://127.0.0.1:4173/` or
`http://127.0.0.1:4173/en/`.

The root remains the canonical Italian URL. On a first visit, browsers whose
preferred language is not Italian are redirected to the stable English route.
The language control remembers an explicit choice. Both pages publish
canonical and reciprocal `hreflang` metadata, and both URLs are present in the
sitemap.

Before publishing under a custom domain, update the canonical URL, Open Graph
image URL, `robots.txt`, and `sitemap.xml`.

The public repository deploys this directory through
`.github/workflows/pages.yml`. The private historical archive must keep public
deployment disabled.
