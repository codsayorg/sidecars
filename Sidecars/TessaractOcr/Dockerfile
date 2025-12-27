FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
EXPOSE 8080
EXPOSE 8081

USER root
# 1. Install prerequisites for adding PPAs
RUN apt-get update && \
    apt-get install -y --no-install-recommends software-properties-common gnupg && \
    # 2. Add the Tesseract 5 PPA (maintained by the official dev)
    add-apt-repository -y ppa:alex-p/tesseract-ocr5 && \
    apt-get update && \
    apt-get install -y --no-install-recommends tesseract-ocr libtesseract-dev tesseract-ocr-eng && \
    # 4. Cleanup
    apt-get purge -y --auto-remove software-properties-common && \
    apt-get clean && rm -rf /var/lib/apt/lists/*

USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["../../SidecarService/SidecarService.csproj", "SidecarService/"]
RUN dotnet restore "SidecarService/SidecarService.csproj"
COPY . .
WORKDIR "/src/SidecarService"
RUN dotnet build "./SidecarService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./SidecarService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SidecarService.dll"]
