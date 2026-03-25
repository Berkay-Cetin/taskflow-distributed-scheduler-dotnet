FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
COPY src/TaskFlow.Shared/TaskFlow.Shared.csproj src/TaskFlow.Shared/
COPY src/TaskFlow.Scheduler/TaskFlow.Scheduler.csproj src/TaskFlow.Scheduler/
RUN dotnet restore src/TaskFlow.Scheduler/TaskFlow.Scheduler.csproj
COPY src/TaskFlow.Shared/ src/TaskFlow.Shared/
COPY src/TaskFlow.Scheduler/ src/TaskFlow.Scheduler/
RUN dotnet publish src/TaskFlow.Scheduler/TaskFlow.Scheduler.csproj -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /out .
ENTRYPOINT ["dotnet", "TaskFlow.Scheduler.dll"]