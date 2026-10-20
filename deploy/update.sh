#!/bin/sh
# Pull the latest Hearth images (and compose/Caddy config) and restart whatever
# changed. Run by hearth-update.timer on a deployed host; safe to run by hand.
# Expects the root .env to set HEARTH_IMAGE_PREFIX — see .env.example.
set -eu
cd "$(dirname "$0")/.."

# -f skips docker-compose.override.yml, which is dev-only
compose() { docker compose -f docker-compose.yml "$@"; }

old=$(git rev-parse HEAD)
git pull --ff-only --quiet
compose pull --quiet
# Compose only recreates containers whose image or config changed
compose up -d --no-build --remove-orphans
# The Caddyfile is a bind mount, which `up` doesn't watch
git diff --quiet "$old" HEAD -- Caddyfile || compose restart caddy
docker image prune -f >/dev/null
