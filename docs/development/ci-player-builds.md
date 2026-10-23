# CI player builds

The `Player builds` workflow (`.github/workflows/player-builds.yml`) produces the
Windows x64 playtest ZIP and the macOS Apple-silicon app on a GitHub-hosted
`macos-15` runner. It calls the same scripts used for local releases:

- `scripts/package-game-windows-x64.sh` (unsigned Windows playtest ZIP);
- `scripts/build-sign-notarize-macos.sh` (Developer ID signature, notarization,
  smoke test, DMG and ZIP), or `scripts/package-game-macos-arm64.sh` for an
  unsigned macOS playtest when signing is switched off.

Results are uploaded as a workflow artifact with a `SHA256SUMS` file and a job
summary. **Nothing is published automatically**: creating a GitHub prerelease,
attaching files and recording release evidence remain manual, reviewed steps.

## When it runs

- **Manually**: Actions → Player builds → Run workflow, with a numeric version
  (for example `0.6.1`) and the "sign and notarize" switch.
- **On tags** matching `vMAJOR.MINOR.PATCH*` (for example `v0.6.1-beta.1`); tag
  builds are always signed. The packaging scripts receive the numeric part
  (`0.6.1`).

The job is skipped unless the repository variable `UNITY_CI_ENABLED` is `true`,
the same switch used by the Unity test gate. Secrets are never exposed to pull
requests from forks: the workflow does not run on `pull_request`.

## One-time setup

Settings → Secrets and variables → Actions.

### Variable

| Name | Value |
| --- | --- |
| `UNITY_CI_ENABLED` | `true` |

### Secrets

| Secret | Used by | How to obtain |
| --- | --- | --- |
| `UNITY_EMAIL` | tests, builds | Unity ID e-mail |
| `UNITY_PASSWORD` | tests, builds | Unity ID password (an account without 2FA prompts, or a dedicated CI account) |
| `UNITY_LICENSE` | tests (`game-ci`) | Contents of the activated license file. On a Mac signed in to Unity Hub: `/Library/Application Support/Unity/Unity_lic.ulf` |
| `MACOS_CERTIFICATE_P12` | signed builds | Export the **Developer ID Application** certificate *with its private key* from Keychain Access as `.p12`, then `base64 -i developer-id.p12 \| pbcopy` |
| `MACOS_CERTIFICATE_PASSWORD` | signed builds | Password chosen when exporting the `.p12` |
| `APPLE_ID` | notarization | Apple Developer account e-mail |
| `APPLE_TEAM_ID` | notarization | 10-character Team ID (developer.apple.com → Membership) |
| `APPLE_APP_SPECIFIC_PASSWORD` | notarization | appleid.apple.com → Sign-In and Security → App-Specific Passwords |

The signing step creates a temporary keychain, imports the certificate, stores
the `HitTheKit-Notary` notarytool profile expected by
`scripts/notarize-macos-app.sh`, and deletes the keychain at the end of the job.

## Verifying the setup

1. Set the secrets and the variable.
2. Run **Player builds** manually with signing **off**. This proves the Unity
   installation, license activation and both player builds.
3. Run it again with signing **on**. Check the job summary for the checksums and
   the notarization log in the macOS step.
4. Download the artifact and test both players on real machines before any
   prerelease, as required by the release checklist.

## Known limitations

- First runs download Unity 6000.5.6f1 with Windows Build Support (several GB);
  expect a long job.
- `scripts/smoke-macos-app.sh` launches the app on the runner's desktop session.
  If it proves flaky on hosted runners, run it locally on the downloaded build
  instead.
- The Windows package is unsigned and keyboard-only until a Windows MIDI backend
  ships (#53).
