# 1단계: 앱을 빌드(컴파일)하는 환경
# .NET 8 SDK 이미지를 기반으로 시작합니다.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 현재 디렉터리의 모든 파일과 폴더를 Docker 이미지 안으로 복사합니다.
# 이 한 줄이 여러 줄의 COPY 명령어를 대체하며 더 확실합니다.
COPY . .

# .sln 파일을 사용하여 모든 프로젝트의 종속성을 복원합니다.
RUN dotnet restore "MyGtdApp/MyGtdApp.csproj"

# 릴리스 모드로 앱을 빌드하고 'out' 폴더에 발행(publish)합니다.
RUN dotnet publish "MyGtdApp/MyGtdApp.csproj" -c Release -o /app/publish

# 2단계: 빌드된 앱을 실행하는 환경 (가벼운 버전)
# ASP.NET Core 런타임 이미지를 기반으로 시작합니다.
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# MyGtdApp.dll 파일을 실행하여 서버를 시작합니다.
ENTRYPOINT ["dotnet", "MyGtdApp.dll"]