# luna-chat

luna-chat is a cross-platform native desktop client (macOS + Windows) that wraps a
locally installed `kiro-cli` binary and your collection of `.skill` files into a
visual, chat-based workflow tool.

Instead of running `kiro` from a terminal and managing prompts by hand, you open
luna-chat, toggle the skills you want active for a session, point it at your input
and output folders, and chat. Outputs land in the folder you choose.

The interface follows a utilitarian, Codex-style control-center aesthetic: dark
surfaces, monospace data, and a signature **Skill Rail** of glowing skill chips.

## Features

- **Skill Rail** — toggle `.skill` files on/off per session; glowing chips show what's loaded
- **Terminal-style chat** — streamed kiro output with Markdown rendering
- **Sidebar** — navigation, session history, and a live kiro status indicator
- **File browser** — input/output folder browsing with watch-based refresh
- **Settings** — kiro binary detection, folder paths, appearance, test connection
- **Local-first** — JSON persistence only, no database, no cloud

## Tech Stack

| Layer | Technology |
|---|---|
| UI | AvaloniaUI 12 (C# / .NET 10) |
| Pattern | MVVM (lightweight `ViewModelBase` + `RelayCommand`) |
| kiro integration | `Process.Start()` subprocess calls |
| Markdown | Markdig |
| Persistence | `System.Text.Json` to the platform data dir |

> The original spec targeted .NET 8 / Avalonia 11 + ReactiveUI. This build targets
> .NET 10 / Avalonia 12 (the installed SDK). Avalonia 12 has no ReactiveUI package,
> so a lightweight hand-rolled MVVM layer is used instead. All spec features are
> implemented.

## Prerequisites

- .NET 10 SDK
- `kiro-cli` installed and on your `PATH` (optional — the app runs without it and
  shows a "kiro not found" status)

## Run

```bash
dotnet run --project luna-chat.csproj
```

or:

```bash
./run.sh
```

## First-run setup

1. Open **Settings** (sidebar ⚙).
2. Confirm the **kiro binary path** (auto-detected from `PATH`) and click **Test connection**.
3. Set your **Skills folder** (containing `.skill` files or folders with `SKILL.md`).
4. Set **Input** and **Output** folders.
5. Click **Save settings**. Skills appear in the Skill Rail on the chat view.

## Data locations

| Platform | Data dir |
|---|---|
| macOS | `~/Library/Application Support/LunaChat/` |
| Windows | `%APPDATA%\LunaChat\` |

Sessions are stored as JSON under `sessions/`, settings in `settings.json`.

## Build

See `build/build-mac.sh` and `build/build-windows.ps1` for publishing
self-contained binaries.
