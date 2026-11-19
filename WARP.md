# WARP.md

This file provides guidance to WARP (warp.dev) when working with code in this repository.

Project scope: this file applies to the Zafiro library subtree at libs/Zafiro.

- Tech stack: .NET 8 (targets net8.0). Any recent SDK (8+; 9.x works) is fine. An SDK pin exists at src/global.json (7.0.100) with roll-forward enabled.
- Solution: Zafiro.sln groups core libraries, filesystem layers/implementations, UI helpers, tests, and UI samples.
- Central package/version management via Directory.Packages.props and Directory.Build.props; common NuGet/pack metadata in src/Common.props.
- CI: DotnetDeployer.Fleet tracks master. Each merge builds and tests Zafiro.sln in Release and, if the tests pass, publishes the NuGet packages with deployer.yaml (NUGET_API_KEY comes from Fleet).

Common commands

- Restore
  - dotnet restore Zafiro.sln

- Build
  - Debug: dotnet build Zafiro.sln -c Debug
  - Release: dotnet build Zafiro.sln -c Release

- Tests (xUnit)
  - Run all: dotnet test Zafiro.sln -c Debug --no-build
  - Run project: dotnet test test/Zafiro.Tests/Zafiro.Tests.csproj -c Debug --no-build
  - Run a single test by FQN: dotnet test test/Zafiro.Tests/Zafiro.Tests.csproj --filter "FullyQualifiedName~Namespace.ClassName.TestMethod"
  - Collect coverage (coverlet.collector is referenced): dotnet test Zafiro.sln -c Debug --collect:"XPlat Code Coverage"
  - Note: Some integration tests rely on DotNet.Testcontainers; ensure Docker is running if you execute the full suite.

- Format/Lint
  - Use built-in analyzers/formatting: dotnet format

- Pack (local NuGet)
  - Example (core lib): dotnet pack src/Zafiro/Zafiro.csproj -c Release -o ./artifacts

- Run UI sample
  - dotnet run --project samples/Zafiro.UI/Sample.Desktop/Sample.Desktop.csproj -c Debug

- Publish (NuGet + GitHub release) via DotnetDeployer
  - Install tool (once): dotnet tool install --global DotnetDeployer.Tool
  - Set secrets as env vars in your shell session, e.g. export NUGET_API_KEY={{NUGET_API_KEY}}
  - Dry-run (no push): dotnetdeployer nuget --api-key "$NUGET_API_KEY" --no-push
  - Real publish (mirrors CI on master): dotnetdeployer nuget --api-key "$NUGET_API_KEY"
  - Optionally, create a GitHub release: dotnetdeployer release

Big-picture architecture

This repo is a multi-project solution organized into three main layers, plus tests and samples. Names below map to folders under src/ and test/.

- Core utilities (Zafiro)
  - Purpose: foundational building blocks and utilities that emphasize functional and reactive programming.
  - Notable areas (by namespace/folder):
    - CSharpFunctionalExtensions/*: helpers/mixins around vkhorikov’s CSharpFunctionalExtensions (Result, Maybe, etc.).
    - Reactive/*: IObservable helpers and reactive stream utilities.
    - Commands, Actions, Works: abstractions to model executable operations.
    - DataModel, Misc, Logging: assorted primitives and cross-cutting helpers.
  - Dependencies: commonly referenced across other projects; relies on ReactiveUI, System.Reactive, Microsoft.Extensions.*, NodaTime, MoreLINQ, Serilog.

- Filesystem abstraction and implementations (Zafiro.FileSystem family)
  - Purpose: a unified, high-level filesystem API with pluggable backends.
  - Core (Zafiro.FileSystem):
    - Contracts like IZafiroFileSystem, IZafiroDirectory, IZafiroFile and related mixins.
    - Functional/async wrappers, caching, smart layers, and comparison strategies.
  - Implementations:
    - Zafiro.FileSystem.Local and Zafiro.FileSystem.Unix: local filesystem specifics.
    - Zafiro.FileSystem.Sftp: SFTP backend (SSH.NET).
    - Zafiro.FileSystem.SeaweedFS and *.Filer.Client: SeaweedFS Filer client (Refit-based).
  - Actions (Zafiro.FileSystem.Actions): composable file operations (e.g., copy, directory ops).
  - Tests: per-implementation test projects (e.g., Zafiro.FileSystem.Local.Tests, SeaweedFS.Tests) + shared tests.

- UI helpers (Zafiro.UI)
  - Purpose: platform-agnostic UI building blocks aligned with Avalonia/ReactiveUI patterns.
  - Areas: navigation (Navigator, Sections), wizards (Classic/Slim), jobs/execution (Stoppable/Unstoppable), commands, transfers, and UI-facing primitives.
  - Dependencies: ReactiveUI.SourceGenerators, ReactiveUI.Validation; consumes Zafiro.FileSystem abstractions.
  - Samples: runnable demos under samples/Zafiro.UI (Sample and Sample.Desktop).

- Versioning, packaging, and shared props
  - Directory.Packages.props centralizes NuGet versions (e.g., Avalonia, ReactiveUI, xUnit, coverlet, etc.).
  - Directory.Build.props defines AvaloniaVersion and enables nullable.
  - src/Common.props sets NuGet metadata (PackageId, Authors, Icon, Tags) shared by packable projects.
  - GitVersion.yml uses GitHubFlow/Mainline strategies; CI integrates with DotnetDeployer for releases.
  - **Semantic versioning**: When merging PRs with breaking changes, include `+semver:major` in the commit message title or body. Without it, GitVersion will only bump the minor version. For non-breaking features use `+semver:minor`, for patches use `+semver:patch` (or omit, as patch is the default).

Project rules for Warp

- Prefer ReactiveUI.Validation and CSharpFunctionalExtensions; lean into a functional style and reactive programming where sensible.
- Do not suffix method names with Async even if they return Task.
- Publishing is handled with DotnetDeployer (nuget and release). Avoid introducing or relying on NUKE build; any legacy references should be ignored in favor of DotnetDeployer.

Android AOT + XAML Behaviors

- Always use the Avalonia XML namespace URI for behaviors instead of assembly-qualified namespaces in XAML.
  - Use xmlns:b="https://github.com/avaloniaui" and tags like b:DropTargetBehavior, b:DragDrop, etc.
  - Do not use xmlns with assembly=... for Avalonia.Xaml.Interactions.* or Xaml.Behaviors.Interactions.* because it forces runtime assembly loads that may be trimmed in Release/AOT.
- Consolidate behavior packages to Xaml.Behaviors family and avoid mixing Avalonia.Xaml.Interactions.* with Xaml.Behaviors.* on Android.
  - Rationale: duplicate assemblies and differing assembly identities complicate trimming and packaging.
- When building Android Release with AOT and trimming, ensure behaviors assemblies are rooted for the linker:
  - Add TrimmerRootAssembly entries for Xaml.Behaviors and the specific Xaml.Behaviors.Interactions.* packages used.
  - This prevents the linker from removing assemblies that are referenced indirectly via XAML.
- MinSdk and globalization:
  - AndroidMinSdkVersion is set to 21 and InvariantGlobalization=true to reduce APK size and warnings.

Rationale

- In AOT/trimmed builds, any assembly only referenced via XAML may be removed by the linker. Using the Avalonia URI (XmlnsDefinition) avoids hard assembly loads and works across platforms.
- Rooting the assemblies guarantees they are packaged and available at runtime, preventing FileNotFoundException on startup when parsing XAML templates that reference behaviors.

