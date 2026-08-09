# Use the official ASP.NET Core runtime as a base image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

# Set environment variable for the port
ENV ASPNETCORE_HTTP_PORTS=8080

# Use the SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
# Copy just the csproj first to restore dependencies (caches this layer)
COPY ["HostelSystem/HostelSystem.csproj", "HostelSystem/"]
RUN dotnet restore "HostelSystem/HostelSystem.csproj"

# Copy the rest of the files and build
COPY . .
WORKDIR "/src/HostelSystem"
RUN dotnet build "HostelSystem.csproj" -c Release -o /app/build

# Publish the app
FROM build AS publish
RUN dotnet publish "HostelSystem.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HostelSystem.dll"]
