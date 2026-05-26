$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectRoot = Join-Path $repoRoot 'DaylogDockExtension'
$globalJsonFile = Join-Path $repoRoot 'global.json'

function Assert-Contains {
    param(
        [string]$Path,
        [string]$Pattern,
        [string]$Message
    )

    $content = Get-Content -LiteralPath $Path -Raw
    if ($content -notmatch $Pattern) {
        throw $Message
    }
}

function Assert-NoSourceMatch {
    param(
        [string[]]$Patterns,
        [string]$Message
    )

    $files = Get-ChildItem -LiteralPath $repoRoot -Recurse -File |
        Where-Object {
            $_.Extension -in @('.cs', '.csproj', '.sln', '.xml') -and
            $_.FullName -notmatch '\\\.github\\' -and
            $_.FullName -notmatch '\\obj\\' -and
            $_.FullName -notmatch '\\bin\\' -and
            $_.FullName -notmatch '\\scripts\\' -and
            $_.FullName -notmatch '\\VALIDATION\.md$'
        }

    foreach ($file in $files) {
        $content = Get-Content -LiteralPath $file.FullName -Raw -ErrorAction SilentlyContinue
        foreach ($pattern in $Patterns) {
            if ($content -match $pattern) {
                throw "$Message Found '$pattern' in $($file.FullName)"
            }
        }
    }
}

$extensionFile = Join-Path $projectRoot 'DaylogDockExtension.cs'
$providerFile = Join-Path $projectRoot 'DaylogDockExtensionCommandsProvider.cs'
$manifestFile = Join-Path $projectRoot 'Package.appxmanifest'
$projectFile = Join-Path $projectRoot 'DaylogDockExtension.csproj'
$solutionFile = Join-Path $repoRoot 'DaylogDockExtension.sln'
$storeFile = Join-Path $projectRoot 'Services\DaylogMarkdownStore.cs'
$captureFile = Join-Path $projectRoot 'Pages\CapturePage.cs'
$historyFile = Join-Path $projectRoot 'Pages\HistoryPage.cs'
$commandsFile = Join-Path $projectRoot 'Commands\DaylogShellCommands.cs'
$smokeTestFile = Join-Path $repoRoot 'DaylogDockExtension.LogicSmokeTests\Program.cs'
$buildPropsFile = Join-Path $repoRoot 'Directory.Build.props'
$editorProjectFile = Join-Path $repoRoot 'DaylogDockExtension.Editor\DaylogDockExtension.Editor.csproj'
$editorProgramFile = Join-Path $repoRoot 'DaylogDockExtension.Editor\Program.cs'
$editorSingleInstanceFile = Join-Path $repoRoot 'DaylogDockExtension.Editor\EditorSingleInstance.cs'
$iconFile = Join-Path $projectRoot 'Assets\Daylog.ico'
$storeLogoFile = Join-Path $projectRoot 'Assets\StoreLogo.png'

Assert-Contains $extensionFile 'Guid\("0703ae8e-0be1-4335-804c-35a03e62c9cd"\)' 'Extension GUID missing or changed.'
Assert-Contains $manifestFile '<com:Class Id="0703ae8e-0be1-4335-804c-35a03e62c9cd"' 'COM class GUID mismatch.'
Assert-Contains $manifestFile '<CreateInstance ClassId="0703ae8e-0be1-4335-804c-35a03e62c9cd"' 'CreateInstance GUID mismatch.'
Assert-Contains $manifestFile 'Name="com.microsoft.commandpalette"' 'Command Palette app extension registration missing.'
Assert-Contains $manifestFile '<Commands/>' 'Command Palette commands provider interface missing from manifest.'

Assert-Contains $projectFile '<TargetFramework>net9\.0-windows10\.0\.26100\.0</TargetFramework>' 'Expected native Windows target framework missing.'
Assert-Contains $projectFile '<EnableMsixTooling>true</EnableMsixTooling>' 'MSIX tooling not enabled.'
Assert-Contains $projectFile '<ApplicationIcon>Assets\\Daylog\.ico</ApplicationIcon>' 'Host executable should use the Daylog icon.'
Assert-Contains $editorProjectFile '<ApplicationIcon>\.\.\\DaylogDockExtension\\Assets\\Daylog\.ico</ApplicationIcon>' 'Editor executable should use the Daylog icon.'
if (-not (Test-Path -LiteralPath $iconFile)) {
    throw 'Daylog.ico is missing from Assets.'
}

if (-not (Test-Path -LiteralPath $storeLogoFile)) {
    throw 'StoreLogo.png is missing from Assets.'
}

