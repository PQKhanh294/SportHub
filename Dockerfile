# Stage 1: Build environment
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o out

# Stage 2: Runtime environment
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build-env /app/out .

# Container mặc định chạy UTC — đặt múi giờ Việt Nam để DateTime.Now/.ToLocalTime() trong code
# (dùng khắp nơi: hạn thanh toán, lịch sử giao dịch, thông báo...) khớp giờ Việt Nam thay vì lệch
# theo giờ server khi deploy.
ENV TZ=Asia/Ho_Chi_Minh
RUN apt-get update && apt-get install -y tzdata && \
    ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ > /etc/timezone && \
    rm -rf /var/lib/apt/lists/*

# Render expects web services to listen on port 8080 or the port specified in PORT env var.
# ASP.NET Core 8 defaults to port 8080 in container environments.
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "SportHub.dll"]
