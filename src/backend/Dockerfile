# syntax=docker/dockerfile:1.7
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

COPY ["src/LastMile.TMS.Domain/LastMile.TMS.Domain.csproj", "src/LastMile.TMS.Domain/"]
COPY ["src/LastMile.TMS.Application/LastMile.TMS.Application.csproj", "src/LastMile.TMS.Application/"]
COPY ["src/LastMile.TMS.Infrastructure/LastMile.TMS.Infrastructure.csproj", "src/LastMile.TMS.Infrastructure/"]
COPY ["src/LastMile.TMS.Persistence/LastMile.TMS.Persistence.csproj", "src/LastMile.TMS.Persistence/"]
COPY ["src/LastMile.TMS.Api/LastMile.TMS.Api.csproj", "src/LastMile.TMS.Api/"]
RUN --mount=type=cache,id=lastmile-tms-nuget,target=/root/.nuget/packages \
    dotnet restore "src/LastMile.TMS.Api/LastMile.TMS.Api.csproj"
COPY src/ src/
RUN --mount=type=cache,id=lastmile-tms-nuget,target=/root/.nuget/packages \
    dotnet publish "src/LastMile.TMS.Api/LastMile.TMS.Api.csproj" \
    -c Release \
    -o /app \
    --no-restore \
    /p:UseAppHost=false
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "LastMile.TMS.Api.dll"]
