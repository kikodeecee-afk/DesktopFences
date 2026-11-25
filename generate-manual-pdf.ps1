$ErrorActionPreference = "Stop"

$OutPdf = "c:\Users\kikod\Downloads\BAGO RESTO DELIVERY\DesktopFences\Read me Fence Tutorial and Functionality.pdf"
if (Test-Path $OutPdf) {
    Set-ItemProperty -Path $OutPdf -Name IsReadOnly -Value $false -ErrorAction SilentlyContinue
    Remove-Item $OutPdf -Force
}

function Escape-PdfText([string]$s) {
    $s = $s.Replace('\', '\\').Replace('(', '\(').Replace(')', '\)')
    return $s
}

# Helper to build PDF content streams for clean 2-page Letter (612 x 792 pt) Reference Manual
function New-PageStreamBuilder {
    return New-Object System.Text.StringBuilder
}

function Add-Rect($sb, [double]$x, [double]$y, [double]$w, [double]$h, [double]$r, [double]$g, [double]$b) {
    [void]$sb.AppendLine(("{0:F3} {1:F3} {2:F3} rg {3:F1} {4:F1} {5:F1} {6:F1} re f" -f $r, $g, $b, $x, $y, $w, $h))
}

function Add-StrokeRect($sb, [double]$x, [double]$y, [double]$w, [double]$h, [double]$r, [double]$g, [double]$b, [double]$lw = 0.8) {
    [void]$sb.AppendLine(("{0:F2} w {1:F3} {2:F3} {3:F3} RG {4:F1} {5:F1} {6:F1} {7:F1} re S" -f $lw, $r, $g, $b, $x, $y, $w, $h))
}

function Add-Text($sb, [string]$font, [double]$size, [double]$x, [double]$y, [double]$r, [double]$g, [double]$b, [string]$text) {
    $esc = Escape-PdfText $text
    [void]$sb.AppendLine(("BT /{0} {1:F1} Tf {2:F3} {3:F3} {4:F3} rg {5:F1} {6:F1} Td ({7}) Tj ET" -f $font, $size, $r, $g, $b, $x, $y, $esc))
}

# ==================== PAGE 1 ====================
$p1 = New-PageStreamBuilder

# Page background (Crisp Slate White)
Add-Rect $p1 0 0 612 792 0.985 0.988 0.995

# Top Navy Blue Header Banner
Add-Rect $p1 0 712 612 80 0.118 0.227 0.541
Add-Rect $p1 0 708 612 4 0.055 0.647 0.914
Add-Text $p1 "F2" 18 36 756 1 1 1 "DESKTOP FENCES - TUTORIAL & FUNCTIONALITY GUIDE"
Add-Text $p1 "F1" 10 36 736 0.82 0.89 0.99 "Official Read-Only User Manual  |  Windows 10 & Windows 11 Edition  |  Zero-Duplicate Organizer"
Add-Text $p1 "F1" 8.5 36 720 0.68 0.80 0.96 "Document Status: Uneditable Reference Manual  |  Page 1 of 2"

# Section 1: What Desktop Fences Does (Functionality Overview)
$y = 682
Add-Text $p1 "F2" 12.5 36 $y 0.118 0.227 0.541 "1. CORE FUNCTIONALITY & HOW IT WORKS"
Add-Rect $p1 36 ($y - 5) 540 1.2 0.118 0.227 0.541

Add-Rect $p1 36 576 540 94 0.94 0.96 0.99
Add-StrokeRect $p1 36 576 540 94 0.78 0.84 0.94 0.8

$bullets1 = @(
    "- 100% Untouched Native Windows Desktop: Never hides or overlays your native desktop wallpaper or icons.",
    "- Zero-Duplicate Instant Transfer: Dragging icons into a fence MOVES them cleanly (never creates duplicates).",
    "- Transparent Hover-Reveal Theme: Borders, header bar, and scrollbars stay 100% invisible on your wallpaper",
    "  until you hover your cursor inside a specific fence box. Also includes 10 rich color themes + Dark/Light mode.",
    "- Two-Way Sync & Safe Restore: Deleting a fence or clicking 'Restore All Icons' returns all files to Desktop.",
    "-Survives Win + D (Show Desktop): Fences stay pinned to your desktop layer behind regular app windows."
)
$by = 655
foreach ($line in $bullets1) {
    $clean = $line.Replace("-Survives", "- Survives")
    Add-Text $p1 "F1" 9.3 46 $by 0.12 0.16 0.23 $clean
    $by -= 13.5
}

# Section 2: LEFT-CLICK MOUSE CONTROLS
$y = 552
Add-Text $p1 "F2" 12.5 36 $y 0.118 0.227 0.541 "2. LEFT-CLICK MOUSE CONTROLS (MOVING, LAUNCHING & SELECTING)"
Add-Rect $p1 36 ($y - 5) 540 1.2 0.118 0.227 0.541

# Table Header
Add-Rect $p1 36 524 540 20 0.15 0.28 0.62
Add-Text $p1 "F2" 9.5 44 530 1 1 1 "LEFT-CLICK ACTION"
Add-Text $p1 "F2" 9.5 235 530 1 1 1 "WHAT IT DOES"

$leftRows = @(
    @("Single Left-Click (on Icon)", "Selects that icon inside the fence."),
    @("Double Left-Click (on Icon)", "Launches / opens the application, shortcut, file, or folder."),
    @("Double Left-Click (Fence Title)", "Opens inline text editor to rename the fence title (press Enter to save)."),
    @("Hold Left-Click + Drag (Empty Box)", "Moves the entire Fence Frame smoothly from ANY empty spot inside the box."),
    @("Hold Left-Click + Drag (Header Bar)", "Moves the Fence Frame with automatic magnetic screen & fence snapping."),
    @("Hold Left-Click + Drag (on Icon)", "Moves the selected icon (or ALL selected icons) to another fence or Desktop."),
    @("Hold Left-Click + Drag (Outer Edges)", "Resizes the fence 1:1 from any of the 8 outer borders or corners."),
    @("Ctrl + Left-Click (on Icons)", "Toggles individual icons ON/OFF to build a custom multi-icon selection."),
    @("Shift + Left-Click (Click or Drag)", "Selects a continuous range of icons OR draws a multi-select marquee box.")
)

$ry = 504
$alt = $false
foreach ($row in $leftRows) {
    if ($alt) { Add-Rect $p1 36 ($ry - 4) 540 20 0.945 0.962 0.988 }
    else      { Add-Rect $p1 36 ($ry - 4) 540 20 1.0 1.0 1.0 }
    Add-StrokeRect $p1 36 ($ry - 4) 540 20 0.85 0.89 0.94 0.5
    Add-Text $p1 "F2" 8.8 44 ($ry + 3) 0.10 0.18 0.35 $row[0]
    Add-Text $p1 "F1" 8.8 235 ($ry + 3) 0.15 0.20 0.28 $row[1]
    $ry -= 20
    $alt = -not $alt
}

# Section 3: RIGHT-CLICK MOUSE CONTROLS
$y = 304
Add-Text $p1 "F2" 12.5 36 $y 0.118 0.227 0.541 "3. RIGHT-CLICK MOUSE CONTROLS (MULTI-SELECT, SORT & LOCK)"
Add-Rect $p1 36 ($y - 5) 540 1.2 0.118 0.227 0.541

Add-Rect $p1 36 276 540 20 0.15 0.28 0.62
Add-Text $p1 "F2" 9.5 44 282 1 1 1 "RIGHT-CLICK ACTION"
Add-Text $p1 "F2" 9.5 235 282 1 1 1 "WHAT IT DOES"

$rightRows = @(
    @("Hold Right-Click + Drag (Inside Box)", "Draws glowing Marquee Selection Box to select multiple icons at once."),
    @("Shift + Hold Right-Click + Drag", "Draws Marquee Selection Box while ADDING to your already-selected icons."),
    @("Right-Click Empty Space / Header", "Opens Fence Menu: Sort by, Hold/Lock Movement, New Fence, Color Themes."),
    @("Right-Click -> Sort by", "Option buttons to auto-sort by Name, Size, Item Type, or Date Modified."),
    @("Right-Click -> Hold / Lock Position", "Pins fence in place (or sets 0.35s Hold Delay) to stop accidental dragging."),
    @("Right-Click (on an Icon)", "Opens Item Menu: Open, Run as Admin, Move to Desktop/Fence, Delete, Props."),
    @("Right-Click System Tray Icon", "Opens Global Menu: New Fence, Transparent Theme, Dark/Light, Restore All.")
)

$ry = 256
$alt = $false
foreach ($row in $rightRows) {
    if ($alt) { Add-Rect $p1 36 ($ry - 4) 540 22 0.945 0.962 0.988 }
    else      { Add-Rect $p1 36 ($ry - 4) 540 22 1.0 1.0 1.0 }
    Add-StrokeRect $p1 36 ($ry - 4) 540 22 0.85 0.89 0.94 0.5
    Add-Text $p1 "F2" 8.8 44 ($ry + 4) 0.10 0.18 0.35 $row[0]
    Add-Text $p1 "F1" 8.8 235 ($ry + 4) 0.15 0.20 0.28 $row[1]
    $ry -= 22
    $alt = -not $alt
}

# Footer Page 1
Add-Rect $p1 36 34 540 0.8 0.78 0.82 0.88
Add-Text $p1 "F1" 8.5 36 20 0.42 0.47 0.55 "Desktop Fences User Manual & Tutorial  -  Continue to Page 2 for Header Controls, Themes, Hotkeys & Step-by-Step Guide"


# ==================== PAGE 2 ====================
$p2 = New-PageStreamBuilder
Add-Rect $p2 0 0 612 792 0.985 0.988 0.995

# Top Header Banner Page 2
Add-Rect $p2 0 726 612 66 0.118 0.227 0.541
Add-Rect $p2 0 722 612 4 0.055 0.647 0.914
Add-Text $p2 "F2" 15.5 36 756 1 1 1 "DESKTOP FENCES - OPERATION, THEMES & SHORTCUTS"
Add-Text $p2 "F1" 9.5 36 736 0.82 0.89 0.99 "Step-by-Step Operation Guide, Header Buttons, Movement Modes & Keyboard Shortcuts  |  Page 2 of 2"

# Section 4: Step-by-Step How to Operate
$y = 694
Add-Text $p2 "F2" 12.5 36 $y 0.118 0.227 0.541 "4. STEP-BY-STEP HOW TO OPERATE YOUR FENCES"
Add-Rect $p2 36 ($y - 5) 540 1.2 0.118 0.227 0.541

Add-Rect $p2 36 544 540 138 0.94 0.96 0.99
Add-StrokeRect $p2 36 544 540 138 0.78 0.84 0.94 0.8

$stepsText = @(
    "Step 1 - Organize Icons (Drag & Drop): Select one or multiple icons on your Windows Desktop and drag them",
    "         into any fence box. They move instantly with zero duplicates. Drag out to Desktop anytime to return.",
    "Step 2 - Hover to Reveal Controls: In 'Transparent' theme, fences stay completely invisible over your wallpaper.",
    "         Simply move your cursor inside a box to pop out its glowing border, title bar, and top-right buttons.",
    "Step 3 - Prevent Accidental Dragging: Right-click inside a fence -> check 'Hold / Lock Fence Position', OR",
    "         open 'Hold & Movement Setting' and pick 'Hold Left-Click (0.35s Delay) to Move Box'.",
    "Step 4 - Auto-Sort Icons: Right-click inside any fence -> 'Sort by' -> pick Name, Size, Item Type, or",
    "         Date Modified (or open 'Auto-Sort Settings...' for the interactive Option Buttons panel).",
    "Step 5 - Collapse / Shade a Fence: Click the arrow button on the top-right of a fence header to roll it up",
    "         into a compact strip; hovering over the strip smoothly expands your icons on demand."
)
$sy = 667
foreach ($sLine in $stepsText) {
    $fontChoice = if ($sLine.StartsWith("Step ")) { "F2" } else { "F1" }
    Add-Text $p2 $fontChoice 9.0 46 $sy 0.12 0.16 0.24 $sLine
    $sy -= 13.0
}

# Section 5: Fence Header Buttons & Color Themes
$y = 520
Add-Text $p2 "F2" 12.5 36 $y 0.118 0.227 0.541 "5. FENCE HEADER BAR BUTTONS & 11 BUILT-IN THEMES"
Add-Rect $p2 36 ($y - 5) 540 1.2 0.118 0.227 0.541

Add-Rect $p2 36 418 540 90 1 1 1
Add-StrokeRect $p2 36 418 540 90 0.82 0.87 0.93 0.8

$themeLines = @(
    "- Palette Button (Top-Right): Switch per-fence themes: Transparent (Hover Reveal), Navy Blue, Rose Pink,",
    "  Midnight Ocean, Obsidian Slate, Emerald Moss, Royal Amethyst, Sunset Amber, Crimson Ruby, Nordic Ice,",
    "  Stealth Glass, or enter any Custom Hex Color (#RRGGBB).",
    "- Dark / Light Pill Switch (Top-Right): Instantly toggles all fences between Dark Glass and Light Glass.",
    "- Roll-Up Arrow Button (Top-Right): Collapses the fence to a thin bar; expands automatically on mouse hover.",
    "- Auto-Hiding Slim Scrollbar: Appears as a subtle 6px pill ONLY while hovering inside an overflowing fence."
)
$ty = 493
foreach ($tLine in $themeLines) {
    Add-Text $p2 "F1" 9.1 46 $ty 0.13 0.18 0.26 $tLine
    $ty -= 13.5
}

# Section 6: Keyboard Shortcuts & Global Hotkeys
$y = 392
Add-Text $p2 "F2" 12.5 36 $y 0.118 0.227 0.541 "6. KEYBOARD SHORTCUTS & GLOBAL HOTKEYS"
Add-Rect $p2 36 ($y - 5) 540 1.2 0.118 0.227 0.541

Add-Rect $p2 36 364 540 20 0.15 0.28 0.62
Add-Text $p2 "F2" 9.5 44 370 1 1 1 "SHORTCUT KEY"
Add-Text $p2 "F2" 9.5 235 370 1 1 1 "FUNCTION"

$hotkeys = @(
    @("Ctrl + Alt + N  (Global)", "Creates a brand-new Fence anywhere on your desktop."),
    @("Ctrl + Alt + H  (Global)", "Quick-Hides or Shows all fences for a 100% clean wallpaper view."),
    @("Ctrl + Alt + Space  (Global)", "Peek Mode: Temporarily brings all fences ON TOP of open windows."),
    @("Ctrl + A  (Inside Fence)", "Selects all icons inside the active fence."),
    @("Enter  /  Alt + Enter", "Enter launches selected icon(s); Alt + Enter opens Windows Properties."),
    @("F2  (Inside Fence)", "Renames the selected file, folder, or shortcut directly on disk."),
    @("Delete  /  Shift + Delete", "Sends selected item(s) to Recycle Bin (or Shift+Delete permanently)."),
    @("Esc  (Inside Fence)", "Clears current multi-icon selection or cancels an active drag/resize.")
)

$ky = 344
$alt = $false
foreach ($row in $hotkeys) {
    if ($alt) { Add-Rect $p2 36 ($ky - 4) 540 20 0.945 0.962 0.988 }
    else      { Add-Rect $p2 36 ($ky - 4) 540 20 1.0 1.0 1.0 }
    Add-StrokeRect $p2 36 ($ky - 4) 540 20 0.85 0.89 0.94 0.5
    Add-Text $p2 "F3" 8.8 44 ($ky + 3) 0.10 0.18 0.35 $row[0]
    Add-Text $p2 "F1" 8.8 235 ($ky + 3) 0.15 0.20 0.28 $row[1]
    $ky -= 20
    $alt = -not $alt
}

# Safety Note Box at bottom of Page 2
Add-Rect $p2 36 112 540 56 0.92 0.97 0.94
Add-StrokeRect $p2 36 112 540 56 0.16 0.62 0.42 0.9
Add-Text $p2 "F2" 9.8 46 151 0.06 0.42 0.24 "SAFE FILE HANDLING GUARANTEE"
Add-Text $p2 "F1" 8.9 46 136 0.12 0.24 0.18 "Your files are never locked into a proprietary database. Deleting a fence, clicking 'Restore All Fenced Icons to"
Add-Text $p2 "F1" 8.9 46 123 0.12 0.24 0.18 "Desktop' in the System Tray, or running the Uninstaller moves every fenced item back to your Windows Desktop."

Add-Rect $p2 36 34 540 0.8 0.78 0.82 0.88
Add-Text $p2 "F1" 8.5 36 20 0.42 0.47 0.55 "Desktop Fences  |  Read-Only Tutorial & Functionality Guide  |  Windows 10 & Windows 11"

# ==================== ASSEMBLE VALID PDF 1.4 BINARY ====================
$enc = [System.Text.Encoding]::GetEncoding("iso-8859-1")
$s1Bytes = $enc.GetBytes($p1.ToString())
$s2Bytes = $enc.GetBytes($p2.ToString())

$ms = New-Object System.IO.MemoryStream
function Write-PdfRaw([byte[]]$b) { $ms.Write($b, 0, $b.Length) }
function Write-PdfStr([string]$s) { Write-PdfRaw ($enc.GetBytes($s)) }

Write-PdfStr "%PDF-1.4`n"
$offsets = @{}

# 1: Catalog
$offsets[1] = $ms.Position
Write-PdfStr "1 0 obj`n<< /Type /Catalog /Pages 2 0 R >>`nendobj`n"

# 2: Pages
$offsets[2] = $ms.Position
Write-PdfStr "2 0 obj`n<< /Type /Pages /Kids [3 0 R 4 0 R] /Count 2 >>`nendobj`n"

# 3: Page 1
$offsets[3] = $ms.Position
Write-PdfStr "3 0 obj`n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 5 0 R /F2 6 0 R /F3 7 0 R >> >> /Contents 8 0 R >>`nendobj`n"

# 4: Page 2
$offsets[4] = $ms.Position
Write-PdfStr "4 0 obj`n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 5 0 R /F2 6 0 R /F3 7 0 R >> >> /Contents 9 0 R >>`nendobj`n"

# 5, 6, 7: Standard Fonts (Helvetica, Helvetica-Bold, Courier)
$offsets[5] = $ms.Position
Write-PdfStr "5 0 obj`n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>`nendobj`n"

$offsets[6] = $ms.Position
Write-PdfStr "6 0 obj`n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>`nendobj`n"

$offsets[7] = $ms.Position
Write-PdfStr "7 0 obj`n<< /Type /Font /Subtype /Type1 /BaseFont /Courier-Bold /Encoding /WinAnsiEncoding >>`nendobj`n"

# 8: Content Stream Page 1
$offsets[8] = $ms.Position
Write-PdfStr ("8 0 obj`n<< /Length {0} >>`nstream`n" -f $s1Bytes.Length)
Write-PdfRaw $s1Bytes
Write-PdfStr "`nendstream`nendobj`n"

# 9: Content Stream Page 2
$offsets[9] = $ms.Position
Write-PdfStr ("9 0 obj`n<< /Length {0} >>`nstream`n" -f $s2Bytes.Length)
Write-PdfRaw $s2Bytes
Write-PdfStr "`nendstream`nendobj`n"

# 10: Info Dictionary
$offsets[10] = $ms.Position
Write-PdfStr "10 0 obj`n<< /Title (Read me Fence Tutorial and Functionality) /Author (Desktop Fences) /Subject (User Manual & Mouse Controls Reference) >>`nendobj`n"

$xrefPos = $ms.Position
Write-PdfStr "xref`n0 11`n"
Write-PdfStr "0000000000 65535 f `n"
for ($i = 1; $i -le 10; $i++) {
    Write-PdfStr ("{0:D10} 00000 n `n" -f $offsets[$i])
}
Write-PdfStr ("trailer`n<< /Size 11 /Root 1 0 R /Info 10 0 R >>`nstartxref`n{0}`n%%EOF`n" -f $xrefPos)

[System.IO.File]::WriteAllBytes($OutPdf, $ms.ToArray())
$ms.Dispose()

# Mark file as Read-Only so it cannot be overwritten or edited!
Set-ItemProperty -Path $OutPdf -Name IsReadOnly -Value $true
Write-Host "Generated Read-Only PDF: $OutPdf ($((Get-Item $OutPdf).Length) bytes)" -ForegroundColor Green
