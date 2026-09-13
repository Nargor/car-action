# Thungthao Scoreboard API — คู่มือสำหรับ Customer

Base URL: `https://shop.thungthao.online`

---

## 🔑 การ Authentication

API นี้รองรับ 2 วิธี:

| วิธี | ใช้กับ | วิธีส่ง |
|------|--------|---------|
| **API Token** | Customer | Header: `Authorization: Bearer <token>` |
| **Username/Password** | Admin เท่านั้น | Body: `username` + `password` |

> ⚠️ Customer ใช้ได้เฉพาะ **API Token** เท่านั้น  
> สร้าง token ได้ที่ https://shop.thungthao.online/admin/tokens

---

## 📋 รายการ API

### 1. Insert Score — บันทึกคะแนน

**Endpoint:** `POST /api/fivem/scoreboard/insert`

**Authentication:** ต้องใช้ API Token (Bearer)

**Request Headers:**
```
Authorization: Bearer <your_api_token>
Content-Type: application/json
```

**Request Body:**
```json
{
  "tiktok_username": "string (required) — TikTok username ของผู้เล่น",
  "tiktok_nickname": "string (required) — ชื่อ display ของผู้เล่น",
  "tiktok_streamer_username": "string (required) — TikTok username ของ streamer",
  "tiktok_streamer_nickname": "string (required) — ชื่อ display ของ streamer",
  "score": "number (required) — คะแนนที่จะบันทึก",
  "score_time": "number (optional) — สถิติเวลา (วินาที) ถ้าไม่ส่งจะเป็น null",
  "game_name": "string (optional) — ชื่อเกม"
}
```

**ตัวอย่าง Request (curl):**
```bash
curl -X POST https://shop.thungthao.online/api/fivem/scoreboard/insert \
  -H "Authorization: Bearer your_token_here" \
  -H "Content-Type: application/json" \
  -d '{
     "tiktok_username": "player123",
     "tiktok_nickname": "Player One",
     "tiktok_streamer_username": "streamer_x",
     "tiktok_streamer_nickname": "Streamer X",
     "score": 150,
     "score_time": 45,
     "game_name": "fivem"
   }'
```

**ตัวอย่าง Request (JavaScript/fetch):**
```javascript
const response = await fetch('https://shop.thungthao.online/api/fivem/scoreboard/insert', {
  method: 'POST',
  headers: {
    'Authorization': 'Bearer your_token_here',
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    tiktok_username: 'player123',
    tiktok_nickname: 'Player One',
    tiktok_streamer_username: 'streamer_x',
    tiktok_streamer_nickname: 'Streamer X',
    score: 150,
    score_time: 45,
    game_name: 'fivem'
  })
});
const data = await response.json();
```

**ตัวอย่าง Request (Lua / FiveM):**
```lua
local headers = {
    ['Authorization'] = 'Bearer your_token_here',
    ['Content-Type'] = 'application/json'
}
local body = json.encode({
    tiktok_username = 'player123',
    tiktok_nickname = 'Player One',
    tiktok_streamer_username = 'streamer_x',
    tiktok_streamer_nickname = 'Streamer X',
    score = 150,
    score_time = 45,
    game_name = 'fivem'
})
PerformHttpRequest('https://shop.thungthao.online/api/fivem/scoreboard/insert', function(status, responseText)
    local result = json.decode(responseText)
    if result.status == 'success' then
        print('Score inserted!')
    end
end, 'POST', body, headers)
```

**Response สำเร็จ (201):**
```json
{
  "status": "success",
  "message": "Score inserted successfully",
  "res_code": "000",
  "status_code": 201
}
```

**Response Error:**
```json
{
  "status": "error",
  "message": "Authentication required (provide username+password or Authorization: Bearer <token>)",
  "res_code": "009",
  "status_code": 401
}
```

---

### 2. Get All Scoreboard — ดึงคะแนนทั้งหมด (รวมทุก streamer)

**Endpoint:** `GET /api/fivem/scoreboard/getall`

**Authentication:** ไม่ต้องใช้ — เป็น public API

**Query Parameters:**

| Parameter | Type | Required | Default | คำอธิบาย |
|-----------|------|----------|---------|-----------|
| `page` | number | ❌ | 1 | หน้าที่ต้องการ |
| `limit` | number | ❌ | 10 | จำนวนต่อหน้า |
| `game_name` | string | ❌ | — | กรองตามชื่อเกม |
| `search` | string | ❌ | — | ค้นหาจาก tiktok_username หรือ nickname |
| `sort_by` | string | ❌ | `score` | โหมดการแสดงผล: `score` = เรียงตามคะแนนรวม (default), `score_time` = เรียงตามเวลาน้อยสุด (เฉพาะคนที่มีข้อมูลเวลา) |

