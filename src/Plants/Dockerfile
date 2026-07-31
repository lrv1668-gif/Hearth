FROM mcr.microsoft.com/dotnet/sdk:10.0 AS builder
WORKDIR /app
COPY Directory.Build.props Directory.Packages.props ./
COPY Data.Abstractions/Data.Abstractions.csproj Data.Abstractions/
COPY Data/Data.csproj Data/
COPY ServiceDefaults/ServiceDefaults.csproj ServiceDefaults/
COPY Plants/Plants.csproj Plants/
RUN dotnet restore Plants/Plants.csproj
COPY Data.Abstractions/ Data.Abstractions/
COPY Data/ Data/
COPY ServiceDefaults/ ServiceDefaults/
COPY Plants/ Plants/
RUN dotnet publish Plants/Plants.csproj -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=builder /out .
EXPOSE 8090
ENV ASPNETCORE_HTTP_PORTS=8090
CMD ["dotnet", "Plants.dll"]
