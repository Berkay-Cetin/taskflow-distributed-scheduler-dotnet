FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
COPY src/TaskFlow.Shared/TaskFlow.Shared.csproj src/TaskFlow.Shared/
COPY src/TaskFlow.Executor/TaskFlow.Executor.csproj src/TaskFlow.Executor/
RUN dotnet restore src/TaskFlow.Executor/TaskFlow.Executor.csproj
COPY src/TaskFlow.Shared/ src/TaskFlow.Shared/
COPY src/TaskFlow.Executor/ src/TaskFlow.Executor/
RUN dotnet publish src/TaskFlow.Executor/TaskFlow.Executor.csproj -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /out .
ENTRYPOINT ["dotnet", "TaskFlow.Executor.dll"]