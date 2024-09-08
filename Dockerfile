# Etapa 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copiar archivos de proyecto y restaurar dependencias
COPY *.csproj ./
RUN dotnet restore ReservationPlatform.csproj

# Copiar el resto de los archivos y construir la aplicación
COPY . ./
RUN dotnet publish ReservationPlatform.csproj -c Release -o out

# Etapa 2: Runtime
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/out .

# Exponer el puerto en el que correrá la aplicación
EXPOSE 80

# Definir el comando para ejecutar la aplicación
ENTRYPOINT ["dotnet", "ReservationPlatform.dll"]
