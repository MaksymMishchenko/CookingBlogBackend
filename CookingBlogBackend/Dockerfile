FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["PostApiService/PostApiService.csproj", "PostApiService/"]
RUN dotnet restore "PostApiService/PostApiService.csproj"

COPY . .
WORKDIR "/src/PostApiService"
RUN dotnet build "PostApiService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PostApiService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "PostApiService.dll"]