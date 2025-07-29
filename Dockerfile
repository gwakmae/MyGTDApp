# 1단계: 앱을 빌드(컴파일)하는 환경
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# .sln과 .csproj 파일들을 먼저 복사해서 패키지를 복원합니다.
COPY ["MyGtdApp.sln", "./"]
# 만약 프로젝트 폴더가 있다면 해당 경로를 정확히 지정해야 합니다. 
# 예를 들어 프로젝트가 MyGtdApp/MyGtdApp.csproj 라면 아래와 같이 수정합니다.
# COPY ["MyGtdApp/MyGtdApp.csproj", "MyGtdApp/"]
# 지금은 파일 목록상 최상위에 있는 것으로 보이므로 아래처럼 단순화합니다.
# .csproj 파일이 여러개라면 모두 복사해주는 것이 좋습니다.
COPY ["Components/", "Components/"]
COPY ["Models/", "Models/"]
COPY ["Services/", "Services/"]
COPY ["wwwroot/", "wwwroot/"]
# .csproj 파일명은 실제 파일명과 일치해야 합니다. (아마도 MyGtdApp.csproj 일 것입니다)
# 만약 다르다면 그 이름으로 수정해주세요.
# COPY ["MyGtdApp.csproj", "./"] 

# 현재는 모든 파일을 복사하는 방식으로 단순화해도 괜찮습니다.
COPY . .
RUN dotnet restore "MyGtdApp.sln"

# 나머지 소스 코드를 복사하고 앱을 'out' 폴더에 발행(publish)합니다.
RUN dotnet publish "MyGtdApp.sln" -c Release -o /app/publish

# 2단계: 빌드된 앱을 실행하는 환경 (가벼운 버전)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# MyGtdApp.dll 파일 이름이 실제 프로젝트 이름과 같은지 확인하세요.
ENTRYPOINT ["dotnet", "MyGtdApp.dll"]