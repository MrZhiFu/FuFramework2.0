#!/usr/bin/env bash
# Preview unreferenced localization keys (report only, Excel is NOT modified).
# Usage: bash Tools/CleanL10nKeys/clean-l10n-keys-preview.sh
python3 "$(dirname "$0")/src/CleanL10nKeys.py"
