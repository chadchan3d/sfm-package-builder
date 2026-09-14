# SFM Package Builder 1.0.4

Build ZIP packages for Source Filmmaker model releases. SFM Package Builder helps collect model families, material folders, Extras, shared-file notices, and README content into the package layout expected by SFM users.

This tool does not compile models, edit source files, install packages into SFM, or verify that a model loads inside Source Filmmaker. Always test the resulting ZIP in a real SFM environment before publishing it.

Current releases, related SFM assets, and related tools are available at:

https://chadchan3d.com/category/assets/

## Requirements

- Windows x64.
- Source Filmmaker model, material, and supporting files available on disk.

No external archive application is required. ZIP creation uses the application runtime.

Windows may show an Unknown Publisher warning because the application is not code-signed.

## Basic Workflow

1. Start `SfmPackageBuilder.exe`.
2. Create or open a `.sfmpack` project.
3. Use Model & Materials to add the primary model, any additional models, and material folders. Related model files are discovered from the current source files.
4. Use Release Details to enter the release name, version, and change notes.
5. Use README to generate, write, import, or omit `README.txt`.
6. Use Extras for supporting files such as documentation, configuration files, or rig scripts when a package needs them.
7. Use Review & Build to inspect the package contents, check the package, choose the output folder and ZIP filename, and build the ZIP.
8. Resolve any required decisions, such as same-version rebuilds or existing output archives.
9. Test the ZIP in Source Filmmaker before sharing it.

## Project Files

`.sfmpack` files store the project recipe: selected sources, destination overrides, release metadata, README configuration, current version, and release history. They do not store global machine settings such as your default output folder, display preferences, or recent projects. Later builds use the current source files on disk.

Source file presence and companion detection are recomputed from disk when relevant. The project file preserves user choices such as selected or excluded companions, but observed filesystem conditions are not authoritative saved state.

## Safety

Builds copy selected content into an app-owned staging folder under your per-user application data directory, then create the ZIP from that staging folder. Source files are not renamed or modified.

Changing release model names affects only the package copies written into the ZIP. Original source filenames stay unchanged.

If the target archive already exists, the application asks whether to Replace, Choose Another Name, or Cancel. Same-version rebuilds also require an explicit application-layer decision.

## Troubleshooting

### Path Cannot Resolve

Use paths that exist on this machine and are accessible to your Windows user account. Long paths are supported to the practical extent available through Windows and .NET.

### Missing Sources

Open the project and use the missing-source recovery workflow. Locate the replacement source deliberately, or remove the missing entry from the package. The application should not silently guess replacements.

### Collisions

Preview Package and Check Package report package destination collisions. Resolve them by changing source selections, release names, or destination overrides so each final package path is intentional.

### Archive Already Exists

Build will ask whether to replace the existing archive, choose another name, or cancel. Replacement affects only the output archive; source files remain unaffected.

### Version Already Exists

A same-version warning applies when that version has already been built from the project. README changelog entries document release notes; they do not by themselves count as prior builds.

### Output ZIP Location

The output folder and archive name fields determine where the ZIP is written. Global default output preferences live in user settings, not in the `.sfmpack` file.

### Build Log and Diagnostics

Build results include validation, staging, ZIP creation, archive verification, cleanup, and decision details. Use those diagnostics when troubleshooting a failed package.

## Program License

SFM Package Builder's original project code and application assets are dedicated to the public domain under CC0 1.0 Universal. The bundled `README.txt` contains the user-facing license statement.

Third-party runtime components retain their own terms. Official runtime licensing material belongs under the `licenses` folder in the downloadable application package.

The program license is separate from model-package usage terms entered by individual creators. Model usage terms describe how downloaders may use or redistribute that specific model package.
