# Public Wiki sources

These Markdown files are the reviewed source for the public
[HitTheKit Wiki](https://github.com/Codewriter90x/HitTheKit/wiki).

The Wiki is a user-facing handbook: it explains how to build, launch, configure
and understand the current pre-release. Deeper architecture, contracts and
release procedures remain under [`docs/`](../README.md).

## Publishing

After changes are merged to `main`, an authenticated maintainer can publish the
current pages with:

```sh
./scripts/publish-github-wiki.sh
```

The script replaces the Wiki's Markdown pages with this directory's versioned
pages, preserving the Wiki Git repository and any non-Markdown attachments. It
does nothing when the rendered Wiki is already current.

Do not edit the rendered Wiki as the long-term source. Make changes here through
a pull request, merge them, and publish again.
