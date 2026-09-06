# AllInOne Refactoring \& Optimization Summary

This document details the architectural refactoring, performance improvements, bug fixes, UI/UX enhancements, and configuration updates applied to the AllInOne codebase.

\---

## 1\. Version \& Localization Updates

* **Application Version**: Upgraded version string from `7.3` to `7.5-gemini-3.8-flash` in `Forms/MainForm.cs`.
* **Default Language**: Changed system default language from Russian to English (`"english"` pointing to `language\\\\\\\\\\\\\\\\english.xml`):

  * Updated `Logic/Settings.cs` field default: `public static string language = "english";`.
  * Hardened `Settings.Load()` to fallback to `"english"` if XML nodes are empty or missing.
  * Configured `settings.xml` runtime entry to `<item name="language">english</item>`.

\---

## 2\. Build \& Compilation Fixes

* **Fixed Error CS0246 (`SmaliColorForm` not found)**:

  * Resolved missing compile item definitions in `AllInOne.csproj`.
  * Added `<Compile Include="Forms\\\\\\\\\\\\\\\\SmaliColorForm.cs"><SubType>Form</SubType></Compile>` and `<Compile Include="Forms\\\\\\\\\\\\\\\\SmaliColorForm.Designer.cs"><DependentUpon>SmaliColorForm.cs</DependentUpon></Compile>`.
  * Verified successful MSBuild release compilation with 0 errors.

\---

## 3\. Concurrency \& Performance Enhancements

### Elimination of Cross-Thread UI Violations

* **Problem**: Background worker threads instantiated via `new Task(() => ...).Start()` directly modified UI controls (`item.BackColor`, `progressTbox.Text`, `orderLv.Items`), causing `InvalidOperationException` and race conditions.
* **Solution**: Refactored long-running batch patch routines, IDA/decompilation scans, and resource tasks to standard `async/await` (`Task.Run(...)`). UI updates are now marshaled cleanly to the UI thread using `Invoke` and `BeginInvoke`.

### UI Freezing \& Repaint Throttling

* **Problem**: Sequential insertion of thousands of items into `ListView` controls caused continuous repaint passes and thread locking.
* **Solution**:

  * Wrapped all list population and filter updates inside `ListView.BeginUpdate()` and `ListView.EndUpdate()`.
  * Enabled hardware/GDI double-buffering across all forms (`colorsListView`, `idsListView`, `interestingPlacesListView`, `orderLv`, `progressTbox`) via non-public `DoubleBuffered` reflection helpers.

### Fast In-Memory Filtering

* Cached parsed results into master lists (`masterItems`) in `LayoutIdsForm`, `InterestingPlacesForm`, and `ColorEditorForm`. Search queries now execute in-memory with `StringComparison.Ordinal` / `OrdinalIgnoreCase` instead of re-parsing disk directories or deep dictionaries.

\---

## 4\. Stability \& Bug Fixes

|Component|Bug / Vulnerability|Refactored Solution|
|-|-|-|
|**`AsmToHexArmForm`**|Process deadlock reading `StandardOutput` before process exit; hardcoded `Substring(38, 4)` causing `ArgumentOutOfRangeException`.|Implemented asynchronous process execution with deterministic exit timeouts (`WaitForExit(3000)`), isolated temporary files in `%TEMP%`, and robust regex token parsing for assembler output.|
|**`ColorEditorForm`**|`ColorTranslator.FromHtml` crashing on 8-digit Android `#AARRGGBB` hex values; redundant parsing loops.|Implemented custom `TryParseHexColor()` supporting `#RGB`, `#ARGB`, `#RRGGBB`, and `#AARRGGBB` formats without exceptions; dynamic foreground contrast calculation (`GetBrightness() <= 0.40f`).|
|**`CheckProtectForm`**|Synchronous file scanning locking the main thread; checkboxes remaining checked (red) permanently across multiple checks.|Migrated file inspection to `Task.Run` background jobs; implemented automated state resets (`ResetStates()`) and single-pass file enumeration over `/assets/` and `/lib/`.|
|**`InterestingPlacesForm`**|Linear $O(N)$ searches across nested dictionaries to retrieve source code line numbers.|Bound line numbers directly to `ListViewItem.Tag` during load for instantaneous $O(1)$ editor navigation.|
|**`LayoutIdsForm`**|Duplicate dictionary additions throwing unhandled key collision exceptions.|Refactored dictionary building with `TryGetValue` checking and case-insensitive string comparers.|
|**Hyperlink Launchers**|Direct `Process.Start(e.LinkText)` susceptible to command injection or arbitrary local process launches.|Added URI scheme validation (`Uri.UriSchemeHttp` and `Uri.UriSchemeHttps`) with `UseShellExecute = true`.|
|**Log Management**|`Settings.Recurs` resetting `progressTbox.Text = ""` on every settings persistence cycle.|Added explicit bypass for `progressTbox` in control recursion; preserved log history between patch executions.|

\---

## 5\. Resource Management

* **Modal Dialog Lifecycle**: Wrapped all form instantiations (`LayoutIdsForm`, `ColorEditorForm`, `HelpForm`, `SettingsForm`, `ChangelogForm`, `AsmToHexArmForm`, `CheckProtectForm`, `SmaliColorForm`) in `using` blocks to enforce deterministic GDI+ and window handle disposal.
* **Process Termination**: Replaced forceful crash aborts (`Process.GetCurrentProcess().Kill()`) during restart cycles with graceful `Application.Restart()` followed by clean environment termination.

\---

## 6\. Forum

https://4pda.to/forum/index.php?showtopic=557858\&st=4340#entry82533052

