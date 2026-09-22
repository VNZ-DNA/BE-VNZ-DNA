FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["VNZ.Api/VNZ.Api.csproj", "VNZ.Api/"]
COPY ["VNZ.Service/VNZ.Service.csproj", "VNZ.Service/"]
COPY ["VNZ.Repository/VNZ.Repository.csproj", "VNZ.Repository/"]
RUN dotnet restore "VNZ.Api/VNZ.Api.csproj"
COPY . .
WORKDIR "/src/VNZ.Api"
RUN dotnet build "./VNZ.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./VNZ.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "VNZ.Api.dll"]
