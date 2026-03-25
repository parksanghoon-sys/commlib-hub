#!/usr/bin/env bash
set -euo pipefail

echo "[hook] skill gate check"
test -f "./AGENT.md"
test -f "./docs/current-plan.md"
ls -1 ./.agents/skills >/dev/null
echo "[hook] ok"
