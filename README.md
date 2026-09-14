# SFM Package Builder

SFM Package Builder is a Windows desktop tool for preparing Source Filmmaker model release ZIPs. It stages package contents from selected model, material, README, and extra-file sources without renaming or modifying the original source files.

Current version: 1.0.4

## What It Does

- Selects one primary `.mdl` and optional additional models.
- Includes known same-stem Source model companion files.
- Adds material folders or individual material files.
- Adds optional extra files and folders.
- Generates, imports, or preserves custom README content.
- Previews package contents before build.
- Validates package plans before creating a ZIP.
- Creates portable ZIP packages using .NET ZIP support.

## Build From Source

See [BUILD.md](BUILD.md) for the exact Windows x64 build, test, publish, and ZIP packaging commands.

## License

Original SFM Package Builder source code, documentation, tests, and application-owned assets are dedicated to the public domain under CC0 1.0 Universal. See [LICENSE](LICENSE).

Third-party .NET runtime components included in self-contained binary releases retain their own notices. Release notice files are kept under `outputs/release-assets/licenses/`.

## Development History

This project did not have a pre-existing Git repository. Version control begins with the surviving 1.0.4 source tree. A reconstructed pre-Git history from surviving local development records is in [docs/DEVELOPMENT_HISTORY.md](docs/DEVELOPMENT_HISTORY.md).
