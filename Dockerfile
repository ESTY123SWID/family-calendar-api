# The API as a container image – for a host that runs Docker (optional; nothing is published by this file).
# Build from the solution folder:  docker build -t family-calendar-api .
# Secrets are NOT in the image: pass them as environment variables when the container runs
# (ConnectionStrings__Default, Jwt__Key, SeedAdmin__Email, SeedAdmin__Password, Cors__AllowedOrigins__0 ...)

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY my-project.Core/my-project.Core.csproj my-project.Core/
COPY my-project.Data/my-project.Data.csproj my-project.Data/
COPY my-project.Service/my-project.Service.csproj my-project.Service/
COPY my-project.API/my-project.API.csproj my-project.API/
RUN dotnet restore my-project.API/my-project.API.csproj
COPY my-project.Core/ my-project.Core/
COPY my-project.Data/ my-project.Data/
COPY my-project.Service/ my-project.Service/
COPY my-project.API/ my-project.API/
RUN dotnet publish my-project.API/my-project.API.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
# The host's proxy terminates HTTPS; the container listens on plain HTTP
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080 \
    Hosting__BehindProxy=true
EXPOSE 8080
ENTRYPOINT ["dotnet", "my-project.API.dll"]
