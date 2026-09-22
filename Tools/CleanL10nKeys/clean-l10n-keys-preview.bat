@echo off
rem Preview unreferenced localization keys (report only, Excel is NOT modified).
rem Usage: run directly, or via Unity menu "FuFramework/Config/Clean L10n Keys - Preview".

python "%~dp0src\CleanL10nKeys.py"

rem Ends with a pause so the window stays open when double-clicked.
rem (English text + pause>nul: cmd's own localized pause message is GBK-encoded
rem  and would show up garbled when the editor captures stdout as UTF-8.)
echo Press any key to continue...
pause > nul
