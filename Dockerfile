FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build-env
WORKDIR /app

# copy everything and build the project
COPY /UserManagement ./
RUN dotnet restore UserManagement_App/*.csproj
RUN dotnet publish UserManagement_App/*.csproj -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 
WORKDIR /app
COPY --from=build-env /app/UserManagement_App/bin/Release/netcoreapp3.1/ .
ENTRYPOINT ["dotnet", "UserManagement_App.dll"]
