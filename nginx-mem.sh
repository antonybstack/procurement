#!/usr/bin/env bash
set -euo pipefail

# Summarize nginx memory usage (non-live).
# - Prints per-process RSS and a summed total (MiB).
# - Optional: pass --footprint to run `sudo footprint` for more accurate
#   physical footprint accounting (may prompt for password).

usage() {
  cat <<EOF
Usage: $(basename "$0") [--footprint]

Summarize nginx memory usage by summing RSS across nginx PIDs.
Options:
  --footprint   Use 'sudo footprint' on nginx PIDs for detailed accounting.
EOF
}

if [[ "${1:-}" == "-h" || "${1:-}" == "--help" ]]; then
  usage
  exit 0
fi

# Find nginx PIDs (master + workers)
PIDS_CSV=$(pgrep -f nginx | paste -sd, - || true)
PIDS_ARR=()

if [[ -n "${PIDS_CSV}" ]]; then
  # Build a space-separated array too (for footprint)
  while IFS= read -r pid; do
    [[ -n "$pid" ]] && PIDS_ARR+=("$pid")
  done < <(echo "$PIDS_CSV" | tr ',' '\n')
fi

if [[ -z "${PIDS_CSV}" ]]; then
  echo "No nginx processes found." >&2
  exit 2
fi

if [[ "${1:-}" == "--footprint" ]]; then
  if ! command -v footprint >/dev/null 2>&1; then
    echo "'footprint' not found. It's available on recent macOS versions." >&2
    exit 3
  fi
  echo "Running 'sudo footprint' on nginx PIDs: ${PIDS_ARR[*]}" >&2
  exec sudo footprint "${PIDS_ARR[@]}"
fi

echo "Per-process RSS (MiB) for nginx:" >&2
printf "%-6s %-6s %-10s %-10s %s\n" PID PPID RSS_KiB RSS_MiB CMD

# Use COMM for the display name; it's after the first three numeric fields.
ps -o pid=,ppid=,rss=,comm= -p "$PIDS_CSV" |
awk '{
  rss_kib=$3; rss_mib=rss_kib/1024;
  # command starts where field 4 begins
  cmd_idx = index($0, $4);
  cmd = substr($0, cmd_idx);
  printf "%-6s %-6s %-10s %10.2f %s\n", $1, $2, rss_kib, rss_mib, cmd;
}'

total_mib=$(ps -o rss= -p "$PIDS_CSV" | awk '{s+=$1} END{printf "%.2f", s/1024}')
count=$(echo "$PIDS_CSV" | awk -F, '{print NF}')

echo
echo "Processes: $count"
echo "Total RSS: ${total_mib} MiB"

