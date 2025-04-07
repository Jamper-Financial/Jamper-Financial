# Use the official .NET SDK image as a build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /source

# Copy the project files and restore dependencies
COPY . .
RUN dotnet restore "Jamper-Financial.Shared/Jamper-Financial.Shared.csproj"
RUN dotnet restore "Jamper-Financial.Web/Jamper-Financial.Web.csproj"

# Publish the shared project
RUN dotnet publish "Jamper-Financial.Shared/Jamper-Financial.Shared.csproj" -c Release -o /app/shared

# Publish the web project
RUN dotnet publish "Jamper-Financial.Web/Jamper-Financial.Web.csproj" -c Release -o /app/web

# Use the official ASP.NET Core runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/web .
COPY --from=build /source/Jamper-Financial.Shared/wwwroot /app/wwwroot

# Copy the database file
COPY --from=build /source/AppDatabase.db /AppDatabase.db

# Expose the port the app runs on
EXPOSE 8080

# Set the entry point for the container
ENTRYPOINT ["dotnet", "Jamper-Financial.Web.dll"]
