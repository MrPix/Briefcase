FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY nuget.config .
COPY src/Briefcase.Domain/Briefcase.Domain.csproj src/Briefcase.Domain/
COPY src/Briefcase.Components/Briefcase.Components.csproj src/Briefcase.Components/
COPY src/Briefcase.Web/Briefcase.Web.csproj src/Briefcase.Web/
RUN dotnet restore src/Briefcase.Web/Briefcase.Web.csproj

COPY src/Briefcase.Domain/ src/Briefcase.Domain/
COPY src/Briefcase.Components/ src/Briefcase.Components/
COPY src/Briefcase.Web/ src/Briefcase.Web/
RUN dotnet publish src/Briefcase.Web/Briefcase.Web.csproj \
    -c Release -o /app/publish --no-restore

FROM caddy:alpine AS final
COPY --from=build /app/publish/wwwroot /usr/share/caddy
COPY src/Briefcase.Web/Caddyfile /etc/caddy/Caddyfile
EXPOSE 80
