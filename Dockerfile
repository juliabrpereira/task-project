FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/TaskApi.Api/TaskApi.Api.csproj src/TaskApi.Api/
RUN dotnet restore src/TaskApi.Api/TaskApi.Api.csproj
COPY src/ src/
RUN dotnet publish src/TaskApi.Api/TaskApi.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "TaskApi.Api.dll"]
