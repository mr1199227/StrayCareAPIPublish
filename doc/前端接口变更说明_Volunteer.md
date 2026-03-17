# 前端接口变更说明 - 志愿者申请流程 (Volunteer Application)

> **注意**：原有的 "志愿者注册" (`/api/auth/register-volunteer`) 逻辑已弃用。现在的逻辑是：用户必须先注册普通账号登陆，然后通过"申请"接口升级为志愿者。

## 1. 核心变更概览

- **移除**：不再需要独立的"注册志愿者"页面。
- **新增**："申请成为志愿者"页面（仅限已登录用户访问）。
- **逻辑**：
    1. 用户先通过普通 `/api/auth/register` 注册账号。
    2. 登录后，访问"申请志愿者"页面，填写技能信息。
    3. 调用 `/api/auth/apply-volunteer` 提交申请。
    4. 申请提交后，后台需审核。审核期间用户登录仍显示为普通用户。

---

## 2. API 接口详情

### 2.1 申请成为志愿者
**Endpoint**: `POST /api/auth/apply-volunteer`
**Auth**: Required (Bearer Token)

**请求体 (Request Body)**:
```json
{
  "userId": 123,                // 当前登录用户的 ID
  "skills": "Dog Training, First Aid", // 技能描述
  "availability": "Weekends",   // 时间安排
  "hasVehicle": true,           // 是否有车
  "experienceLevel": "Intermediate" // 经验等级
}
```

**响应 (Response)**:
- `200 OK`: `{"message": "Volunteer application submitted successfully..."}`
- `400 Bad Request`: 用户不存在或已经是志愿者。

---

### 2.2 管理员审核接口 (Admin Only)

#### 获取待审核列表
**Endpoint**: `GET /api/admin/pending-volunteers`
**Response**: 
```json
[
  {
    "userId": 123,
    "username": "john_doe",
    "skills": "Dog Training",
    "status": "Pending" 
    // ...
  }
]
```

#### 批准申请
**Endpoint**: `POST /api/admin/approve-volunteer/{userId}`

#### 拒绝申请
**Endpoint**: `POST /api/admin/reject-volunteer/{userId}`

---

## 3. 前端修改建议 (Frontend Tasks)

1.  **废弃**原有的 `RegisterVolunteer` 页面及调用。
2.  **User Profile / Dashboard**:
    - 添加一个 "Become a Volunteer" 按钮。
    - 仅当用户角色为 `User` 时显示。
3.  **创建申请表单 (Application Form)**:
    - 包含：Skills, Availability, Vehicle Checkbox, Experience Level。
    - 提交到 `/api/auth/apply-volunteer`。
4.  **Admin Dashboard**:
    - 增加 "Pending Volunteers" 标签页。
    - 调用列表接口，并提供 "Approve" 和 "Reject" 操作按钮。
