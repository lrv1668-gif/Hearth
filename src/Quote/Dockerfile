FROM mcr.microsoft.com/dotnet/sdk:10.0 AS builder
WORKDIR /app
COPY Directory.Build.props Directory.Packages.props ./
COPY ServiceDefaults/ServiceDefaults.csproj ServiceDefaults/
COPY Quote/Quote.csproj Quote/
RUN dotnet restore Quote/Quote.csproj
COPY ServiceDefaults/ ServiceDefaults/
COPY Quote/ Quote/
RUN dotnet publish Quote/Quote.csproj -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=builder /out .
EXPOSE 8086
ENV ASPNETCORE_HTTP_PORTS=8086
CMD ["dotnet", "Quote.dll"]
