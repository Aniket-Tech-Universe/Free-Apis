FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["UnsecuredAPIKeys.WebAPI/UnsecuredAPIKeys.WebAPI.csproj", "UnsecuredAPIKeys.WebAPI/"]
COPY ["UnsecuredAPIKeys.Data/UnsecuredAPIKeys.Data.csproj", "UnsecuredAPIKeys.Data/"]
RUN dotnet restore "UnsecuredAPIKeys.WebAPI/UnsecuredAPIKeys.WebAPI.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/UnsecuredAPIKeys.WebAPI"
RUN dotnet build "UnsecuredAPIKeys.WebAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "UnsecuredAPIKeys.WebAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 10000
ENV ASPNETCORE_URLS=http://+:10000
ENV ASPNETCORE_ENVIRONMENT=Production
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "UnsecuredAPIKeys.WebAPI.dll"]
