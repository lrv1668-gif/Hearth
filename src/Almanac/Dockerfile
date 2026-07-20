FROM mcr.microsoft.com/dotnet/sdk:10.0 AS builder
WORKDIR /app
COPY Almanac/Almanac.csproj Almanac/
RUN dotnet restore Almanac/Almanac.csproj
COPY Almanac/ Almanac/
RUN dotnet publish Almanac/Almanac.csproj -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=builder /out .
EXPOSE 8089
ENV ASPNETCORE_HTTP_PORTS=8089
CMD ["dotnet", "Almanac.dll"]
