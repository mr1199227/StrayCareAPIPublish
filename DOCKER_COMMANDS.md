# Docker 常用指令 (StrayCareAPI)

这里是本项目常用的 Docker 指令集合，方便你快速查阅。

## 1. 基础操作 (日常使用)

### 🚀 启动数据库
在写代码或运行 API 之前，必须先运行这个：
```bash
docker-compose up -d
```
*   `up`: 启动
*   `-d`: 后台运行 (Detached)，不会占用这个终端窗口

### 🛑 停止数据库
当你把电脑关机，或者不想跑了：
```bash
docker-compose down
```
*   这会停止并删除容器，但**数据会保留** (因为我们配置了 Volume)。

### 🔄 重启数据库
如果数据库卡住了，或者你想重置一下服务：
```bash
docker-compose restart
```

---

## 2. 检查与调试

### 📋 查看正在运行的容器
看看数据库有没有活著：
```bash
docker ps
```
*   你应该能看到 `straycare_sql` 状态是 `Up`。

### 📜 查看日志 (报错时用)
如果连不上数据库，看看它报什么错：
```bash
docker logs straycare_sql
```

---

## 3. 高级操作 (慎用)

### ⚠️ 彻底重置 (删除所有数据)
如果你想把数据库**彻底清空**，重新开始：
```bash
docker-compose down -v
```
*   `-v`: 删除 Volume (数据卷)。
*   **警告**: 执行这个后，你之前注册的用户、上传的数据都会**消失**。
*   执行完后，你需要再次运行 `dotnet ef database update` 来重新创建表结构。

---

### 哪怕找不到命令？
如果提示 `command not found: docker`，请确保你已经打开了 **Docker Desktop** 应用。
