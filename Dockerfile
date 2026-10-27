FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8004
ENV ASPNETCORE_URLS=http://+:8004

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Log.Api/Log.Api.csproj", "Log.Api/"]
COPY ["Log.Application/Log.Application.csproj", "Log.Application/"]
COPY ["Log.Domain/Log.Domain.csproj", "Log.Domain/"]
COPY ["Log.Infrastructure/Log.Infrastructure.csproj", "Log.Infrastructure/"]
RUN dotnet restore "Log.Api/Log.Api.csproj"
COPY . .
RUN dotnet build "Log.Api/Log.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Log.Api/Log.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Log.Api.dll"]