Assert-Contains $projectFile 'BuildAndCopyEditor' 'Extension build must bundle the WinUI editor executable.'
Assert-Contains $projectFile 'Targets="Publish"' 'Editor must be published self-contained into the extension package.'
Assert-Contains $editorProjectFile '<SelfContained>true</SelfContained>' 'Editor must be self-contained so .NET 9 is not required machine-wide.'
if ((Get-Content -LiteralPath $projectFile -Raw) -match '<UseWinUI>true') {
    throw 'WinUI must not be enabled on the Command Palette host project.'
}
Assert-Contains $projectFile '<AllowUnsafeBlocks>true</AllowUnsafeBlocks>' 'LibraryImport interop requires unsafe blocks enabled.'
Assert-Contains $projectFile 'Microsoft\.CommandPalette\.Extensions' 'Command Palette SDK package reference missing.'
Assert-Contains $buildPropsFile 'WindowsSdkPackageVersion' 'Shared Windows SDK version must be pinned for host and editor.'
Assert-Contains $editorProjectFile '<UseWinUI>true</UseWinUI>' 'WinUI editor project is not enabled.'
Assert-Contains $editorProjectFile 'Microsoft\.WindowsAppSDK' 'Windows App SDK package reference missing for native editor.'
Assert-Contains $editorProgramFile 'EditorSingleInstance\.TryClaim' 'Editor must enforce a single running instance.'
Assert-Contains $editorSingleInstanceFile 'ShowEventName' 'Editor single-instance foreground signal missing.'
Assert-Contains $editorSingleInstanceFile 'AllowSetForegroundWindow' 'Existing editor must be allowed to move to foreground.'
Assert-Contains $editorSingleInstanceFile 'IsAnotherInstanceRunning' 'Editor wake must use mutex, not process enumeration.'
if ((Get-Content -LiteralPath $editorSingleInstanceFile -Raw) -match 'GetProcessesByName') {
    throw 'EditorSingleInstance must not scan processes; that can freeze Command Palette.'
}
Assert-Contains $globalJsonFile '"version": "9\.0\.300"' 'global.json should pin the validated .NET SDK feature band.'
Assert-Contains $solutionFile 'DaylogDockExtension\.LogicSmokeTests\\DaylogDockExtension\.LogicSmokeTests\.csproj' 'Logic smoke test project is not in the solution.'

Assert-Contains $providerFile 'Id = "daylog\.dock"' 'Provider dock ID missing.'
Assert-Contains $providerFile 'GetDockBands\(\)' 'Explicit dock band override missing.'
Assert-Contains $providerFile 'GetCommandItem\(string id\)' 'Pinned dock settings must resolve the Daylog band through GetCommandItem.'
Assert-Contains $providerFile 'id == "daylog\.dock\.openEditor"' 'GetCommandItem must resolve the pinned Open Daylog dock command ID.'
Assert-Contains $providerFile 'DisplayName = "Daylog"' 'Provider display name should be Daylog.'
Assert-Contains $providerFile 'Title = DisplayName' 'Dock band title does not use provider display name.'
Assert-Contains $providerFile 'new OpenEditorCommand\(_state\)' 'Configured dock band does not launch the native editor.'
Assert-Contains $providerFile 'new OpenEditorCommand\(_state\)' 'Dock band must always invoke OpenEditorCommand at click time.'
Assert-Contains $commandsFile 'class OpenEditorCommand' 'OpenEditorCommand missing for dock band invoke.'
Assert-Contains $commandsFile '_state\.Settings\.Reload\(\)' 'Dock band click should reload settings in OpenEditorCommand.Invoke, not on every GetDockBands scan.'

Assert-Contains $commandsFile 'FolderPicker' 'Native folder picker missing.'
Assert-Contains $commandsFile 'LibraryImport\("user32\.dll"\)' 'Native folder picker window interop should use source-generated LibraryImport.'
Assert-Contains $commandsFile 'SetApartmentState\(System\.Threading\.ApartmentState\.STA\)' 'Native folder picker should run on an STA thread.'
Assert-Contains $commandsFile 'explorer\.exe' 'Reveal Folder shell action missing.'
Assert-Contains $commandsFile 'EnsureTodayFile\(\)' 'Open/reveal commands do not ensure today file.'

Assert-Contains $commandsFile 'class OpenEditorCommand' 'Native editor launch command missing.'
Assert-Contains $commandsFile 'DaylogDockExtension\.Editor\.exe' 'Extension must launch the separate editor executable.'
Assert-Contains $commandsFile 'ResolveEditorPath' 'Editor path resolver missing.'

