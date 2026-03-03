# Base stage for runtime
FROM mcr.microsoft.com/dotnet/aspnet:11.0-preview AS base
USER $APP_UID
WORKDIR /app
EXPOSE 5000

# SDK stage for building
FROM mcr.microsoft.com/dotnet/sdk:11.0-preview AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["IndieAPI.Api/IndieAPI.Api.csproj", "IndieAPI.Api/"]
RUN dotnet restore "IndieAPI.Api/IndieAPI.Api.csproj"
COPY . .
WORKDIR "/src/IndieAPI.Api"
RUN dotnet build "IndieAPI.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "IndieAPI.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final stage for production image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "IndieAPI.Api.dll"]
