# KanbanBoard

**A minimal, extensible Windows desktop kanban widget.**

KanbanBoard starts as a single unobtrusive floating bar on your desktop. Press a hotkey to capture a task instantly, or hover over the bar to expand it into a full desktop workbench — kanban board, calendar, schedule, sticky notes, and a system monitor. Minimal by default, powerful when you need it.

## Features

### Floating bar & interaction
- Always-on-top bar showing the current time, today's due-task count, and a quick-add button
- Hover to expand (250 ms default), move away to collapse (400 ms default) — both configurable
- Drag to move; the position is remembered and auto-clamped to screen edges
- System tray icon + menu: Show/Hide, Quick Add, Notes, Focus Mode, Lock Layout, Click-through, Settings, Check for Updates, Start on Boot, Exit
- Global hotkeys (defaults: `Ctrl+Shift+Space` quick add, `Ctrl+K` command palette) with conflict detection
- Command palette: search tasks globally and run commands (quick add / settings / toggle view / focus mode / exit)
- Auto-hide when a fullscreen app is active (game / slideshow / remote desktop); restore on exit
- Hide on lock screen; restore on unlock
- Per-Monitor V2 DPI awareness, basic multi-monitor support

### Kanban & tasks
- Three columns: **To Do / Doing / Done**
- Cards carry title, description, priority, tags, due date, repeat rule, and subtasks
- Drag to reorder within a column; drag across columns to move
- Check to complete; double-click to edit
- Right-click menu: edit / archive / delete
- Archive and Trash views with restore / delete-forever
- Four view modes: **Kanban / List / Calendar / Schedule**
- Live search

### Quick add (natural language)
Type something like:

```
Submit report Friday 3pm #work !high remind 15m before
```

and KanbanBoard parses:

- **Date** — today / tomorrow / next Monday / 12/31 / 明天 / 周五 / 3月15日
- **Time** — 3pm / 15:30 / 下午3点
- **Repeat** — every day / weekly / every weekday / 每天 / 每周
- **Tags** — `#work`
- **Priority** — `!high` / `!low` / `!urgent` / `!高` / `!紧急`
- **Reminder** — `remind 15m before` / `提前15分钟提醒`

Both Chinese and English are supported. A live preview shows what was parsed; press Enter to save.

### Reminders & notifications
- Toast notifications with Snooze / Complete / Dismiss actions
- Merged notification when many tasks fall due at once
- Repeating tasks auto-advance to their next occurrence
- Focus mode and Windows Do-Not-Disturb (best-effort) suppress reminders

### Local data
- SQLite (WAL mode); every change is saved immediately
- Automatic backup at startup (default keep 14 days, configurable 7–30)
- Crash recovery: on a corrupted database, restore from the latest valid backup, keep the original file, and notify
- Schema migration: backup → transactional migration → rollback on failure; a database written by a newer version is refused by older builds
- Import/export: JSON / CSV / Markdown / ICS

### Sticky notes
- Markdown text with 6 color markers; pin a note to the desktop
- Pinned notes float as small always-on-top windows (drag, double-click to edit, × to unpin)
- Notes manager window (create / edit / delete), reachable from the board header 📝 or the tray

### System monitor
- Status bar in the expanded board: CPU / memory / disk / battery (2-second polling)

### Settings & personalization
- Theme: Dark / Light / Follow system
- Material: None / Transparent / Acrylic / Mica
- Opacity (10%–100%), font scale
- Languages: Simplified Chinese / Traditional Chinese / English
- Fully customizable hotkeys
- Start on boot (with optional delay), auto backup, backup retention, anonymous crash reporting

## Tech stack

- **C# / WPF / .NET 8** (native Windows)
- **SQLite** via `Microsoft.Data.Sqlite`
- **Win32 P/Invoke** for the tray icon, global hotkeys, click-through, top-most, fullscreen detection, and window material

## Project layout

```
KanbanBoard/
├── KanbanBoard.sln
├── README.md
├── LICENSE
├── publish.ps1              # publish a self-contained portable build
├── run.ps1                  # build + run (dev)
├── docs/
│   └── ARCHITECTURE.md
├── src/KanbanBoard/
│   ├── App.xaml(.cs)        # entry point + service wiring
│   ├── MainWindow.xaml(.cs) # floating bar + expanded workbench
│   ├── Models/              # domain models + settings
│   ├── Data/                # SQLite, migration, backup, repositories
│   ├── Services/            # hotkeys/tray/notifications/reminders/parser/detectors/import-export/theme
│   ├── Interop/             # Win32 P/Invoke
│   ├── Controls/            # columns, cards, quick-add, editor, settings, toast, calendar, schedule, notes
│   ├── ViewModels/          # MVVM
│   ├── Converters/          # value converters
│   └── Resources/Themes/    # light/dark themes + base styles
└── tests/KanbanBoard.Tests/ # xUnit tests
```

## Build & run

Requires the .NET 8 SDK.

```powershell
# Build + run (dev)
.\run.ps1

# Or manually
dotnet build src\KanbanBoard\KanbanBoard.csproj -c Debug
```

Smoke test (starts and auto-exits after 2 s, used to verify the app boots without crashing):

```powershell
src\KanbanBoard\bin\Debug\net8.0-windows\KanbanBoard.exe --smoke-test
```

## Tests

```powershell
dotnet test tests\KanbanBoard.Tests\KanbanBoard.Tests.csproj
```

## Publishing a portable build

```powershell
.\publish.ps1
# Output: publish\KanbanBoard.exe (self-contained single file, no runtime required)
```

## Known limitations

- **Mica** falls back to acrylic blur because the window uses `AllowsTransparency`.
- **Toasts** are in-app windows rather than native Windows toasts (avoids unpackaged-app AUMID registration); `NotificationService` is abstracted so native toasts can be swapped in later.
- **Virtual-desktop "follow" mode** is not implemented yet; the board stays fixed (planned for a later phase).
- **Font scale** is persisted but applied on a best-effort basis.
- **Focus-assist detection** reads the Do-Not-Disturb registry toggle (best-effort).
- **Auto-update** checks a version JSON; the feed URL is empty by default, so it reports "up to date" until configured.
- **Note Markdown** is stored but rendered as plain text for now.

## Roadmap

- **Next up (P1)** — timeline/Gantt views, WebDAV sync, efficiency/info/tool widgets, basic plugin system, embed-in-desktop layer
- **Later (P2)** — plugin marketplace, automation rules, collaboration, advanced security, accessibility, third-party import (Todoist/Trello/Microsoft To Do)

## License

MIT — see [LICENSE](LICENSE).

