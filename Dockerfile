# Dockerfile
# Code attribution: multi-stage build pattern adapted from Microsoft's official
# Azure Functions custom container documentation:
# Microsoft, "Azure Functions image builder - .NET isolated worker container,"
# Microsoft Learn, 2025. [Online]. Available:
# https://learn.microsoft.com/en-us/azure/azure-functions/functions-how-to-custom-container
# [Accessed: 24-Aug-2026].
# and Docker's official Dockerfile reference:
# Docker Inc., "Dockerfile reference," Docker Docs, 2025. [Online].
# Available: https://docs.docker.com/reference/dockerfile/
# [Accessed: 24-Aug-2026].

# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish "CoffeeNChillFunctions.csproj" -c Release -o /home/site/wwwroot

# ---- Runtime stage ----
FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated9.0
ENV AzureFunctionsJobHost__Logging__Console__IsEnabled=true
COPY --from=build ["/home/site/wwwroot", "/home/site/wwwroot"]
EXPOSE 80