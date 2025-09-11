#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Instala libgdiplus
RUN apt-get update && apt-get install -y libgdiplus

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["web_api_users.csproj", "."]
RUN dotnet restore "./web_api_users.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "web_api_users.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "web_api_users.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Usuario no-root para más seguridad
RUN useradd -m appuser && chown -R appuser:appuser /app
USER appuser

ENTRYPOINT ["dotnet", "web_api_users.dll"]

# docker build -t dotnet-minio-webapi .

# docker run \
# --name miniowebapi \
# --restart always \
# -d -p 7775:80 -p 7773:443 dotnet-minio-webapi