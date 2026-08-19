# Unity EditMode/PlayMode gate

Unity changes are not considered validated by the .NET suite alone. The exact
candidate commit must pass both Unity EditMode and PlayMode using Unity
`6000.5.6f1`.

## Automated gate

The `unity-tests` CI matrix contains independent `editmode` and `playmode`
jobs. It is deliberately controlled by the repository variable
`UNITY_CI_ENABLED`.

Keep the variable set to `false` until these repository secrets contain valid
Unity Personal or Professional CI credentials:

- `UNITY_LICENSE`;
- `UNITY_EMAIL`;
- `UNITY_PASSWORD`.

After replacing the placeholder values, set `UNITY_CI_ENABLED=true` and run the
workflow manually or update a trusted branch. Confirm both jobs produce XML and
log artifacts before making them required branch checks.

Repository secrets are unavailable to pull requests from forks by design. Do
not use `pull_request_target` to execute untrusted Unity project code with
secrets. A maintainer must run the manual gate on the fork commit instead.

## Manual gate

From a clean checkout on macOS with Unity Hub Editor `6000.5.6f1` installed:

```sh
./scripts/sync-core-to-unity.sh

UNITY_EDITOR=/Applications/Unity/Hub/Editor/6000.5.6f1/Unity.app/Contents/MacOS/Unity
PROJECT_PATH="$PWD/src/HitTheKit.Unity"
RESULT_ROOT="$PWD/artifacts/unity-tests"
mkdir -p "$RESULT_ROOT"

"$UNITY_EDITOR" -batchmode -projectPath "$PROJECT_PATH" \
  -runTests -testPlatform EditMode \
  -testResults "$RESULT_ROOT/editmode-results.xml" \
  -logFile "$RESULT_ROOT/editmode.log"

"$UNITY_EDITOR" -batchmode -projectPath "$PROJECT_PATH" \
  -runTests -testPlatform PlayMode \
  -testResults "$RESULT_ROOT/playmode-results.xml" \
  -logFile "$RESULT_ROOT/playmode.log"
```

Both commands must exit zero. Inspect the XML and logs rather than reporting
only the process exit status. Record the commit SHA, operating system, Editor
revision, architecture, totals, failures and skipped tests in the pull request.

Documentation-only changes may state that Unity was not required. Runtime,
scene, prefab, package, ProjectSettings, UXML, USS and Editor-tool changes must
provide Unity evidence.

## Activation boundary

Never commit a Unity license file, account password or serial. Store credentials
only as encrypted repository or environment secrets. Placeholder secrets are
not valid credentials and the activation variable must remain `false` while
they are present.
