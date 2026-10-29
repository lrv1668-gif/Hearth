#!/bin/sh
# Set the login for /logs (Dozzle): writes infra/dozzle/users.yml with a bcrypt hash of the
# password and restarts Dozzle. Run on the host, from anywhere: infra/deploy/dozzle-user.sh <name>
# Running it again replaces the login. Dozzle only runs with COMPOSE_PROFILES=logs in
# infra/.env; without it the login is saved for when it's turned on.
set -eu
cd "$(dirname "$0")/.."

[ $# -eq 1 ] || { echo "usage: $0 <username>" >&2; exit 1; }

printf 'Password for %s: ' "$1" >&2
stty -echo
trap 'stty echo' EXIT
read -r password
stty echo
echo >&2
[ -n "$password" ] || { echo "The password can't be empty" >&2; exit 1; }

# -f skips docker-compose.override.yml, which is dev-only; the image comes from the compose file
compose() { docker compose -f docker-compose.yml "$@"; }

mkdir -p dozzle
# Naming the service runs it even with its profile off — only `generate`, nothing stays up
printf '%s\n' "$password" | compose run --rm -T --no-deps dozzle generate "$1" > dozzle/users.yml.tmp
mv dozzle/users.yml.tmp dozzle/users.yml

if compose config --services | grep -qx dozzle; then
    compose up -d --no-build dozzle
    compose restart dozzle
    echo "Saved. Sign in at /logs as $1." >&2
else
    echo "Saved. Logs are off: add COMPOSE_PROFILES=logs to infra/.env, then run infra/deploy/update.sh." >&2
fi
