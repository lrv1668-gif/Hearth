# Changelog

All notable changes to Hearth are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project uses a single version number for the whole stack (frontend +
all backend services are tagged and released together).

## [Unreleased]

### Added

- MIT `LICENSE`.
- Published container images for all services (`ghcr.io/lrv1668-gif/hearth-*`), built for `linux/amd64` and `linux/arm64`.
- `docker-compose.ghcr.yml` — a compose file for running Hearth from published images without cloning the repo.
- `docs/SELF-HOSTING.md` — setup guide for running Hearth from published images.
- Root `.env.example` documenting stack-wide configuration (`FRONTEND_URL`, `HEARTH_VERSION`).
