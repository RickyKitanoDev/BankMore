FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Tarifa.API.csproj", "./"]
RUN dotnet restore "Tarifa.API.csproj"
COPY . .
RUN dotnet build "Tarifa.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Tarifa.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Tarifa.API.dll"]
