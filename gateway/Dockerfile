# ===== Build stage =====
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ApiGateway/*.csproj ./ApiGateway/
RUN dotnet restore ApiGateway/ApiGateway.csproj

COPY ApiGateway/. ./ApiGateway/
WORKDIR /src/ApiGateway
RUN dotnet publish -c Release -o /app/publish

# ===== Runtime stage =====
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=build /app/publish .
COPY ApiGateway/ocelot.json .

EXPOSE 80
ENTRYPOINT ["dotnet", "ApiGateway.dll"]