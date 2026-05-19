FROM mcr.microsoft.com/dotnet/sdk:10.0 AS builder
WORKDIR /app
COPY Data.Abstractions/Data.Abstractions.csproj Data.Abstractions/
COPY Data/Data.csproj Data/
COPY Rss/Rss.csproj Rss/
RUN dotnet restore Rss/Rss.csproj
COPY Data.Abstractions/ Data.Abstractions/
COPY Data/ Data/
COPY Rss/ Rss/
RUN dotnet publish Rss/Rss.csproj -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=builder /out .
EXPOSE 8085
ENV ASPNETCORE_HTTP_PORTS=8085
CMD ["dotnet", "Rss.dll"]
