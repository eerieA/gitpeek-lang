# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . ./
# The publish command will create a DLL named after your project
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Configure environment variables
# Setting ASPNETCORE_URLS, or else it will not be exposed
# Also have to generate dev cert on host machine
ENV ASPNETCORE_URLS="http://+:80;https://+:443"
ENV ASPNETCORE_ENVIRONMENT=Production

# Optional: Create non-root user for security
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

EXPOSE 80
EXPOSE 443

# Replace "MyWebApp.dll" with whatever name is in your .csproj file
# You can find this by looking at the AssemblyName in your .csproj
# or by checking what DLL gets created when you run 'dotnet publish'
ENTRYPOINT ["dotnet", "gitpeek-lang.dll"]