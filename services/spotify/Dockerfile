FROM mcr.microsoft.com/dotnet/sdk:10.0 AS builder
WORKDIR /app
COPY Data.Abstractions/Data.Abstractions.csproj Data.Abstractions/
COPY Data/Data.csproj data/
COPY spotify/spotify.csproj spotify/
RUN dotnet restore spotify/spotify.csproj
COPY Data.Abstractions/ Data.Abstractions/
COPY Data/ data/
COPY spotify/ spotify/
RUN dotnet publish spotify/spotify.csproj -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=builder /out .
EXPOSE 8083
ENV ASPNETCORE_HTTP_PORTS=8083
CMD ["dotnet", "spotify.dll"]
