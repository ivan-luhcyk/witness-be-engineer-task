ARG BUILDPLATFORM
ARG TARGETPLATFORM

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["LeaseProcessing/LeaseProcessing.Functions/LeaseProcessing.Functions.csproj", "LeaseProcessing/LeaseProcessing.Functions/"]
RUN dotnet restore "LeaseProcessing/LeaseProcessing.Functions/LeaseProcessing.Functions.csproj"
COPY . .
RUN dotnet publish "LeaseProcessing/LeaseProcessing.Functions/LeaseProcessing.Functions.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM --platform=$TARGETPLATFORM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0
WORKDIR /home/site/wwwroot
COPY --from=build /app/publish .