$editorFile = Join-Path $projectRoot 'Editor\DaylogEditorWindow.cs'
Assert-Contains $editorFile 'TextChanged' 'WinUI editor does not autosave from text changes.'
Assert-Contains $editorFile 'DispatcherTimer' 'WinUI editor should debounce autosave.'
Assert-Contains $editorFile 'SaveDailyText\(_loadedDate, _editor\.Text\)' 'WinUI editor does not force-save current note text before navigation.'
Assert-Contains $editorFile 'SaveDailyTextAsync\(date, text\)' 'WinUI editor autosave should write asynchronously off the typing path.'
Assert-Contains $editorFile 'IsSpellCheckEnabled = false' 'WinUI editor should keep spellcheck highlighting off.'
Assert-Contains $editorFile 'IsTextPredictionEnabled = false' 'WinUI editor should keep text prediction highlighting off.'
Assert-Contains $editorFile '_status\.Visibility = Visibility\.Collapsed' 'WinUI editor should not show save-status text.'
Assert-Contains $editorFile 'SemaphoreSlim' 'WinUI editor should serialize sync and async saves to avoid stale overwrites.'
Assert-Contains $editorFile 'BuildUtilityRail' 'WinUI editor should expose the bottom utility rail.'
Assert-Contains $editorFile 'ToggleTheme' 'WinUI editor should include theme toggle.'
Assert-Contains $editorFile '_paperSurface' 'WinUI editor should host the writing surface.'
Assert-Contains $editorFile 'footerBackground' 'WinUI editor dark mode should use a darker footer bar.'
Assert-Contains $editorFile 'TextControlBackgroundFocused' 'WinUI editor dark mode must override TextBox internal focused background.'
Assert-Contains $editorFile 'SaveColorScheme' 'WinUI editor should persist color scheme like Freewrite.'
$settingsFile = Join-Path $projectRoot 'Services\DaylogSettings.cs'
Assert-Contains $settingsFile 'ColorScheme' 'Daylog settings should persist color scheme.'
Assert-Contains $settingsFile 'SaveColorScheme' 'Daylog settings store should save color scheme.'
Assert-Contains $editorFile 'ToggleCalendar' 'WinUI editor should include history calendar.'
if ((Get-Content -LiteralPath $editorFile -Raw) -match 'StartVoiceTyping|OpenChat|ToggleTimer|ToggleBackspace|ToggleFullscreen|InsertNewEntryMarker') {
    throw 'Freewrite-only utilities (talk, chat, timer, backspace, fullscreen, new entry) should not be in the daylog editor.'
}
Assert-Contains $editorFile 'RenderCalendar' 'WinUI editor should include a calendar jump surface.'
Assert-Contains $editorFile 'AcceptsReturn = true' 'WinUI editor should be multiline.'
Assert-Contains $editorFile 'ChooseFolderAsync' 'WinUI editor needs a subtle folder chooser.'
Assert-Contains $editorFile 'MoveDay\(-1\)' 'WinUI editor needs previous-day navigation.'
Assert-Contains $editorFile 'MoveDay\(1\)' 'WinUI editor needs next-day navigation.'
Assert-Contains $editorFile 'BringToFront\(\)' 'WinUI editor needs a foreground activation entrypoint.'
Assert-NoSourceMatch @('AgentDebugLog', '#region agent log') 'Temporary debug instrumentation should not remain in source.'

Assert-Contains $captureFile 'live autosave' 'Capture fallback does not mention native autosave editor.'
if ((Get-Content -LiteralPath $captureFile -Raw) -match 'FormContent|AdaptiveCard|"title":\s*"Save"') {
    throw 'Capture page still exposes the old CmdPal form with Save actions.'
}

Assert-Contains $storeFile 'Path\.Combine\(DaylogRoot, month, fileName\)' 'Default Daylog file layout missing.'
Assert-Contains $storeFile '## \{timestamp:HH:mm\}' 'Timestamp heading format missing.'
Assert-Contains $storeFile 'SaveDailyText\(DateOnly date, string body\)' 'Single daily note save method missing.'
Assert-Contains $storeFile 'SaveDailyTextAsync\(DateOnly date, string body\)' 'Async single daily note save method missing.'
Assert-Contains $storeFile 'FirstDailyFileDate\(\)' 'Past navigation needs a first-entry boundary.'
Assert-Contains $storeFile 'DailyFileDates\(\)' 'Calendar needs all saved daily file dates.'
Assert-Contains (Join-Path $projectRoot 'Services\DaylogState.cs') 'IsCalendarSelectable' 'Calendar selectability rules missing.'
Assert-Contains $historyFile 'RecentFiles\(14\)' 'History does not request 14 recent files.'
Assert-Contains $smokeTestFile 'finally' 'Smoke test should clean temp output in a finally block.'
Assert-Contains $smokeTestFile 'Clean\(root\)' 'Smoke test should clean temp Daylog root.'
Assert-Contains $smokeTestFile 'Clean\(settingsDirectory\)' 'Smoke test should clean temp settings directory.'
Assert-Contains $smokeTestFile '30-day future bound should be allowed' 'Smoke tests do not cover 30-day future navigation.'
Assert-Contains $smokeTestFile 'before-first-entry bound should fail' 'Smoke tests do not cover past navigation boundary.'
Assert-Contains $smokeTestFile 'async daily note save mismatch' 'Smoke tests do not cover async daily note save.'

Assert-NoSourceMatch @('Electron', 'Tauri', '\bReact\b', 'Next\.js', 'web wrapper') 'Non-native stack reference found in source.'
Assert-NoSourceMatch @(
    '\bAI chat\b',
    '\bvideo\b',
    '\bPDF export\b',
    '\bmarkdown renderer\b',
    '\bsearch\b',
    '\btags\b',
    '\bsync\b',
    '\bcalendar heatmap\b',
    '\bObsidian\b'
) 'MVP non-goal reference found in source.'

Write-Host 'Static Daylog Dock verification passed.'
