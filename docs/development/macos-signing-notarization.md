# macOS signing and notarization

This is the distribution-only workflow for the HitTheKit macOS player. Normal
Unity Editor, EditMode, PlayMode, and local development builds do not require a
Developer ID signature or an Apple notarization submission.

## Requirements

- macOS on Apple silicon with Unity `6000.5.6f1`;
- Xcode and its command-line tools;
- an Apple Developer Program membership;
- exactly one usable `Developer ID Application` identity and its private key in
  the login Keychain, or `HITTHEKIT_CODESIGN_IDENTITY` set to the intended valid
  identity;
- a notarytool Keychain profile named `HitTheKit-Notary`.

Configure the notary profile personally with:

```bash
xcrun notarytool store-credentials "HitTheKit-Notary"
```

Credentials and private keys remain in Keychain. They are never arguments to
the repository scripts and must never be committed.

## Recommended first-release operation

For the first public candidate, build, sign, notarize, and package on the
dedicated Mac that already contains the legitimate Developer ID private key,
the exact Unity Editor and modules, and the activated Unity license. Run the
pipeline from an exact clean tag, then upload only the verified DMG, ZIP,
checksums, and release evidence. This avoids exporting the signing identity
before the automated release path has been reviewed end to end.

Do not configure a general-purpose personal Mac as a permanently trusted
self-hosted runner merely to avoid moving the certificate. A workflow executed
on that machine can run repository-controlled code with the runner account's
permissions. If a self-hosted runner is introduced later, dedicate and isolate
it, restrict it to manual protected release jobs, never run untrusted pull
requests on it, and remove release credentials outside the narrow signing job.

## Optional GitHub Actions signing path

GitHub Actions secrets can hold a small password-protected PKCS#12 (`.p12`)
export after Base64 encoding, together with its password. Base64 is only a text
encoding; the protection comes from GitHub Secrets, the `.p12` password, a
temporary keychain, and strict workflow permissions.

A future protected `release` environment would need at least:

- `BUILD_CERTIFICATE_BASE64`: Developer ID Application certificate **and
  private key** exported as a password-protected `.p12`;
- `P12_PASSWORD`: export password;
- `KEYCHAIN_PASSWORD`: a unique temporary keychain password; and
- notarization credentials, either an app-specific password with Apple ID and
  Team ID, or an eligible **team** App Store Connect API key. Individual API
  keys cannot be used by `notarytool`.

The workflow must import the identity into a temporary keychain, create the
temporary `HitTheKit-Notary` profile expected by the scripts, run the complete
pipeline, verify the artifacts, and delete the keychain and decoded files even
on failure. It must run only through a manual or protected release trigger;
secrets must never be exposed to pull-request builds.

Signing secrets solve only certificate access. A GitHub-hosted runner must also
install Unity `6000.5.6f1`, the required platform modules, and use a valid Unity
license. Until that complete path is tested, local signing on the dedicated Mac
is the simpler and safer release procedure.

## Build and signing architecture

`build-macos-distribution-app.sh` builds the native arm64 CoreMIDI plug-in,
synchronizes the managed Core assembly, and asks Unity to create a non-
Development macOS player. The bundle identifier remains
`com.codewriter90x.hitthekit`. Before signing, it installs the MPL license,
project notice, third-party notices, licensing overview, and an exact-revision
`SOURCE-CODE.txt` under `Contents/Resources/Legal`. The final DMG exposes the
same `Legal/` directory at its top level.

`sign-macos-app.sh` signs a staged copy and replaces the input only after the
staged bundle passes strict verification. It signs every nested Mach-O first,
including `HitTheKitCoreMidi.dylib`, Burst, UnityPlayer, and Mono libraries. The
outer app is signed last. `codesign --deep` is not used for signing.

Every distribution component uses:

- the same Developer ID Application identity;
- Hardened Runtime (`--options runtime`);
- Apple's secure timestamp service (`--timestamp`).

The outer Unity player is signed with `scripts/HitTheKit.entitlements`. The
current Mono scripting backend performs just-in-time compilation, so Hardened
Runtime requires the narrowly scoped
`com.apple.security.cs.allow-jit` entitlement. This requirement was reproduced
against the signed distribution player: without it Mono stopped before engine
initialization and no render surface was created; with it the same build
initialized Unity and Metal normally.

