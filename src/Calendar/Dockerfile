FROM mcr.microsoft.com/dotnet/sdk:10.0 AS builder
WORKDIR /app
COPY Directory.Build.props Directory.Packages.props ./
COPY Data.Abstractions/Data.Abstractions.csproj Data.Abstractions/
COPY Data/Data.csproj Data/
COPY ServiceDefaults/ServiceDefaults.csproj ServiceDefaults/
COPY Calendar/Calendar.csproj Calendar/
RUN dotnet restore Calendar/Calendar.csproj
COPY Data.Abstractions/ Data.Abstractions/
COPY Data/ Data/
COPY ServiceDefaults/ ServiceDefaults/
COPY Calendar/ Calendar/
RUN dotnet publish Calendar/Calendar.csproj -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=builder /out .
EXPOSE 8087
ENV ASPNETCORE_HTTP_PORTS=8087
CMD ["dotnet", "Calendar.dll"]
