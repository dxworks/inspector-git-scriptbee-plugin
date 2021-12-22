FROM mcr.microsoft.com/dotnet/runtime:5.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["InspectorGit/InspectorGit.csproj", "InspectorGit/"]
RUN dotnet restore "InspectorGit/InspectorGit.csproj"
COPY . .
WORKDIR "/src/InspectorGit"
RUN dotnet build "InspectorGit.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "InspectorGit.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "InspectorGit.dll"]
