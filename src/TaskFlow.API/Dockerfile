FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
COPY src/TaskFlow.Shared/TaskFlow.Shared.csproj src/TaskFlow.Shared/
COPY src/TaskFlow.API/TaskFlow.API.csproj src/TaskFlow.API/
RUN dotnet restore src/TaskFlow.API/TaskFlow.API.csproj
COPY src/TaskFlow.Shared/ src/TaskFlow.Shared/
COPY src/TaskFlow.API/ src/TaskFlow.API/
RUN dotnet publish src/TaskFlow.API/TaskFlow.API.csproj -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /out .
EXPOSE 8080
ENTRYPOINT ["dotnet", "TaskFlow.API.dll"]