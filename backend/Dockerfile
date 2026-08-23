FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files and restore as distinct layers
COPY ["src/ECommerce.API/ECommerce.API.csproj", "src/ECommerce.API/"]
COPY ["src/ECommerce.Infrastructure/ECommerce.Infrastructure.csproj", "src/ECommerce.Infrastructure/"]
COPY ["src/ECommerce.UseCases/ECommerce.UseCases.csproj", "src/ECommerce.UseCases/"]
COPY ["src/ECommerce.Domain/ECommerce.Domain.csproj", "src/ECommerce.Domain/"]
COPY ["src/ECommerce.Shared/ECommerce.Shared.csproj", "src/ECommerce.Shared/"]

COPY Directory.Build.props .

RUN dotnet restore "src/ECommerce.API/ECommerce.API.csproj"

# Copy everything else and build
COPY src/ src/

WORKDIR "/src/src/ECommerce.API"
RUN dotnet build "ECommerce.API.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "ECommerce.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Build the final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Expose the standard port
EXPOSE 8080

ENTRYPOINT ["dotnet", "ECommerce.API.dll"]
