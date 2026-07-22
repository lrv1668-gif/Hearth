FROM mcr.microsoft.com/dotnet/sdk:10.0 AS builder
WORKDIR /app
COPY Directory.Build.props Directory.Packages.props ./
COPY Data.Abstractions/Data.Abstractions.csproj Data.Abstractions/
COPY Data/Data.csproj Data/
COPY ServiceDefaults/ServiceDefaults.csproj ServiceDefaults/
COPY Weather/Weather.csproj Weather/
RUN dotnet restore Weather/Weather.csproj
COPY Data.Abstractions/ Data.Abstractions/
COPY Data/ Data/
COPY ServiceDefaults/ ServiceDefaults/
COPY Weather/ Weather/
RUN dotnet publish Weather/Weather.csproj -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=builder /out .
EXPOSE 8082
ENV ASPNETCORE_HTTP_PORTS=8082
CMD ["dotnet", "Weather.dll"]
