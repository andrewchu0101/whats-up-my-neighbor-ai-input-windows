# Repository migration — 2026-10-18

This public repository is the independent Windows edition of **What's up my neighbor AI Input**.

- Original repository: https://github.com/yuipan0611/groq-ai-voice-input-macos (renamed in place to https://github.com/yuipan0611/whats-up-my-neighbor-ai-input-macos).
- Snapshot: `eceb1b508af83446431244741263d79e0903f911` on original `main`.
- Retained history: `windows/`, Windows CI, `LICENSE`, and `.gitignore`; `windows/` was promoted to the root. No macOS application code or signing secrets were imported.
- Tool: git-filter-repo `a40bce548d2c`. Authors and commit messages were retained; filtered commit IDs necessarily differ. See [commit map](migration-commit-map.txt); zero IDs mean commits without retained changes were removed.
- Historical beta: original source `b072d0a61d976a6765b3a6ce6eb47527b466c2f8` maps to `c1dda7ffa684de56afb12dba06d3f4bc214c710d`. Tag `windows-v0.1.0-beta.1` identifies that historical source, not the migration edits on `main`.
- The beta release assets are byte-for-byte copies of the original assets. Their manifest and validation evidence intentionally retain the original source SHA and CI run. Copying them is not a rebuild or a new acceptance test.
- Migration CI on `main` builds the relocated sources independently and uploads development artifacts. It does not replace the historical beta or publish a stable release.
- Historical CI, PR, and release evidence in `VALIDATION.md` continues to point to the original repository. GitHub redirects those URLs after its rename.
- AppId, executable name, Credential Manager target, settings, vocabulary paths, and installer upgrade identity are unchanged. Existing beta installers still contain their original publisher links.
- Real Windows 10/11 microphones, Bluetooth, Chinese IME, and third-party applications remain pending physical-device acceptance.

No feature parity with macOS is implied. Future development and releases happen independently here.
