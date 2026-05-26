# syntax=docker/dockerfile:1.7

FROM node:22-alpine AS frontend-build
WORKDIR /src/frontend

COPY BizAnalytics.Frontend/package.json BizAnalytics.Frontend/package-lock.json ./
RUN npm ci

COPY BizAnalytics.Frontend/ ./

ARG VITE_API_BASE_URL=/api
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL

RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS backend-build
WORKDIR /src

COPY BizAnalytics.Api/BizAnalytics.Api.csproj BizAnalytics.Api/
RUN dotnet restore BizAnalytics.Api/BizAnalytics.Api.csproj

COPY BizAnalytics.Api/ BizAnalytics.Api/
RUN dotnet publish BizAnalytics.Api/BizAnalytics.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        curl \
        libfontconfig1 \
        libgdiplus \
    && rm -rf /var/lib/apt/lists/*

COPY --from=backend-build /app/publish ./
COPY --from=frontend-build /src/frontend/dist ./wwwroot

ENTRYPOINT ["dotnet", "BizAnalytics.Api.dll"]
