# Architecture

## Overview

A single-process WPF application. `App` is the composition root: it initializes services in dependency order inside `OnStartup` and exposes singletons through the static service locator `AppServices`.

```
App.OnStartup
 ├─ AppPaths.EnsureCreated / CrashLogService.Init
 ├─ AppServices.Initialize
 │   ├─ SettingsService.Load        (settings.json)
 │   ├─ Database.Open               (SQLite + migration + recovery + backup)
 │   ├─ TaskRepository              (task data access)
 │   ├─ NoteRepository              (note data access)
 │   └─ SystemMonitorService        (CPU/memory/disk/battery)
 ├─ MainWindow (floating bar + workbench, DataContext = MainViewModel)
 └─ StartServices
     ├─ NotificationService         (stacked toasts)
     ├─ HotkeyService               (RegisterHotKey)
     ├─ TrayIconService             (Shell_NotifyIcon + ContextMenu)
     ├─ ReminderService             (30 s polling for due reminders)
     ├─ FullscreenDetector          (foreground window covers a monitor)
     ├─ SessionLockDetector         (SystemEvents.SessionSwitch)
     └─ StickyNotes.RefreshAll      (restore pinned-note windows)
```

## Layers

| Layer | Location | Description |
|---|---|---|
| Presentation | `Controls/`, `MainWindow`, `ViewModels/`, `Converters/` | MVVM; controls reach the main VM through `UiContext.Main` |
| Services | `Services/` | No UI side effects; unit-testable |
| Data | `Data/` | SQLite connections, migration, backup, repositories |
| Domain | `Models/` | `TaskItem` / `Subtask` / `Note` / `AppSettings` / enums |
| Interop | `Interop/` | Win32 P/Invoke |

## Key design decisions

1. **Settings in JSON, data in SQLite** — settings stay readable even if the database is damaged; task/note data uses SQLite (WAL) for concurrency and transactions.
2. **Dates stored as `long` ticks** — avoids locale/time-zone ambiguity and keeps cross-language parsing stable.
3. **Native Win32 for tray and hotkeys** (not WinForms) — avoids the many name collisions (`Application`, `Color`, `Point`) between `System.Windows.Forms` and WPF.
4. **Component crash isolation (reserved)** — each UI component is a separate `UserControl`; the main process falls back to `DispatcherUnhandledException`. Plugins (later) follow a `mount → update → unmount` lifecycle, a 120×80 minimum size, and can only call host APIs.
5. **Database migration strategy** — `PRAGMA user_version` tracks the version; migrate with a backup (existing DBs only) → run in a transaction → rollback on failure; refuse to open a DB whose version is newer than the build.

## Data flow (task drag & drop)

```
TaskCard.MouseMove
  → DragDrop.DoDragDrop("KanbanTask", taskId)
  → KanbanColumn.OnDrop → compute insertion index
  → UiContext.Main.MoveTask(id, target, index)
  → in-memory reorder + TaskRepository.Reorder (persists SortOrder)
  → MainViewModel.Refreshed event → column headers update
```

## Reminder flow

```
ReminderService (every 30 s)
  → focus mode / Do-Not-Disturb check
  → TaskRepository.GetDueReminders(now)
  → merged notification or per-task toasts (snooze/complete/dismiss)
  → clear fired reminders + advance repeating tasks (preserving the lead time)
```

## Tests

`tests/KanbanBoard.Tests` (xUnit) covers natural-language parsing, import/export, the task repository, and the note repository (all against a temporary database). `Database` accepts an injected path so tests stay isolated.
