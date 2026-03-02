FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["HmlrApi/HmlrApi.csproj", "HmlrApi/"]
RUN dotnet restore "HmlrApi/HmlrApi.csproj"
COPY . .
WORKDIR "/src/HmlrApi"
RUN dotnet publish "HmlrApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
ENTRYPOINT ["dotnet", "HmlrApi.dll"]