**ตัวอย่าง Request:**
```bash
# ดึงทั้งหมด หน้า 1 (เรียงตาม score)
curl "https://shop.thungthao.online/api/fivem/scoreboard/getall"

# กรองตามเกม + paginate
curl "https://shop.thungthao.online/api/fivem/scoreboard/getall?game_name=fivem&page=1&limit=20"

# ค้นหาผู้เล่น
curl "https://shop.thungthao.online/api/fivem/scoreboard/getall?search=player123"

# เรียงตาม score_time น้อยสุด (Time Attack mode)
curl "https://shop.thungthao.online/api/fivem/scoreboard/getall?sort_by=score_time&game_name=fivem"
```

**ตัวอย่าง Request (JavaScript):**
```javascript
const params = new URLSearchParams({ game_name: 'fivem', page: 1, limit: 20 });
const response = await fetch(`https://shop.thungthao.online/api/fivem/scoreboard/getall?${params}`);
const data = await response.json();
```

**Response สำเร็จ (200) — mode `score` (default):**
```json
{
  "status": "success",
  "res_code": "000",
  "status_code": 200,
  "sort_by": "score",
  "data": [
    {
      "tiktok_username": "player123",
      "tiktok_nickname": "Player One",
      "tiktok_streamer_username": "streamer_x",
      "tiktok_streamer_nickname": "Streamer X",
      "game_name": "fivem",
      "total_score": 1500,
      "total_score_time": 380,
      "best_score_time": 42,
      "global_rank": 1
    }
  ],
  "pagination": {
    "total": 100,
    "per_page": 10,
    "current_page": 1,
    "total_pages": 10
  }
}
```

**Response สำเร็จ (200) — mode `score_time`:**
```json
{
  "status": "success",
  "res_code": "000",
  "status_code": 200,
  "sort_by": "score_time",
  "data": [
    {
      "tiktok_username": "speedrunner99",
      "tiktok_nickname": "SpeedRunner",
      "tiktok_streamer_username": "streamer_x",
      "tiktok_streamer_nickname": "Streamer X",
      "game_name": "fivem",
      "total_score": 300,
      "total_score_time": 90,
      "best_score_time": 28,
      "global_rank": 1
    }
  ],
  "pagination": {
    "total": 50,
    "per_page": 10,
    "current_page": 1,
    "total_pages": 5
  }
}
```

> 📝 `total_score` = ผลรวมคะแนนทั้งหมดของผู้เล่น  
> 📝 `total_score_time` = ผลรวมเวลาทั้งหมด (วินาที) ของผู้เล่น  
> 📝 `best_score_time` = **เวลาน้อยสุด** (วินาที) ของผู้เล่น — ใช้เป็นสถิติดีสุด, `null` = ไม่มีข้อมูลเวลา  
> 📝 `global_rank` = อันดับรวมในทุก streamer  
> ⚠️ mode `score_time` จะแสดงเฉพาะผู้เล่นที่มีข้อมูล score_time เท่านั้น (กรอง null ออก)

---

### 3. Get Scoreboard By Streamer — ดึงคะแนนเฉพาะ streamer

**Endpoint:** `GET /api/fivem/scoreboard/getbystreamer`

**Authentication:** ไม่ต้องใช้ — เป็น public API

**Query Parameters:**

| Parameter | Type | Required | Default | คำอธิบาย |
|-----------|------|----------|---------|-----------|
| `username` | string | ✅ | — | TikTok username ของ streamer ที่ต้องการ |
| `page` | number | ❌ | 1 | หน้าที่ต้องการ |
| `limit` | number | ❌ | 10 | จำนวนต่อหน้า |
| `game_name` | string | ❌ | — | กรองตามชื่อเกม |
| `search` | string | ❌ | — | ค้นหาจาก tiktok_username หรือ nickname |
| `sort_by` | string | ❌ | `score` | โหมดการแสดงผล: `score` = เรียงตามคะแนนรวม (default), `score_time` = เรียงตามเวลาน้อยสุด (เฉพาะคนที่มีข้อมูลเวลา) |

**ตัวอย่าง Request:**
```bash
# ดึงคะแนนของ streamer "streamer_x" (เรียงตาม score)
curl "https://shop.thungthao.online/api/fivem/scoreboard/getbystreamer?username=streamer_x"

# กรองตามเกม
curl "https://shop.thungthao.online/api/fivem/scoreboard/getbystreamer?username=streamer_x&game_name=fivem&limit=50"

# ค้นหา
curl "https://shop.thungthao.online/api/fivem/scoreboard/getbystreamer?username=streamer_x&search=player"

