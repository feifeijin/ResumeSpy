FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["ResumeSpy.UI/ResumeSpy.UI.csproj", "ResumeSpy.UI/"]
COPY ["ResumeSpy.Infrastructure/ResumeSpy.Infrastructure.csproj", "ResumeSpy.Infrastructure/"]
COPY ["ResumeSpy.Core/ResumeSpy.Core.csproj", "ResumeSpy.Core/"]
RUN dotnet restore "ResumeSpy.UI/ResumeSpy.UI.csproj"
COPY . .
WORKDIR "/src/ResumeSpy.UI"
RUN dotnet publish "ResumeSpy.UI.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
CMD ["sh", "-c", "ASPNETCORE_URLS=http://0.0.0.0:${PORT:-8080} dotnet ResumeSpy.UI.dll"]
