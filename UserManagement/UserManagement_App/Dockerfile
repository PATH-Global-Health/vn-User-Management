#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["UserManagement_App/UserManagement_App.csproj", "UserManagement_App/"]
COPY ["Service/Service.csproj", "Service/"]
COPY ["Data/Data.csproj", "Data/"]
RUN dotnet restore "UserManagement_App/UserManagement_App.csproj"
COPY . .
WORKDIR "/src/UserManagement_App"
RUN dotnet build "UserManagement_App.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "UserManagement_App.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "UserManagement_App.dll"]