# เรียงตาม score_time น้อยสุด (Time Attack mode)
curl "https://shop.thungthao.online/api/fivem/scoreboard/getbystreamer?username=streamer_x&sort_by=score_time&game_name=fivem"
```

**ตัวอย่าง Request (JavaScript):**
```javascript
const params = new URLSearchParams({ username: 'streamer_x', game_name: 'fivem', limit: 50 });
const response = await fetch(`https://shop.thungthao.online/api/fivem/scoreboard/getbystreamer?${params}`);
const data = await response.json();
```

**ตัวอย่าง Request (Lua / FiveM):**
```lua
PerformHttpRequest(
  'https://shop.thungthao.online/api/fivem/scoreboard/getbystreamer?username=streamer_x&game_name=fivem',
  function(status, responseText)
    local result = json.decode(responseText)
    if result.status == 'success' then
      for i, player in ipairs(result.data) do
        print(player.global_rank .. '. ' .. player.tiktok_nickname .. ' — ' .. player.total_score)
      end
    end
  end,
  'GET', '', {}
)
```

**Response สำเร็จ (200):**
```json
{
  "status": "success",
  "res_code": "000",
  "status_code": 200,
  "sort_by": "score",
  "data": [
    {
      "tiktok_username": "player123",
      "tiktok_nickname": "Player One",
      "tiktok_streamer_username": "streamer_x",
      "tiktok_streamer_nickname": "Streamer X",
      "game_name": "fivem",
      "total_score": 800,
      "total_score_time": 120,
      "best_score_time": 35,
      "global_rank": 1
    }
  ],
  "pagination": {
    "total": 25,
    "per_page": 10,
    "current_page": 1,
    "total_pages": 3
  }
}
```

> 📝 `global_rank` = อันดับเฉพาะใน streamer นั้น (ไม่ใช่ rank รวม)  
> 📝 `best_score_time` = เวลาน้อยสุด (วินาที) ของผู้เล่นใน streamer นั้น, `null` = ไม่มีข้อมูลเวลา  
> ⚠️ mode `score_time` จะแสดงเฉพาะผู้เล่นที่มีข้อมูล score_time เท่านั้น

---

## ⚠️ Error Codes

| res_code | HTTP Status | ความหมาย |
|----------|-------------|----------|
| `000` | 200/201 | สำเร็จ |
| `005` | 401 | API token ไม่ถูกต้องหรือถูก disable |
| `008` | 423 | บัญชีถูก ban |
| `009` | 401 | ไม่มี authentication |
| `022` | 404 | ไม่พบข้อมูล |
| `024` | 400 | ขาด field ที่จำเป็น |
| `090` | 500 | Internal server error |
| `091` | 405 | HTTP method ไม่ถูกต้อง |

---

## 💡 Tips สำหรับ Developer

1. **score เป็น additive** — ทุกครั้งที่ insert จะ +score ให้ผู้เล่นคนนั้น ไม่ใช่ set ค่าใหม่
2. **score_time เป็น additive เช่นกัน** — รวมเวลาทั้งหมดของผู้เล่น (วินาที) ถ้าไม่ส่ง score_time จะไม่นับเวลาสำหรับ record นั้น
3. **tiktok_username** ใช้เป็น unique key ในการ aggregate คะแนน
4. **game_name** ใช้แยก scoreboard ระหว่างเกม ถ้าไม่ส่งจะรวมทุกเกม
5. **limit สูงสุด** — ไม่มีการ cap แต่แนะนำไม่เกิน 100 ต่อ request
6. **case-insensitive** — `username` search และ `tiktok_streamer_username` ไม่สนตัวพิมพ์เล็ก/ใหญ่

---

## 🤖 Prompt สำหรับบอก AI ให้ implement

```
ฉันมี API สำหรับ TikTok Scoreboard ดังนี้:

Base URL: https://shop.thungthao.online
Authentication: Header "Authorization: Bearer <TOKEN>"

APIs:
1. POST /api/fivem/scoreboard/insert
   - Body: tiktok_username, tiktok_nickname, tiktok_streamer_username, tiktok_streamer_nickname, score (number), score_time (number, optional, วินาที), game_name (optional)
   - ต้องส่ง Authorization header
   - Response: { status, res_code, status_code, message }

2. GET /api/fivem/scoreboard/getall
   - Query: page, limit, game_name, search
   - ไม่ต้อง auth
   - Response: { status, res_code, data: [...], pagination: { total, per_page, current_page, total_pages } }

3. GET /api/fivem/scoreboard/getbystreamer
   - Query: username (required), page, limit, game_name, search
   - ไม่ต้อง auth
   - Response: { status, res_code, data: [...], pagination: {...} }

data object: { tiktok_username, tiktok_nickname, tiktok_streamer_username, tiktok_streamer_nickname, game_name, total_score, total_score_time, global_rank }

ช่วย implement [บอก AI ว่าอยากทำอะไร เช่น "FiveM Lua resource ที่ส่ง score อัตโนมัติ" หรือ "React component แสดง leaderboard"]
```