Nested libraries inherit the host executable's entitlement and are not given
their own entitlement blob. CoreMIDI input through a system framework does not
require App Sandbox or another Hardened Runtime exception. Release builds must
not acquire broader exceptions such as unsigned executable memory, disabled
executable-page protection, disabled library validation, `get-task-allow`,
network, microphone, camera, location, or Apple Events without a separately
demonstrated requirement.

## Verification

Before notarization:

```bash
./scripts/verify-macos-signature.sh --pre-notarization \
  artifacts/macos-distribution/0.3.0/HitTheKit.app
```

The verifier checks every Mach-O for a Developer ID authority, matching Team
ID, Hardened Runtime, secure timestamp, and absence of ad-hoc signatures. It
also runs Gatekeeper assessment, but a rejection before notarization is
expected. After stapling, the default mode requires Gatekeeper acceptance:

```bash
./scripts/verify-macos-signature.sh HitTheKit.app
```

## Notarization and distribution

`notarize-macos-app.sh` verifies the signature, packages the app with `ditto`,
and submits exactly that package through `xcrun notarytool` using only the
`HitTheKit-Notary` profile. Upload completion is insufficient: the script
requires final status `Accepted`. Invalid submissions stop, and their Apple log
is kept only below the ignored artifact directory.

After acceptance the script staples and validates the ticket, then requires a
successful Gatekeeper assessment. The complete pipeline additionally creates a
final resource-safe ZIP and a drag-to-Applications DMG. The DMG itself is sent
to Apple's notary service, stapled, and assessed as the final distribution
container, following Apple's direct-distribution guidance:

```bash
HITTHEKIT_BUILD_NUMBER=1 \
  ./scripts/build-sign-notarize-macos.sh 0.3.0
```

Generated apps, ZIPs, submission summaries, and notary logs remain below
`artifacts/`, which is ignored by Git.

GitHub prereleases are public distribution artifacts and must use the complete
`build-sign-notarize-macos.sh` output. Do not upload ZIPs produced by
`package-game-macos-arm64.sh`: that script intentionally uses an ad-hoc
signature for local playtesting and its output will be rejected by Gatekeeper
on another Mac. The distribution build also installs the canonical
`branding/app-icon/macos/HitTheKit.icns` before Developer ID signing.

Commercial audio and charts are never added to the public DMG or repository.
For a player who owns local song bindings, `package-local-song-pack.sh` creates
a private ZIP containing only each selected song's `song.json`, declared chart,
and declared audio. Extracting that ZIP into `~/Documents` produces the
automatically discovered `~/Documents/HTKSongs` folder.

## Runtime smoke

`smoke-macos-app.sh` launches the signed player for a bounded interval and
rejects immediate exits, native-loader/signature failures, early native stack
traces, incomplete Unity initialization, and failure to create the Metal render
surface. Before an actual submission, also visually confirm the Main Menu and
exercise CoreMIDI availability on the release machine when hardware is present.

The smoke launches the application through LaunchServices, matching a
Finder/Gatekeeper launch. Do not execute the binary under `Contents/MacOS`
directly as a GUI smoke: current macOS releases can abort an AppKit application
during process registration when its bundle is bypassed.

## Security and recovery on a new Mac

Never store signing credentials in environment files, shell scripts, issue
comments, CI logs, or Git. Files such as `.p12`, `.pfx`, `.p8`, and certificate
requests are explicitly ignored.

On a new Mac, legitimately install the Developer ID certificate together with
its private key according to the Apple account policy, then configure the
`HitTheKit-Notary` Keychain profile again. Git is not a signing-key backup.

If the maintainer chooses to create a `.p12` backup manually, keep it in
encrypted private storage outside Git. Rotate or revoke compromised or expired
certificates in the Apple Developer account, install the replacement identity,
and validate it before the next distribution.

## Troubleshooting

- **Identity missing/private key missing:** `security find-identity -v -p
  codesigning` must show a valid Developer ID Application identity.
- **Nested code unsigned or ad-hoc:** rebuild and run the inside-out signing
  script; do not patch the bundle with `codesign --deep --sign`.
- **Notary status Invalid/Rejected:** inspect the saved notary log, correct the
  diagnosed bundle problem, rebuild, and submit once again only after local
  verification is green.
- **Stapling failure:** confirm that the accepted submission corresponds to the
  exact app and that the Apple ticket is available.
- **Gatekeeper rejection:** verify Developer ID authority, Team ID, Hardened
  Runtime, secure timestamp, accepted notarization, and staple validation.
