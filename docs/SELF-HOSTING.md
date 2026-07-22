# Self-Hosting Hearth from Published Images

This guide is for running Hearth from prebuilt images on GitHub Container
Registry, without cloning the repository or building anything locally. If
you're developing Hearth itself, use the [main README](../README.md) instead
— it covers `docker compose watch` / `docker compose up --build` from source.

Published images are built for `linux/amd64` and `linux/arm64` (Raspberry Pi
5 and similar) from tagged releases. See available tags and images at
`https://github.com/lrv1668-gif/Hearth/pkgs/container/hearth-tasks` (and the
sibling `hearth-<service>` packages).

## 1. Get the files

You don't need the full repo — just three things, from a tagged release:

- [`docker-compose.ghcr.yml`](../docker-compose.ghcr.yml)
- [`Caddyfile`](../Caddyfile)
- [`.env.example`](../.env.example) (stack-wide config)

Put them in an empty directory. Copy `.env.example` to `.env`.

## 2. Set up service configuration

Some services need their own credentials or location data, supplied as flat
`<service>.env` files next to `docker-compose.ghcr.yml` (referenced by its
`env_file:` entries). Use the `.env.example` under each service's directory
in the repo as a template for what each file needs:

| Service | File | Needs | Template |
|---|---|---|---|
| Weather | `weather.env` | `LATITUDE`, `LONGITUDE` | [`src/Weather/.env.example`](../src/Weather/.env.example) |
| Spotify | `spotify.env` | Spotify app credentials — see below | [`src/Spotify/.env.example`](../src/Spotify/.env.example) |
| Calendar | `calendar.env` | Google OAuth credentials — see below | [`src/Calendar/.env.example`](../src/Calendar/.env.example) |
| Photos | `photos.env` | `UNSPLASH_ACCESS_KEY` | [`src/Photos/.env.example`](../src/Photos/.env.example) |
| Birds | `birds.env` | `EBIRD_API_KEY`, `LATITUDE`, `LONGITUDE` | [`src/Birds/.env.example`](../src/Birds/.env.example) |
| Almanac | `almanac.env` | optional `LATITUDE`/`LONGITUDE`/`TZ`/frost dates | [`src/Almanac/.env.example`](../src/Almanac/.env.example) |

Tasks, RSS, and Quote need no configuration.

### Third-party credentials are yours to obtain

Hearth doesn't ship with shared API keys or OAuth apps — each self-hoster
registers their own, scoped to their own deployment:

- **Spotify** — create an app at [developer.spotify.com](https://developer.spotify.com) to get a client ID/secret.
- **Google Calendar** — create an OAuth client at [Google Cloud Console](https://console.cloud.google.com/apis/credentials) (see `src/Calendar/.env.example` for details).
- **Unsplash (Photos)** — create an app at [unsplash.com/developers](https://unsplash.com/developers).
- **eBird (Birds)** — request an API key at [ebird.org](https://ebird.org/api/keygen).

These are all optional — omit a service's `env_file` entry and it degrades
gracefully or isn't used by the frontend.

## 3. Configure the domain (if not localhost)

By default, `FRONTEND_URL` in `.env` is `http://localhost`, and `Caddyfile`
listens on bare `:80` with no TLS — fine for localhost or LAN use.

For a real domain with automatic HTTPS, edit `.env`:

```
FRONTEND_URL=https://dashboard.example.com
```

and update `Caddyfile` to use a domain block instead of `:80`:

```caddyfile
dashboard.example.com {
    handle /tasks* { reverse_proxy tasks:8081 }
    handle /spotify* { reverse_proxy spotify:8083 }
    handle /weather* { reverse_proxy weather:8082 }
    handle /photos* { reverse_proxy photos:8084 }
    handle /rss* { reverse_proxy rss:8085 }
    handle /calendar/google* { reverse_proxy calendar:8087 }
    handle /calendar/items* { reverse_proxy calendar:8087 }
    handle /quote* { reverse_proxy quote:8086 }
    handle /birds* { reverse_proxy birds:8088 }
    handle /almanac* { reverse_proxy almanac:8089 }
    handle { reverse_proxy frontend:3000 }
}
```

Caddy issues and renews the certificate automatically via Let's Encrypt —
this requires DNS for the domain to point at the host, and ports 80 and 443
both reachable from the internet. Certificates are stored in the `caddy-data`
volume, which must persist across restarts (don't run `docker compose down
-v`) — reissuing on every restart can trip Let's Encrypt's rate limits.

If you set `FRONTEND_URL` to a real domain, also update `SPOTIFY_REDIRECT_URI`
(in `spotify.env`) and `GOOGLE_REDIRECT_URI` (in `calendar.env`) to use that
same domain, and register the exact same callback URL in the Spotify
Developer dashboard and Google Cloud OAuth console — the app-side redirect
URI must match what's registered there exactly, or the OAuth flow fails.

## 4. Pick a version and start

```
HEARTH_VERSION=v0.1.0
```

in `.env` (or leave as `latest` to track the newest tag). Then:

```bash
docker compose -f docker-compose.ghcr.yml up -d
```

Using `-f` explicitly matters here — without it, Compose looks for
`docker-compose.override.yml`, which doesn't exist in this download and isn't
part of the published-image flow.

Open the configured `FRONTEND_URL` (or `http://localhost` by default).

## Trust notes

`.env` files are gitignored in the source repo and never copied into the
Docker build context, so published images never contain real credentials —
secrets only exist in the containers you run, supplied by your own
`<service>.env` files at runtime.
