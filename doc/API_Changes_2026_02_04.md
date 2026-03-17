# StrayCare API 接口更新说明 (2026-02-04)

本文档汇总了 2026年2月4日 新增的后端接口，供前端开发人员参考。

## 1. 用户个人资料 (User Profile)

### 1.1 编辑个人资料 (Edit Profile)
允许登录用户更新自己的个人信息和头像。

*   **URL**: `/api/auth/edit-profile`
*   **Method**: `POST`
*   **Auth**: Required (Bearer Token)
*   **Content-Type**: `multipart/form-data`

**Request Parameters (Form Data):**

| Key | Type | Required | Description |
| :--- | :--- | :--- | :--- |
| `FullName` | String | No | 用户真实姓名 |
| `PhoneNumber` | String | No | 电话号码 |
| `Address` | String | No | 地址 |
| `ProfileImage` | File | No | 头像文件 (.jpg, .png, .webp, Max 5MB) |
| `VolunteerData.Skills` | String | No | (志愿者) 技能描述 |
| `VolunteerData.Availability` | String | No | (志愿者) 空闲时间 |
| `VolunteerData.HasVehicle` | Boolean | No | (志愿者) 是否有车 |
| `VolunteerData.ExperienceLevel` | String | No | (志愿者) 经验 (Beginner/Intermediate/Expert) |
| `ShelterData.ShelterName` | String | No | (收容所) 名称 |
| `ShelterData.Address` | String | No | (收容所) 地址 |
| `ShelterData.Description` | String | No | (收容所) 描述 |
| `ShelterData.Latitude` | Double | No | (收容所) 纬度 |
| `ShelterData.Longitude` | Double | No | (收容所) 经度 |

**Response (Success 200):**
```json
{
  "message": "Profile updated successfully.",
  "data": {
    "userId": 1,
    "username": "user1",
    "email": "user1@example.com",
    "fullName": "New Name",
    "phoneNumber": "1234567890",
    "address": "New Address",
    "profileImageUrl": "/uploads/profiles/guid.jpg",
    "role": "User",
    "balance": 100.00,
    "profileData": { ... } // Volunteer/Shelter data if applicable
  }
}
```

### 1.2 获取当前用户资料 (Get Current User Profile)
获取当前登录用户的完整个人信息，**包含角色专属数据**。

*   **URL**: `/api/auth/me`
*   **Method**: `GET`
*   **Auth**: Required (Bearer Token)

**Response (Success 200) - User:**
```json
{
  "userId": 4,
  "username": "testuser",
  "email": "test@example.com",
  "fullName": "Test User",
  "phoneNumber": "123456789",
  "address": "123 Main St",
  "profileImageUrl": "/uploads/profiles/abc123.jpg",
  "role": "User",
  "balance": 100.00,
  "isActive": true,
  "isApproved": true,
  "profileData": null
}
```

**Response (Success 200) - Volunteer:**
```json
{
  "userId": 5,
  "username": "volunteer1",
  "email": "vol@example.com",
  "fullName": "John Volunteer",
  "phoneNumber": "987654321",
  "address": "456 Oak Ave",
  "profileImageUrl": "/uploads/profiles/def456.jpg",
  "role": "Volunteer",
  "balance": 50.00,
  "isActive": true,
  "isApproved": true,
  "profileData": {
    "id": 1,
    "userId": 5,
    "skills": "Dog Walking, First Aid",
    "availability": "Weekends",
    "hasVehicle": true,
    "experienceLevel": "Intermediate"
  }
}
```

**Response (Success 200) - Shelter:**
```json
{
  "userId": 6,
  "username": "shelter1",
  "email": "shelter@example.com",
  "fullName": "Happy Paws Shelter",
  "phoneNumber": "555-1234",
  "address": "789 Shelter Rd",
  "profileImageUrl": "/uploads/profiles/ghi789.jpg",
  "role": "Shelter",
  "balance": 500.00,
  "isActive": true,
  "isApproved": true,
  "profileData": {
    "id": 1,
    "userId": 6,
    "shelterName": "Happy Paws Animal Shelter",
    "address": "789 Shelter Road, City",
    "latitude": 23.1234,
    "longitude": 113.5678,
    "description": "Caring for animals since 2010"
  }
}
```

---

## 2. 管理员用户管理 (Admin User Management)

### 2.1 获取用户列表 (Get All Users)
获取系统中所有普通用户（非管理员）的列表。

*   **URL**: `/api/admin/users`
*   **Method**: `GET`
*   **Auth**: Required (Admin Role)

