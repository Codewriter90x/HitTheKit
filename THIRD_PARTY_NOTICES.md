# Third-Party Notices

HitTheKit's license does not replace the licenses or terms that govern
third-party software. This inventory records dependencies verified from the
repository and resolved package metadata as of 2026-08-18. Project artwork
provenance is recorded separately in
[`docs/legal/ASSET_PROVENANCE.md`](docs/legal/ASSET_PROVENANCE.md).

| Component | Version | Declared license or terms | Role | Redistributed by this repository? | Review status / action |
| --- | --- | --- | --- | --- | --- |
| Unity Editor and built-in Unity modules | Editor 6000.5.6f1; modules 1.0.0 | Unity Terms of Service and Unity Editor Software Terms | Authoring, compilation, runtime APIs | Editor and module source/binaries are not tracked; a future player build may contain Unity runtime components | **Legal review required:** confirm current plan eligibility, player redistribution, GPL interaction, and separate Unity permission for embedded systems. |
| Unity Test Framework | 1.7.0 | Unity Companion License v1.4 | EditMode and PlayMode testing | Package source is resolved by Unity Package Manager, not tracked or intended for player distribution | Preserve the package license and copyright notice if substantial package portions are redistributed. |
| Unity Custom NUnit (`com.unity.ext.nunit`) | 2.1.0, based on NUnit 3.5 | Unity Package Distribution License v2.1; bundled NUnit portions under MIT | Transitive Unity test dependency | Resolved by Unity Package Manager, not tracked or intended for player distribution | Preserve Unity package terms and the bundled NUnit MIT notice when applicable. |
| .NET SDK | 8.0.411 requested by `global.json` | MIT plus the distribution's third-party notices | Build tooling | No | Build tool only; follow the license and notices of the installed distribution. |
| Microsoft.NET.Test.Sdk | 18.9.0 | MIT | Core and MIDI-tool test tooling | NuGet package is restored, not tracked; not a runtime dependency | Preserve MIT notices if redistributed. |
| xUnit.net / Visual Studio runner | xUnit 2.9.3; runner 4.0.0 | Apache License 2.0 / MIT as declared by the packages | Core and MIDI-tool test tooling | NuGet packages are restored, not tracked; not runtime dependencies | Preserve the applicable notices if redistributed. |
| Melanchall.DryWetMidi | 8.0.3 | MIT | MIDI-file parsing in the standalone `HitTheKit.MidiCapture` developer tool | Present on `main` as a restored NuGet dependency of the tool; it is not linked into the Unity runtime | Preserve the MIT notice and audit the exact packaged dependency set before distributing the tool. A HitTheKit commercial license does not relicense DryWetMIDI. |
| Universal Render Pipeline and rendering packages | URP/Core/Shader Graph/URP Config 17.5.0; Searcher 4.9.4 | Package-specific Unity license files and Unity Package Manager terms | Unity rendering and shader authoring | Resolved by Unity Package Manager; package source is not tracked | Preserve the license and notice shipped with each exact resolved package when redistribution makes it applicable. |
| Unity resolved support packages | Burst 1.8.29; Collections 6.5.0; Mathematics 1.4.0; Performance Testing 3.5.0; Mono.Cecil 1.11.6 | Package-specific license files; Mono.Cecil is MIT | Transitive compilation, collections, mathematics, testing and assembly-inspection support | Resolved by Unity Package Manager; package source is not tracked | Regenerate this list from `Packages/packages-lock.json` for a release candidate and preserve every applicable package notice. |
| GitHub Unity `.gitignore` template | upstream `github/gitignore` template, revision not recorded | CC0-1.0 | Repository ignore rules | The adapted text is tracked in `.gitignore` | No attribution required by CC0; provenance is retained in the file header and this inventory. |

## Verified sources

- GNU GPL version 3: <https://www.gnu.org/licenses/gpl-3.0.html>
- Unity terms: <https://unity.com/legal/terms-of-service>
- Unity Companion License: <https://unity.com/legal/licenses/unity-companion-license>
- Unity Package Distribution License:
  <https://unity.com/legal/licenses/unity-package-distribution-license>
- Microsoft .NET SDK: <https://github.com/dotnet/sdk>
- Microsoft test platform: <https://github.com/microsoft/vstest>
- xUnit.net: <https://www.nuget.org/packages/xunit/2.9.3>
- xUnit.net Visual Studio runner:
  <https://www.nuget.org/packages/xunit.runner.visualstudio/4.0.0>
- DryWetMIDI 8.0.3:
  <https://www.nuget.org/packages/Melanchall.DryWetMidi/8.0.3>
- GitHub gitignore templates: <https://github.com/github/gitignore>

This is a good-faith engineering inventory, not a definitive legal
compatibility opinion. Before distributing binaries, regenerate the inventory
from the exact build and retain every license or notice required by packaged
components.
