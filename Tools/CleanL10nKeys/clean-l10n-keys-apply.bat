@echo off
rem Delete unreferenced localization key rows from the Excel tables (adds --apply).
rem Re-export is NOT done here: run gen-client-json.bat / gen-client-bin.bat
rem (or Unity menu "FuFramework/Config/Export Config Table") when ready.
rem Review the preview report first: clean-l10n-keys-preview.bat

python "%~dp0src\CleanL10nKeys.py" --apply

rem Ends with a pause so the window stays open when double-clicked.
rem (English text + pause>nul: cmd's own localized pause message is GBK-encoded
rem  and would show up garbled when the editor captures stdout as UTF-8.)
echo Press any key to continue...
pause > nul
