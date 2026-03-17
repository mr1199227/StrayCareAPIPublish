# StrayCareAPI 接口文档 — 前端 Java 使用指南

> **Base URL**: `https://<your-host>/api`  
> **认证方式**: JWT Bearer Token（登录后获取 token，放入 `Authorization: Bearer <token>` 请求头）

---

## 目录

1. [认证模块 (Auth)](#1-认证模块-auth)
2. [动物模块 (Animal)](#2-动物模块-animal)
3. [品种模块 (Breed)](#3-品种模块-breed)
4. [目击模块 (Sightings)](#4-目击模块-sightings)
5. [喂养模块 (Feeding)](#5-喂养模块-feeding)
6. [领养模块 (Adoption)](#6-领养模块-adoption)
7. [收容所模块 (Shelter)](#7-收容所模块-shelter)
8. [志愿者模块 (Volunteer)](#8-志愿者模块-volunteer)
9. [医疗众筹模块 (Medical)](#9-医疗众筹模块-medical)
10. [管理员模块 (Admin)](#10-管理员模块-admin)
11. [通用响应格式](#11-通用响应格式)
12. [Java 调用示例](#12-java-调用示例)

---

## 1. 认证模块 (Auth)

> 路由前缀: `/api/auth`  
> 无需 Token（公开接口）

### 1.1 用户注册

| 项目 | 说明 |
|------|------|
| **URL** | `POST /api/auth/register` |
| **Content-Type** | `application/json` |
| **需要 Token** | ❌ |

**请求体 (JSON)**:
```json
{
  "username": "string (必填, 最大100)",
  "email": "string (必填, Email格式, 最大200)",
  "phoneNumber": "string (可选, 最大20)",
  "password": "string (必填, 6~100)",
  "role": "string (必填, 可选值: User/Admin/Volunteer, 默认: User)"
}
```

**成功响应** `200`:
```json
{
  "message": "Registration successful",
  "data": { ... }
}
```

**失败响应** `400`:
```json
{ "message": "错误原因" }
```

---

### 1.2 志愿者注册

| 项目 | 说明 |
|------|------|
| **URL** | `POST /api/auth/register-volunteer` |
| **Content-Type** | `application/json` |
| **需要 Token** | ❌ |

**请求体 (JSON)**:
```json
{
  "username": "string (必填, 最大100)",
  "email": "string (必填, Email格式, 最大200)",
  "phoneNumber": "string (可选, 最大20)",
  "password": "string (必填, 6~100)",
  "role": "Volunteer",
  "fullName": "string (必填, 最大100)",
  "skills": "string (可选, 最大500)",
  "availability": "string (可选, 最大200)",
  "hasVehicle": false,
  "experienceLevel": "string (可选, 默认: Beginner, 最大50)"
}
```

**成功响应** `200`:
```json
{ "message": "Registration successful" }
```

---

### 1.3 收容所注册

| 项目 | 说明 |
|------|------|
| **URL** | `POST /api/auth/register-shelter` |
| **Content-Type** | `multipart/form-data` |
| **需要 Token** | ❌ |

**请求参数 (Form-Data)**:

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `username` | string | ✅ | 用户名 (最大100) |
| `email` | string | ✅ | 邮箱 |
| `password` | string | ✅ | 密码 (最大100) |
| `phoneNumber` | string | ❌ | 手机 (最大20) |
| `shelterName` | string | ✅ | 收容所名 (最大200) |
| `address` | string | ✅ | 地址 (最大300) |
| `latitude` | double | ✅ | 纬度 |
| `longitude` | double | ✅ | 经度 |
| `description` | string | ❌ | 描述 (最大1000) |
| `licenseFile` | File | ❌ | 营业执照图片 (JPG/PNG, ≤10MB) |

**成功响应** `200`:
```json
{ "message": "Registration successful. Pending admin approval." }
```

---

### 1.4 用户登录

| 项目 | 说明 |
|------|------|
| **URL** | `POST /api/auth/login` |
| **Content-Type** | `application/json` |
| **需要 Token** | ❌ |

**请求体 (JSON)**:
```json
{
  "email": "string (必填, Email格式)",
  "password": "string (必填)"
}
```

**成功响应** `200`:
```json
{
  "token": "eyJhbGci...",
  "userId": 1,
  "username": "xxx",
  "role": "User"
}
```

**失败响应**:
- `401` — 邮箱或密码错误
- `403` — 账号待审核 `{ "message": "Account is pending approval." }`

---

### 1.5 忘记密码

| 项目 | 说明 |
|------|------|
| **URL** | `POST /api/auth/forgot-password` |
| **Content-Type** | `application/json` |

**请求体**:
```json
{ "email": "string (必填, Email格式)" }
```

**响应** `200`:
```json
{
  "message": "...",
  "resetToken": "string",
  "email": "string"
}
```

---

### 1.6 重置密码

| 项目 | 说明 |
|------|------|
| **URL** | `POST /api/auth/reset-password` |
| **Content-Type** | `application/json` |

**请求体**:
```json
{
  "email": "string (必填)",
  "resetToken": "string (必填)",
  "newPassword": "string (必填, 6~100)"
}
```

**成功响应** `200`:
```json
{ "message": "Password reset successful." }
```

---

### 1.7 编辑个人资料 (Edit Profile)

| 项目 | 说明 |
|------|------|
| **URL** | `POST /api/auth/edit-profile` |
| **Content-Type** | `multipart/form-data` |
| **需要 Token** | ✅ |

**请求参数 (Form-Data)**:

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `fullName` | string | ❌ | 姓名 (最大100) |
| `phoneNumber` | string | ❌ | 电话号码 (最大20) |
| `address` | string | ❌ | 地址 (最大200) |
| `profileImage` | File | ❌ | 头像图片 |
| `volunteerData.skills` | string | ❌ | 志愿者技能 (限 Volunteer 角色) |
| `volunteerData.availability` | string | ❌ | 志愿者空闲时间 (限 Volunteer 角色) |
| `volunteerData.hasVehicle` | bool | ❌ | 志愿者是否有车 (限 Volunteer 角色) |
| `volunteerData.experienceLevel` | string | ❌ | 志愿者经验水平 (限 Volunteer 角色) |
| `shelterData.shelterName` | string | ❌ | 收容所名称 (限 Shelter 角色) |
| `shelterData.address` | string | ❌ | 收容所地址 (限 Shelter 角色) |
| `shelterData.latitude` | double | ❌ | 收容所纬度 (限 Shelter 角色) |
| `shelterData.longitude` | double | ❌ | 收容所经度 (限 Shelter 角色) |
| `shelterData.description` | string | ❌ | 收容所描述 (限 Shelter 角色) |

**成功响应** `200`:
```json
{
  "message": "...",
  "data": { ... }
}
```

---

### 1.8 获取当前用户资料 (Get Current User Profile)

| 项目 | 说明 |
|------|------|
| **URL** | `GET /api/auth/me` |
| **需要 Token** | ✅ |

**成功响应** `200`: 返回用户完整资料对象。包含角色专属数据 (Volunteer/Shelter Profile)。

---

### 1.9 发送邮箱验证码 (Send OTP)

| 项目 | 说明 |
|------|------|
| **URL** | `POST /api/auth/send-verification` |
| **Content-Type** | `application/json` |
| **需要 Token** | ❌ |

**请求体 (JSON)**:
```json
{
  "email": "string (必填, Email格式)",
  "type": "number / string (可选, 默认为 0/Email, 可选值：0=Email, 1=PasswordReset, 2=Approved, 3=Rejected)"
}
```

**成功响应** `200`:
```json
{ "message": "Verification email sent successfully." }
```

---

### 1.10 验证邮箱 (Verify OTP)

| 项目 | 说明 |
|------|------|
| **URL** | `POST /api/auth/verify-email` |
| **Content-Type** | `application/json` |
| **需要 Token** | ❌ |

**请求体 (JSON)**:
```json
{
  "email": "string (必填)",
  "code": "string (必填, 接收到的验证码)"
}
```

**成功响应** `200`:
```json
{ "message": "Email verified successfully." }
```

---

## 2. 动物模块 (Animal)

> 路由前缀: `/api/animal`  
> 无需特定角色（公开接口）

### 2.1 获取所有动物

| 项目 | 说明 |
|------|------|
| **URL** | `GET /api/animal/getAnimals` |
| **需要 Token** | ❌ |

**成功响应** `200`: 返回 `Animal[]` 数组

---

### 2.2 获取单个动物

| 项目 | 说明 |
|------|------|
| **URL** | `GET /api/animal/getAnimal/{id}` |
| **需要 Token** | ❌ |

**成功响应** `200`: 返回 `Animal` 对象  
**失败响应** `404`: `{ "message": "动物不存在" }`

---

### 2.3 创建新动物

| 项目 | 说明 |
|------|------|
| **URL** | `POST /api/animal/createAnimal` |
| **Content-Type** | `multipart/form-data` |
| **需要 Token** | ❌ |

**请求参数 (Form-Data)**:

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `name` | string | ✅ | 动物名 (最大100) |
| `breedId` | int | ✅ | 品种ID |
| `gender` | string | ❌ | 性别 (默认 Unknown) |
| `size` | string | ❌ | 体型 (默认 Unknown) |
| `estAge` | string | ❌ | 估计年龄 (默认 Unknown) |
| `status` | string | ❌ | 状态 (默认 Stray) |
| `imageFiles` | File[] | ❌ | 图片 (最多3张) |

**成功响应** `201`: 返回创建的 `Animal` 对象

---

### 2.4 获取动物活动历史

| 项目 | 说明 |
|------|------|
| **URL** | `GET /api/animal/getActivityHistory/{id}` |
| **需要 Token** | ❌ |

**成功响应** `200`: 返回 `ActivityHistoryDto[]`

```json
[
  {
    "activityType": "Sighting | Feeding",
    "id": 1,
    "timestamp": "2026-01-28T...",
    "locationName": "公园",
    "latitude": 31.2,
    "longitude": 121.5,
    "imageUrl": "/uploads/...",
    "description": "描述",
    "username": "报告人"
  }
]
```

---

### 2.5 获取动物目击记录

| 项目 | 说明 |
|------|------|
| **URL** | `GET /api/animal/getAnimalSightings/{animalId}` |
| **需要 Token** | ❌ |

**成功响应** `200`: 返回目击记录数组

```json
[
  {
    "id": 1,
    "timestamp": "...",
    "description": "...",
    "sightingImageUrl": "...",
    "locationName": "...",
    "latitude": 31.2,
    "longitude": 121.5,
    "reporterName": "用户名"
  }
]
```

---

### 2.6 获取附近动物

| 项目 | 说明 |
|------|------|
| **URL** | `GET /api/animal/getNearbyAnimals?latitude=31.2&longitude=121.5&radius=5.0` |
| **需要 Token** | ❌ |

**Query 参数**:

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `latitude` | double | ✅ | 纬度 |
| `longitude` | double | ✅ | 经度 |
| `radius` | double | ❌ | 搜索半径(km, 默认5.0) |

**成功响应** `200`: 返回 `AnimalDto[]`

```json
[
  {
    "id": 1,
    "name": "小花",
    "species": "Dog",
    "breedId": 3,
    "breedName": "金毛",
    "gender": "Female",
    "size": "Large",
    "estAge": "2 years",
    "imageUrls": ["/uploads/animals/xxx.jpg"],
    "status": "Stray",
    "isVaccinated": false,
    "isNeutered": false,
    "currentGoalType": "None",
    "goalAmount": 0,
    "raisedAmount": 0,
    "goalStatus": "Completed",
    "locationName": "中央公园",
    "latitude": 31.2,
    "longitude": 121.5,
    "distanceInKm": 1.23
  }
]
```

---

## 3. 品种模块 (Breed)

> 路由前缀: `/api/breed`  
> 公开接口

### 3.1 获取品种列表

| 项目 | 说明 |
|------|------|
| **URL** | `GET /api/breed/getBreeds` 或 `GET /api/breed/getBreeds?species=Dog` |
| **需要 Token** | ❌ |

**Query 参数**:

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `species` | string | ❌ | 按物种筛选 (Dog / Cat) |

**成功响应** `200`: 返回 `Breed[]`

```json
[
  { "id": 1, "name": "金毛", "species": "Dog" }
]
```

### 3.2 获取单个品种

| 项目 | 说明 |
|------|------|
| **URL** | `GET /api/breed/getBreed/{id}` |

**成功响应** `200`: 返回 `Breed` 对象  
**失败响应** `404`: `{ "message": "品种不存在" }`

---

## 4. 目击模块 (Sightings)

> 路由前缀: `/api/sightings`

### 4.1 创建目击报告

| 项目 | 说明 |
|------|------|
| **URL** | `POST /api/sightings/createSighting` |
| **Content-Type** | `multipart/form-data` |
| **需要 Token** | ❌ |

> **智能分支**:  
> - **场景 A**: `animalId` 有值 → 更新已知动物的位置  
> - **场景 B**: `animalId` 为空 → 创建新动物 + 目击记录

**请求参数 (Form-Data)**:

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `animalId` | int? | ❌ | 已知动物ID (为空则新建) |
| `newAnimalName` | string | 场景B必填 | 新动物名 (最大100) |
| `breedId` | int? | 场景B必填 | 品种ID |
| `imageFile` | File | ❌ | 目击照片 |
| `locationName` | string | ✅ | 位置名称 (最大200) |
| `latitude` | double | ✅ | 纬度 |
| `longitude` | double | ✅ | 经度 |
| `description` | string | ❌ | 描述 (最大500) |
| `reporterUserId` | int | ✅ | 报告者用户ID |

**成功响应** `200`: 返回创建的记录数据

---

### 4.2 获取动物的目击记录

| 项目 | 说明 |
|------|------|
| **URL** | `GET /api/sightings/getAnimalSightings/{animalId}` |

**成功响应** `200`: 返回目击记录数组

---

## 5. 喂养模块 (Feeding)

> 路由前缀: `/api/feeding`

### 5.1 创建喂养记录

| 项目 | 说明 |
|------|------|
| **URL** | `POST /api/feeding/createFeedingLog` |
| **Content-Type** | `multipart/form-data` |
| **需要 Token** | ❌ |

**请求参数 (Form-Data)**:

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `animalId` | int | ✅ | 动物ID |
| `userId` | int | ✅ | 喂养人ID |
| `proofImage` | File | ❌ | 证明照片 |
| `description` | string | ❌ | 描述 (最大500) |
| `locationName` | string | ✅ | 位置名称 (最大200) |
| `latitude` | double | ✅ | 纬度 |
| `longitude` | double | ✅ | 经度 |

**成功响应** `200`:
```json
{
  "message": "...",
  "data": { FeedingLogDto }
}
```

---

## 6. 领养模块 (Adoption)

> 路由前缀: `/api/adoption`

### 6.1 提交领养申请

| 项目 | 说明 |
|------|------|
| **URL** | `POST /api/adoption/apply` |
| **Content-Type** | `application/json` |
| **需要 Token** | ✅ |
| **角色要求** | `User` |

**请求体 (JSON)**:
```json
{
  "animalId": 1,
  "message": "string (必填, 最大1000)"
}
```

**成功响应** `200`:
```json
{ "message": "Application submitted successfully." }
```

**失败响应**:
- `401` — Token 无效
- `404` — 动物不存在
- `400` — 其他错误

---

## 7. 收容所模块 (Shelter)

> 路由前缀: `/api/shelter`  
> 🔒 **所有接口需要 Token 且角色为 `Shelter`**

### 7.1 动物入所

| 项目 | 说明 |
|------|------|
| **URL** | `POST /api/shelter/intake/{animalId}` |
| **Content-Type** | `multipart/form-data` |

**请求参数 (Form-Data)**:

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `newProfileImage` | File | ❌ | 新头像图片 |

**成功响应** `200`:
```json
{ "message": "...", "data": { ... } }
```

---

### 7.2 查看领养申请列表

| 项目 | 说明 |
|------|------|
| **URL** | `GET /api/shelter/applications` |

**成功响应** `200`: 返回领养申请数组

---

### 7.3 批准领养申请

| 项目 | 说明 |
|------|------|
| **URL** | `POST /api/shelter/applications/{appId}/approve` |

**成功响应** `200`:
```json
{ "message": "Application approved." }
```

---

### 7.4 拒绝领养申请

| 项目 | 说明 |
|------|------|
| **URL** | `POST /api/shelter/applications/{appId}/reject` |
| **Content-Type** | `application/json` |

**请求体 (JSON)**:
```json
{ "reason": "string (必填)" }
```

---

### 7.5 查看收容所自己的动物

| 项目 | 说明 |
|------|------|
| **URL** | `GET /api/shelter/animals` |

**成功响应** `200`: 返回动物数组

---

## 8. 志愿者模块 (Volunteer)

> 路由前缀: `/api/volunteer`  
> 🔒 **所有接口需要 Token 且角色为 `Volunteer`**

### 8.1 获取我的任务

| 项目 | 说明 |
|------|------|
| **URL** | `GET /api/volunteer/my-tasks` |

**成功响应** `200`: 返回任务数组

---

### 8.2 认领任务

| 项目 | 说明 |
|------|------|
| **URL** | `POST /api/volunteer/tasks/{taskId}/claim` |

**成功响应** `200`:
```json
{ "message": "Task claimed successfully." }
```

---

### 8.3 完成任务

| 项目 | 说明 |
|------|------|
| **URL** | `POST /api/volunteer/tasks/{taskId}/complete` |
| **Content-Type** | `multipart/form-data` |

**请求参数 (Form-Data)**:

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `taskId` | int | ✅ | 任务ID |
| `proofImage` | File | ❌ | 完成证明照片 |
| `receiptImage` | File | ❌ | 收据照片 |
| `expenseAmount` | decimal | ❌ | 花费金额 (默认0) |

---

### 8.4 放弃任务

| 项目 | 说明 |
|------|------|
| **URL** | `POST /api/volunteer/tasks/{taskId}/abandon` |

**成功响应** `200`:
```json
{ "message": "Task abandoned successfully." }
```

---

## 9. 医疗众筹模块 (Medical)

> 路由前缀: `/api/medical`

### 9.1 设置众筹目标

| 项目 | 说明 |
|------|------|
| **URL** | `POST /api/medical/set-goal` |
| **Content-Type** | `application/json` |

**请求体 (JSON)**:
```json
{
  "animalId": 1,
  "goalType": "string (必填, Vaccine/Neuter, 最大50)",
  "goalAmount": 500.00
}
```

**成功响应** `200`: `{ "message": "众筹目标设置成功" }`  
**失败响应** `404`: `{ "message": "动物不存在" }`

---

### 9.2 捐赠

| 项目 | 说明 |
|------|------|
| **URL** | `POST /api/medical/donate` |
| **Content-Type** | `application/json` |

**请求体 (JSON)**:
```json
{
  "userId": 1,
  "animalId": 1,
  "amount": 100.00
}
```

**成功响应** `200`:
```json
{ "message": "捐赠成功", "donationId": 1, "amount": 100.00 }
```

---

### 9.3 认领医疗任务

| 项目 | 说明 |
|------|------|
| **URL** | `POST /api/medical/claim-task` |
| **Content-Type** | `application/json` |

**请求体 (JSON)**:
```json
{
  "animalId": 1,
  "volunteerId": 2,
  "taskType": "string (必填, Vaccine/Neuter)"
}
```

**成功响应** `200`:
```json
{ "message": "任务认领成功", "taskId": 1 }
```

---

### 9.4 完成医疗任务

| 项目 | 说明 |
|------|------|
| **URL** | `POST /api/medical/complete-task` |
| **Content-Type** | `application/json` |

**请求体 (JSON)**:
```json
{
  "taskId": 1,
  "expenseAmount": 200.00
}
```

> ⚠️ 注意：此接口的 `CompleteTaskDto` 还支持 `proofImage` 和 `receiptImage` 文件字段，但此处用 JSON 提交时不含文件。

---

### 9.5 审批医疗任务

| 项目 | 说明 |
|------|------|
| **URL** | `POST /api/medical/approve-task/{taskId}` |

**成功响应** `200`: `{ "message": "任务审批成功" }`

---

## 10. 管理员模块 (Admin)

> 路由前缀: `/api/admin`  
> 🔒 **所有接口需要 Token 且角色为 `Admin`**

### 10.1 查看待审核收容所

| 项目 | 说明 |
|------|------|
| **URL** | `GET /api/admin/pending-shelters` |

**成功响应** `200`: 返回待审核收容所数组

---

### 10.2 批准收容所

| 项目 | 说明 |
|------|------|
| **URL** | `POST /api/admin/approve-shelter/{userId}` |

---

### 10.3 拒绝收容所

| 项目 | 说明 |
|------|------|
| **URL** | `POST /api/admin/reject-shelter/{userId}` |

---

### 10.4 查看流浪动物领养申请

| 项目 | 说明 |
|------|------|
| **URL** | `GET /api/admin/stray-applications` |

---

### 10.5 批准领养申请

| 项目 | 说明 |
|------|------|
| **URL** | `POST /api/admin/applications/{appId}/approve` |

---

### 10.6 拒绝领养申请

| 项目 | 说明 |
|------|------|
| **URL** | `POST /api/admin/applications/{appId}/reject` |
| **Content-Type** | `application/json` |

**请求体 (JSON)**:
```json
{ "reason": "string (必填)" }
```

---

## 11. 通用响应格式

### 成功响应
```json
// 情况 A: 带 message + data
{ "message": "操作成功", "data": { ... } }

// 情况 B: 只有 message
{ "message": "操作成功" }

// 情况 C: 直接返回数据（列表/对象）
[ { ... }, { ... } ]
```

### 错误响应

项目使用 **全局异常处理中间件 (GlobalExceptionMiddleware)**，所有错误响应统一格式：

```json
{
  "message": "错误描述（对前端友好）",
  "errorCode": "BAD_REQUEST",
  "traceId": "0HN1234..."
}
```

> 开发环境额外附带 `detail` 字段（堆栈信息），生产环境自动隐藏。

**errorCode 对照表**:

| HTTP 状态码 | errorCode | 触发条件 |
|-------------|-----------|---------|
| `400` | `BAD_REQUEST` | 参数错误 / ArgumentException |
| `401` | `UNAUTHORIZED` | Token 缺失/无效 |
| `403` | — | 角色不匹配（Forbid） |
| `404` | `NOT_FOUND` | 资源不存在 / KeyNotFoundException |
| `409` | `CONFLICT` | 操作冲突 / InvalidOperationException |
| `500` | `INTERNAL_ERROR` | 服务器内部错误 |

---

## 12. Java 调用示例

### 12.1 使用 HttpURLConnection 登录

```java
import java.io.*;
import java.net.*;

public class ApiClient {
    private static final String BASE_URL = "https://your-host/api";
    
    /**
     * 用户登录，返回 JWT Token
     */
    public static String login(String email, String password) throws Exception {
        URL url = new URL(BASE_URL + "/auth/login");
        HttpURLConnection conn = (HttpURLConnection) url.openConnection();
        conn.setRequestMethod("POST");
        conn.setRequestProperty("Content-Type", "application/json; charset=UTF-8");
        conn.setDoOutput(true);
        
        // 构造 JSON 请求体
        String json = String.format(
            "{\"email\":\"%s\",\"password\":\"%s\"}", email, password);
        
        try (OutputStream os = conn.getOutputStream()) {
            os.write(json.getBytes("UTF-8"));
        }
        
        int code = conn.getResponseCode();
        InputStream is = (code == 200) ? conn.getInputStream() : conn.getErrorStream();
        BufferedReader br = new BufferedReader(new InputStreamReader(is, "UTF-8"));
        StringBuilder sb = new StringBuilder();
        String line;
        while ((line = br.readLine()) != null) {
            sb.append(line);
        }
        br.close();
        
        if (code == 200) {
            // 解析 JSON 中的 token 字段
            // 建议使用 Gson 或 Jackson: JsonObject obj = JsonParser.parseString(sb.toString()).getAsJsonObject();
            // return obj.get("token").getAsString();
            return sb.toString(); // 返回完整 JSON
        } else {
            throw new RuntimeException("Login failed: " + sb.toString());
        }
    }
}
```

### 12.2 带 Token 发送 GET 请求

```java
/**
 * 发送带 JWT Token 的 GET 请求
 */
public static String getWithToken(String path, String token) throws Exception {
    URL url = new URL(BASE_URL + path);
    HttpURLConnection conn = (HttpURLConnection) url.openConnection();
    conn.setRequestMethod("GET");
    conn.setRequestProperty("Authorization", "Bearer " + token);
    conn.setRequestProperty("Accept", "application/json");
    
    int code = conn.getResponseCode();
    InputStream is = (code == 200) ? conn.getInputStream() : conn.getErrorStream();
    BufferedReader br = new BufferedReader(new InputStreamReader(is, "UTF-8"));
    StringBuilder sb = new StringBuilder();
    String line;
    while ((line = br.readLine()) != null) {
        sb.append(line);
    }
    br.close();
    return sb.toString();
}

// 使用示例
// String animals = getWithToken("/animal/getAnimals", token);
// String myTasks = getWithToken("/volunteer/my-tasks", token);
```

### 12.3 带 Token 发送 POST JSON 请求

```java
/**
 * 发送带 JWT Token 的 POST JSON 请求
 */
public static String postJsonWithToken(String path, String json, String token) throws Exception {
    URL url = new URL(BASE_URL + path);
    HttpURLConnection conn = (HttpURLConnection) url.openConnection();
    conn.setRequestMethod("POST");
    conn.setRequestProperty("Content-Type", "application/json; charset=UTF-8");
    conn.setRequestProperty("Authorization", "Bearer " + token);
    conn.setDoOutput(true);
    
    try (OutputStream os = conn.getOutputStream()) {
        os.write(json.getBytes("UTF-8"));
    }
    
    int code = conn.getResponseCode();
    InputStream is = (code >= 200 && code < 300) ? conn.getInputStream() : conn.getErrorStream();
    BufferedReader br = new BufferedReader(new InputStreamReader(is, "UTF-8"));
    StringBuilder sb = new StringBuilder();
    String line;
    while ((line = br.readLine()) != null) {
        sb.append(line);
    }
    br.close();
    return sb.toString();
}

// 使用示例: 提交领养申请
// String json = "{\"animalId\":1,\"message\":\"我想领养这只小狗\"}";
// String result = postJsonWithToken("/adoption/apply", json, token);
```

### 12.4 上传文件 (multipart/form-data)

```java
import java.io.*;
import java.net.*;
import java.nio.file.*;

/**
 * 发送 multipart/form-data 请求（含文件上传）
 */
public static String postMultipart(String path, String token,
        Map<String, String> fields, String fileFieldName, File file) throws Exception {
    
    String boundary = "----Boundary" + System.currentTimeMillis();
    URL url = new URL(BASE_URL + path);
    HttpURLConnection conn = (HttpURLConnection) url.openConnection();
    conn.setRequestMethod("POST");
    conn.setRequestProperty("Content-Type", "multipart/form-data; boundary=" + boundary);
    if (token != null) {
        conn.setRequestProperty("Authorization", "Bearer " + token);
    }
    conn.setDoOutput(true);
    
    try (OutputStream os = conn.getOutputStream();
         PrintWriter writer = new PrintWriter(new OutputStreamWriter(os, "UTF-8"), true)) {
        
        // 写入普通字段
        for (Map.Entry<String, String> entry : fields.entrySet()) {
            writer.append("--").append(boundary).append("\r\n");
            writer.append("Content-Disposition: form-data; name=\"")
                  .append(entry.getKey()).append("\"\r\n\r\n");
            writer.append(entry.getValue()).append("\r\n");
        }
        
        // 写入文件字段
        if (file != null && fileFieldName != null) {
            writer.append("--").append(boundary).append("\r\n");
            writer.append("Content-Disposition: form-data; name=\"")
                  .append(fileFieldName).append("\"; filename=\"")
                  .append(file.getName()).append("\"\r\n");
            writer.append("Content-Type: ").append(Files.probeContentType(file.toPath()))
                  .append("\r\n\r\n");
            writer.flush();
            Files.copy(file.toPath(), os);
            os.flush();
            writer.append("\r\n");
        }
        
        writer.append("--").append(boundary).append("--\r\n");
    }
    
    int code = conn.getResponseCode();
    InputStream is = (code >= 200 && code < 300) ? conn.getInputStream() : conn.getErrorStream();
    BufferedReader br = new BufferedReader(new InputStreamReader(is, "UTF-8"));
    StringBuilder sb = new StringBuilder();
    String line;
    while ((line = br.readLine()) != null) {
        sb.append(line);
    }
    br.close();
    return sb.toString();
}

// 使用示例: 创建目击报告
// Map<String, String> fields = new HashMap<>();
// fields.put("locationName", "中央公园");
// fields.put("latitude", "31.2");
// fields.put("longitude", "121.5");
// fields.put("reporterUserId", "1");
// fields.put("animalId", "5");
// File photo = new File("sighting.jpg");
// String result = postMultipart("/sightings/createSighting", null, fields, "imageFile", photo);
```

---

## 角色权限总结

| 角色 | 可访问模块 |
|------|-----------|
| **无需登录** | Auth（登录/注册）、Animal、Breed、Sightings、Feeding、Medical |
| **User** | 以上 + Adoption (提交领养申请) |
| **Volunteer** | 以上 + Volunteer (任务管理) |
| **Shelter** | 以上 + Shelter (动物入所、领养审批) |
| **Admin** | 以上 + Admin (收容所审核、领养审批) |
