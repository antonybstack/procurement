#!/usr/bin/env bash
set -euo pipefail

# Launch htop focused on current nginx processes.
# Falls back with a clear message if no nginx is running.

if ! command -v htop >/dev/null 2>&1; then
  echo "htop is not installed. Install with: brew install htop" >&2
  exit 1
fi

# Prefer matching master + workers; -f catches full command lines.
PIDS=$(pgrep -f nginx | paste -sd, - || true)

if [[ -z "${PIDS}" ]]; then
  echo "No nginx processes found." >&2
  exit 2
fi

echo "Attaching htop to nginx PIDs: ${PIDS}" >&2
exec htop -p "${PIDS}"

