FROM mcr.microsoft.com/dotnet/sdk:10.0 AS builder
WORKDIR /app
COPY Data.Abstractions/Data.Abstractions.csproj Data.Abstractions/
COPY Data/Data.csproj Data/
COPY Spotify/Spotify.csproj Spotify/
RUN dotnet restore Spotify/Spotify.csproj
COPY Data.Abstractions/ Data.Abstractions/
COPY Data/ Data/
COPY Spotify/ Spotify/
RUN dotnet publish Spotify/Spotify.csproj -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=builder /out .
EXPOSE 8083
ENV ASPNETCORE_HTTP_PORTS=8083
CMD ["dotnet", "Spotify.dll"]
