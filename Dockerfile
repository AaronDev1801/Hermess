FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar el archivo de proyecto/restaurar dependencias
COPY ["Hermess.csproj", "./"]
RUN dotnet restore "./Hermess.csproj"

# Copiar el resto del código y compilar
COPY . .
RUN dotnet publish "Hermess.csproj" -c Release -o /app/publish

# Imagen de ASP.NET para correr la app
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Puerto
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Punto de entrada
ENTRYPOINT ["dotnet", "Hermess.dll"]
