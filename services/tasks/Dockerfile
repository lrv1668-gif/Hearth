FROM mcr.microsoft.com/dotnet/sdk:10.0 AS builder
WORKDIR /app
COPY Data.Abstractions/Data.Abstractions.csproj Data.Abstractions/
COPY Data/Data.csproj data/
COPY tasks/Tasks.csproj tasks/
RUN dotnet restore tasks/Tasks.csproj
COPY Data.Abstractions/ Data.Abstractions/
COPY Data/ data/
COPY tasks/ tasks/
RUN dotnet publish tasks/Tasks.csproj -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=builder /out .
EXPOSE 8081
ENV ASPNETCORE_HTTP_PORTS=8081
CMD ["dotnet", "tasks.dll"]
