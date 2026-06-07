# =========================================================
# Stage 1: Build Stage
# We use the full Microsoft SDK image to compile the code and restore packages
# =========================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# a) Copy only .csproj files first for efficient caching during NuGet restore
COPY ["Quraaa.API/Quraaa.API.csproj", "Quraaa.API/"]
COPY ["Quraaa.Application/Quraaa.Application.csproj", "Quraaa.Application/"]
COPY ["Quraaa.Domain/Quraaa.Domain.csproj", "Quraaa.Domain/"]
COPY ["Quraaa.Infrastructure/Quraaa.Infrastructure.csproj", "Quraaa.Infrastructure/"]
COPY ["Quraaa.Persistence/Quraaa.Persistence.csproj", "Quraaa.Persistence/"]

# b) Restore all NuGet packages required for the project
RUN dotnet restore "Quraaa.API/Quraaa.API.csproj"

# c) Copy the remaining source code files into the container
COPY . .

# d) Navigate to the API project folder and build it in Release mode
WORKDIR "/src/Quraaa.API"
RUN dotnet build "Quraaa.API.csproj" -c Release -o /app/build

# =========================================================
# Stage 2: Publish Stage
# Compile and publish the application binaries into a clean directory
# =========================================================
FROM build AS publish
RUN dotnet publish "Quraaa.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# =========================================================
# Stage 3: Final Runtime Stage
# We use a lightweight ASP.NET runtime image (without the SDK)
# to minimize container size and optimize production deployment
# =========================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Copy the published output from the publish stage
COPY --from=publish /app/publish .

# Expose port 8080 and configure ASP.NET Core to listen on it for incoming client requests
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# The entry point command to run the API when the container starts
ENTRYPOINT ["dotnet", "Quraaa.API.dll"]