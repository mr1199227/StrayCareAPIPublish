# 阶段 1：使用 .NET 8 SDK 进行编译和发布
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 将 csproj 文件复制到工作目录并还原依赖项
COPY ["StrayCareAPI.csproj", "./"]
RUN dotnet restore "./StrayCareAPI.csproj"

# 将所有源代码复制过去并进行发布
COPY . .
RUN dotnet publish "StrayCareAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 阶段 2：使用更轻量的 .NET 8 运行时环境来运行 API
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# 告诉 Render 我们的 API 运行在 8080 端口 (Render 默认侦听端口)
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# 启动你的应用 (注意：将 StrayCareAPI.dll 替换为你实际编译出的 dll 名字，通常和项目名一致)
ENTRYPOINT ["dotnet", "StrayCareAPI.dll"]
