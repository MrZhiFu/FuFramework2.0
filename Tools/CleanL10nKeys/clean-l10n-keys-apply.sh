#!/usr/bin/env bash
# Delete unreferenced localization key rows from the Excel tables (--apply).
# Re-export is NOT done here: run gen-client-json.sh / gen-client-bin.sh when ready.
# Review the preview report first: bash Tools/CleanL10nKeys/clean-l10n-keys-preview.sh
python3 "$(dirname "$0")/src/CleanL10nKeys.py" --apply
