# Usar imagen base de .NET runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV PORT=8080
ENV ASPNETCORE_URLS=http://0.0.0.0:8080

# Usar imagen de SDK para build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar el archivo de proyecto y restaurar dependencias
COPY *.csproj ./
RUN dotnet restore

# Copiar todo el código fuente y buildear
COPY . ./
RUN dotnet publish -c Release -o /app/publish

# Imagen final
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

# Comando para ejecutar la aplicación
ENTRYPOINT ["dotnet", "HumbleChoiceScrapper.dll"]