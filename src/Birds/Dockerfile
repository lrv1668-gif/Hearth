FROM mcr.microsoft.com/dotnet/sdk:10.0 AS builder
WORKDIR /app
COPY Directory.Build.props Directory.Packages.props ./
COPY Data.Abstractions/Data.Abstractions.csproj Data.Abstractions/
COPY Data/Data.csproj Data/
COPY ServiceDefaults/ServiceDefaults.csproj ServiceDefaults/
COPY Birds/Birds.csproj Birds/
RUN dotnet restore Birds/Birds.csproj
COPY Data.Abstractions/ Data.Abstractions/
COPY Data/ Data/
COPY ServiceDefaults/ ServiceDefaults/
COPY Birds/ Birds/
RUN dotnet publish Birds/Birds.csproj -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=builder /out .
EXPOSE 8088
ENV ASPNETCORE_HTTP_PORTS=8088
CMD ["dotnet", "Birds.dll"]
