# Desktop Fences (Windows 10 & Windows 11)

A lightweight, zero-dependency Windows desktop icon organizer built in C# + WPF + Win32 P/Invoke. Group desktop shortcuts, apps, games, and folders into sleek customizable fences—including a **Transparent Hover-Reveal** theme—while keeping your native Windows desktop 100% untouched.

## Key Features
- **100% Untouched Native Windows Desktop:** No full-screen overlay and no hidden `SysListView32` layer. Native Windows drag-select, wallpapers, and shortcuts work natively.
- **Zero-Duplicate Instant Drag-and-Drop (`< 1ms` OLE Release):** Drag one or multiple icons from your desktop into any fence to move them cleanly with zero duplicates; drag them back out onto your desktop anytime.
- **✨ Transparent (Hover-Reveal) Theme + 10 Dual-Mode Color Themes:**
  - **Transparent (Hover Reveal):** Borders, backdrop, header bar, and scrollbars stay 100% invisible so your desktop wallpaper shines through cleanly—popping out smoothly only when your cursor enters a specific fence box.
  - **Built-in Color Themes:** *Navy Blue*, *Rose Pink*, *Midnight Ocean*, *Obsidian Slate*, *Emerald Moss*, *Royal Amethyst*, *Sunset Amber*, *Crimson Ruby*, *Nordic Ice*, *Stealth Glass*, plus custom Hex colors and a compact **Dark / Light Mode** pill toggle.
- **256×256 High-Definition True-Color Icons:** Extracts crisp 32-bit ARGB Jumbo icons directly from Windows Shell (`SHGetImageList` `SHIL_JUMBO` / `SHIL_EXTRALARGE`).
- **Multi-Icon Marquee Selection:** Hold **Left-Click**, **Right-Click**, or **`Shift + Right-Click`** and drag inside any fence to rubber-band select multiple icons at once (plus `Ctrl + Click`, `Shift + Click`, and `Ctrl + A`).
- **Per-Fence Auto-Sort (`Sort by`):** Right-click inside any fence to sort by **Name**, **Size**, **Item Type**, or **Date Modified** (Ascending / Descending or Manual Order).
- **Anchor-Based 1:1 DPI-Safe Resizing & Magnetic Snapping:** Smooth 8-way edge/corner resizing, Roll-Up shade mode, Quick-Hide (`Ctrl + Alt + H`), Peek on Top (`Ctrl + Alt + Space`), and Quick New Fence (`Ctrl + Alt + N`).

## Quick Download & Install (Zero Dependencies)
 inside `Release/`:
1. **One-Click Installer:** Run `DesktopFences-Setup-v1.0.0-win-x64.exe` (installs per-user with no admin UAC required, creates Start Menu & Desktop shortcuts, and registers in Windows Installed Apps).
2. **Portable Edition:** Extract `DesktopFences-Portable-v1.0.0-win-x64.zip` anywhere and run `DesktopFences.exe`.

## Building from Source (Zero SDK Install Required)
Uses the built-in Windows C# compiler (`csc.exe`) included on every Windows 10 and Windows 11 PC:

```powershell
powershell -ExecutionPolicy Bypass -File .\build-release.ps1
```
