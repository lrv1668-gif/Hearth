FROM mcr.microsoft.com/dotnet/sdk:10.0 AS builder
WORKDIR /app
COPY Photos/Photos.csproj Photos/
RUN dotnet restore Photos/Photos.csproj
COPY Photos/ Photos/
RUN dotnet publish Photos/Photos.csproj -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=builder /out .
EXPOSE 8084
ENV ASPNETCORE_HTTP_PORTS=8084
CMD ["dotnet", "Photos.dll"]
