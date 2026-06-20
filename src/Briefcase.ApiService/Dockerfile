FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
# Npgsql probes for GSSAPI/Kerberos support at startup; install the library so
# the probe succeeds silently instead of logging a "cannot load" warning.
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 curl \
    && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY nuget.config .
COPY src/Briefcase.Domain/Briefcase.Domain.csproj src/Briefcase.Domain/
COPY src/Briefcase.ServiceDefaults/Briefcase.ServiceDefaults.csproj src/Briefcase.ServiceDefaults/
COPY src/Briefcase.Infrastructure/Briefcase.Infrastructure.csproj src/Briefcase.Infrastructure/
COPY src/Briefcase.ApiService/Briefcase.ApiService.csproj src/Briefcase.ApiService/
RUN dotnet restore src/Briefcase.ApiService/Briefcase.ApiService.csproj

COPY src/Briefcase.Domain/ src/Briefcase.Domain/
COPY src/Briefcase.ServiceDefaults/ src/Briefcase.ServiceDefaults/
COPY src/Briefcase.Infrastructure/ src/Briefcase.Infrastructure/
COPY src/Briefcase.ApiService/ src/Briefcase.ApiService/
RUN dotnet publish src/Briefcase.ApiService/Briefcase.ApiService.csproj \
    -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Briefcase.ApiService.dll"]
