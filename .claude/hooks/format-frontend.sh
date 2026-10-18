#!/usr/bin/env bash
# PostToolUse hook: run Prettier on frontend files Claude just edited or wrote.
# Formatting failures never block the edit — CI's format:check is the backstop.
set -u

file=$(jq -r '.tool_input.file_path // empty')
[ -z "$file" ] && exit 0

frontend="$CLAUDE_PROJECT_DIR/frontend"
case "$file" in
    "$frontend"/node_modules/*) exit 0 ;;
    "$frontend"/*.svelte | "$frontend"/*.ts | "$frontend"/*.js | "$frontend"/*.css | "$frontend"/*.json) ;;
    *) exit 0 ;;
esac

cd "$frontend" && npx --no-install prettier --write --log-level warn "$file" >&2 || true
exit 0
