#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
# .NET 8 escucha en 8080 por defecto; se mantiene el 80 para conservar los mapeos existentes
ENV ASPNETCORE_HTTP_PORTS=80
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
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

# Usuario no-root para m�s seguridad
RUN useradd -m appuser && chown -R appuser:appuser /app
USER appuser

ENTRYPOINT ["dotnet", "web_api_users.dll"]

# docker build -t dotnet-minio-webapi .

# docker run \
# --name miniowebapi \
# --restart always \
# -d -p 7775:80 -p 7773:443 dotnet-minio-webapi