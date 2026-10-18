# Shared image for every backend service — pick one with the SERVICE build arg
# (set per service in docker-compose.yml), e.g.
#   docker build --build-arg SERVICE=Tasks -f src/Dockerfile src
ARG SERVICE

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS builder
ARG SERVICE
# Set by BuildKit (amd64/arm64). Publishing for one RID drops the other platforms'
# native libraries (SkiaSharp alone ships ~18) from the image.
ARG TARGETARCH
WORKDIR /src

# Restore from project files only, so source edits don't invalidate the restore layer
COPY Directory.Build.props Directory.Packages.props ./
COPY Data.Abstractions/Data.Abstractions.csproj Data.Abstractions/
COPY Data/Data.csproj Data/
COPY ServiceDefaults/ServiceDefaults.csproj ServiceDefaults/
COPY ${SERVICE}/${SERVICE}.csproj ${SERVICE}/
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages,sharing=locked \
    dotnet restore ${SERVICE}/${SERVICE}.csproj -a ${TARGETARCH}

COPY Data.Abstractions/ Data.Abstractions/
COPY Data/ Data/
COPY ServiceDefaults/ ServiceDefaults/
COPY ${SERVICE}/ ${SERVICE}/
# The apphost has its .dll name baked in, so renaming it to a fixed name lets one
# ENTRYPOINT serve every service (chiseled images have no shell to expand ${SERVICE}).
# /data is the volume mount point, created here because the runtime image has no
# shell; new volumes inherit its non-root ownership.
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages,sharing=locked \
    dotnet publish ${SERVICE}/${SERVICE}.csproj -c Release -a ${TARGETARCH} --self-contained false \
        --no-restore -o /out \
    && mv /out/${SERVICE} /out/service \
    && mkdir /data

# chiseled: no shell/package manager, non-root by default; -extra adds ICU + tzdata
# (TimeZoneInfo is used for local-time math in Almanac, Birds, Photos, CacheFreshness)
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled-extra
WORKDIR /app
COPY --from=builder /out .
COPY --from=builder --chown=$APP_UID:$APP_UID /data /data
USER $APP_UID
ENTRYPOINT ["/app/service"]