**Response (Success 200):**
```json
[
  {
    "id": 4,
    "username": "testuser",
    "email": "test@example.com",
    "fullName": "Test User",
    "role": "User",
    "isApproved": true,
    "isActive": true,  // true=正常, false=被封禁
    "phoneNumber": "123456",
    "phoneNumber": "123456",
    "profileImageUrl": null
  }
]
```

### 2.2 获取用户详情 (Get User Details)
获取指定用户的完整信息，包括 Volunteer 或 Shelter 的详细资料。

*   **URL**: `/api/admin/users/{userId}/details`
*   **Method**: `GET`
*   **Auth**: Required (Admin Role)

**Response (Volunteer):**
```json
{
  "id": 4,
  "username": "volunteer1",
  "fullName": "Volunteer One",
  "role": "Volunteer",
  "isApproved": true,
  "isActive": true,
  "profileData": {
    "id": 1,
    "skills": "Dog Walking",
    "availability": "Weekends",
    "hasVehicle": true,
    "experienceLevel": "Expert"
  }
}
```

### 2.2 封禁用户 (Block User)
封禁指定用户，封禁后用户无法登录。

*   **URL**: `/api/admin/users/{userId}/block`
*   **Method**: `POST`
*   **Auth**: Required (Admin Role)

**Response (Success 200):**
```json
{
  "message": "User has been blocked."
}
```

### 2.3 解封用户 (Unblock User)
恢复用户的登录权限。

*   **URL**: `/api/admin/users/{userId}/unblock`
*   **Method**: `POST`
*   **Auth**: Required (Admin Role)

**Response (Success 200):**
```json
{
  "message": "User has been unblocked."
}
```

---

## 3. 钱包交易记录 (Wallet Records)

### 3.1 获取所有充值记录 (Admin: Get All Top-ups)
管理员查看所有用户的充值记录。

*   **URL**: `/api/admin/topup-records`
*   **Method**: `GET`
*   **Auth**: Required (Admin Role)

**Response (Success 200):**
```json
[
  {
    "id": 1,
    "userId": 4,
    "userName": "testuser",
    "amount": 100.00,
    "timestamp": "2026-02-04T06:00:00Z",
    "description": "Top-up via CreditCard"
  }
]
```

### 3.2 获取我的交易记录 (User: Get My History)
用户查看自己的钱包流水（包含充值和捐赠）。

*   **URL**: `/api/wallet/history`
*   **Method**: `GET`
*   **Auth**: Required (Bearer Token)

**Response (Success 200):**
```json
[
  {
    "id": 2,
    "amount": -50.00,        // 负数表示支出 (捐赠)
    "transactionType": "Donation",
    "timestamp": "2026-02-04T06:05:00Z",
    "description": "Donated to Animal 1 (Bobby)"
  },
  {
    "id": 1,
    "amount": 100.00,        // 正数表示收入 (充值)
    "transactionType": "Topup",
    "timestamp": "2026-02-04T06:00:00Z",
    "description": "Top-up via CreditCard"
  }
]
```

---

## 3.3 编辑动物资料 (Edit Animal Profile)
允许用户编辑动物信息。采用分级权限控制：

1.  **User / Volunteer**:
    *   **允许**: 修改基本特征 (`Breed`, `Gender`, `Size`, `EstAge`) 和上传 `NewImages`。
    *   **禁止**: 修改名字、状态、医疗、众筹等信息。
2.  **Shelter**:
    *   **允许**: 修改 **Name** (名字) + 上述所有 User 权限字段。
    *   **禁止**: 修改状态、医疗、众筹等信息。
3.  **Admin**:
    *   **允许**: 修改 **所有** 字段 (含 Status, Medical, Funding, Location 等)。

*   **URL**: `/api/animals/{id}`
*   **Method**: `PUT`
*   **Auth**: Required (Admin, or Shelter Owner)
*   **Content-Type**: `multipart/form-data` (if uploading images) or `application/x-www-form-urlencoded`

**Request Body (Form-Data):**
| Field | Type | Required | Description |
| :--- | :--- | :--- | :--- |
| `Name` | String | No | 动物名称 *(Shelter/Admin Only)* |
| `Species` | String | No | 物种 (Dog/Cat) *(Admin Only)* |
| `Status` | String | No | 状态 (Stray, Adopted) *(Admin Only)* |
| `CurrentGoalType` | String | No | 众筹目标类型 (None, Vaccine, Neuter) *(Admin Only)* |
| `GoalStatus` | String | No | 众筹状态 (Fundraising, Completed) *(Admin Only)* |
| `IsVaccinated` | Boolean | No | 是否疫苗 *(Admin Only)* |
| `NewImages` | File[] | No | 新增图片 |

**Response (Success 200):**
```json
{
  "message": "Animal updated successfully",
  "animalId": 10
}
```
