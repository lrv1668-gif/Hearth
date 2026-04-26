FROM mcr.microsoft.com/dotnet/sdk:10.0 AS builder
WORKDIR /app
COPY Data.Abstractions/Data.Abstractions.csproj Data.Abstractions/
COPY Data/Data.csproj data/
COPY weather/weather.csproj weather/
RUN dotnet restore weather/weather.csproj
COPY Data.Abstractions/ Data.Abstractions/
COPY Data/ data/
COPY weather/ weather/
RUN dotnet publish weather/weather.csproj -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=builder /out .
EXPOSE 8082
ENV ASPNETCORE_HTTP_PORTS=8082
CMD ["dotnet", "weather.dll"]
