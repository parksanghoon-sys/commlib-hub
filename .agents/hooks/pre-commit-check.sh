#!/usr/bin/env bash
set -euo pipefail

echo "[hook] pre-commit validation"

if [ ! -f "./docs/current-plan.md" ]; then
  echo "current-plan.md missing"
  exit 1
fi

if git diff --cached --name-only | grep -E "src/CommLib.Domain|src/CommLib.Application" >/dev/null 2>&1; then
  if ! grep -q "Architecture Review" ./docs/current-plan.md; then
    echo "Architecture Review section missing in current-plan.md"
    exit 1
  fi
fi

echo "[hook] ok"
