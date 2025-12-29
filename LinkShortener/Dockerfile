FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["LinkShortener.csproj", "."]
RUN dotnet restore "./LinkShortener.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "LinkShortener.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LinkShortener.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LinkShortener.dll"]