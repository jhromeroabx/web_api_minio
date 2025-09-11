FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

#ENV ASPNETCORE_HTTP_PORT https://+:5001
# ENV ASPNETCORE_URLS http://*:5000
#
#FOR SWAGGER
ENV ASPNETCORE_ENVIRONMENT=Development
#
# EXPOSE 5000
# EXPOSE 5001

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["web_api_users/web_api_users.csproj", "web_api_users/"]
RUN dotnet restore "web_api_users/web_api_users.csproj"
COPY . .
WORKDIR "/src/web_api_users"
RUN dotnet build "web_api_users.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "web_api_users.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "web_api_users.dll"]
# docker build --rm -t minio-loasi .