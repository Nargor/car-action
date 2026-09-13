# 🏁 car-action — High-Speed Car Race & TikTok Live Interactive Game

[![Unity 6](https://img.shields.io/badge/Unity-6000.6.0f1-black?logo=unity)](https://unity.com/)
[![Render Pipeline](https://img.shields.io/badge/Render%20Pipeline-URP%2017.6-blue)](https://unity.com/srp/Universal-Render-Pipeline)
[![GitHub Repo](https://img.shields.io/badge/GitHub-car--action-brightgreen?logo=github)](https://github.com/Nargor/car-action)

เกมแข่งรถ 3 มิติความเร็วสูงบน Unity 6 (URP) มาพร้อมฟิสิกส์การขับขี่สมจริง, สนามแข่ง Procedural ความยาว 12,000 เมตร, และระบบเชื่อมต่อ **TikTok Live Interactive** ให้ผู้ชมพิมพ์ร่วมแข่งขัน ส่งของขวัญกดไนตรัส และแกล้งรถคู่แข่งได้แบบสดๆ!

---

## 🌟 ฟีเจอร์หลัก (Key Features)

### 1. 🏎️ 3 โหมดการแข่งขัน (Game Modes)
- **PLAY VS AI**: ผู้เล่นลงไปขับประลองความเร็วกับคู่แข่ง AI (ปรับแต่งจำนวนได้ 0–49 คัน)
- **SPECTATOR MODE (AI)**: โหมดผู้สังเกตการณ์ สตรีมเมอร์รับชมการชิงชัยของ AI สูงสุด 50 คัน พร้อมมุมกล้องอิสระ
- **PLAYER LIVE (TIKTOK)**: โหมดไลฟ์สตรีมแบบโต้ตอบ ผู้ชมใน TikTok พิมพ์ `'a'` เพื่อ Join รถลง Starting Grid

### 2. 🔴 ระบบ TikTok Live Interactive Racing
- **พิมพ์ 'a' เพื่อลงแข่ง**: ผู้ชมที่พิมพ์ 'a' ในช่องแชทจะได้รับรถ Ferrari สุ่มสีประจำตัว 1 คัน ไปจอดเข้าแถวที่ Starting Grid (สูงสุด 50 คัน)
- **ป้ายชื่อและโปรไฟล์ลอยเหนือรถ (Overhead Billboard UI)**: แสดงชื่อและรูปโปรไฟล์ลอยอยู่เหนือหลังคารถ หันหน้าตามกล้องตลอดเวลา
- **ระบบของขวัญไนตรัส (Nitro Boost ⚡)**: ส่ง Gift (Rose, TikTok) เพื่อเร่งสปีดทะลุ 250+ km/h ชั่วขณะ
- **ระบบแกล้งรถ (Prank 🍌)**: สั่งแกล้งรถคู่แข่งให้หมุนติ้วท้ายปัด (Spinout) หรือเบรกฉุกเฉิน
- **Lock Join on Start**: เมื่อกดเริ่มแข่ง ระบบจะปิดรับการ Join อัตโนมัติ ป้องกันรถลงแทรกระหว่างแข่ง
- **Dev Simulator ในตัว**: มีปุ่มจำลองคนพิมพ์ 'a', ไนตรัส, และแกล้งรถ ให้ทดสอบได้ทันทีโดยไม่ต้องเปิดไลฟ์จริง

### 3. 🎥 ระบบกล้องถ่ายทอดสด 5 มุมมอง (Camera System)
- **FPS**: มุมมองในห้องโดยสารหลังพวงมาลัย
- **TPS / TPS2**: มุมมองบุคคลที่สามตามท้ายรถ (ระยะใกล้และไกล)
- **BIRDEYE**: มุมมองมุมสูงจากท้องฟ้า
- **NOCLIP**: บินสำรวจสนามแข่งได้อย่างอิสระ
- **Orbit Mouse Drag**: คลิกเมาส์ค้างตรงกลางจอเพื่อหมุนมุมกล้อง 360 องศารอบตัวรถได้อิสระ

### 4. 📊 Leaderboard แบบ Drag & Drop
- กระดานอันดับ Real-time ทางฝั่งขวา เลื่อน Scroll ดูนักแข่งได้ครบ 50 คน
- คลิกที่แถวชื่อนักแข่งเพื่อ**สลับกล้องไปจับรถคันนั้นได้ทันที**
- คลิกเมาส์ลากย้ายตำแหน่งหน้าต่าง Leaderboard ไปวางตรงไหนของจอก็ได้ตามสะดวก

### 5. 🌐 In-Engine Web-based UI (HTML / CSS / JavaScript)
- ไฟล์ UI ถูกแยกไว้ใน `Assets/WebUI/` (`index.html`, `style.css`, `app.js`)
- แก้ไขสี ฟอนต์ หรือปุ่มได้ง่ายดายด้วยมาตรฐาน Web
- **ทำงานภายในตัวเกม 100% (Zero Network Ports)** ไม่มีการเปิดหรือจอง Port ใดๆ ในระบบปฏิบัติการ
- **Live Hot-Reload**: แก้ไขไฟล์ HTML/CSS แล้วเซฟ ตัวเกมจะอัปเดตหน้าตา UI สดๆ ทันที

### 6. 🇹🇭 แก้ไขภาษาไทยและปัญหาสระลอย (Thai Font & Vowels Fix)
- ใช้ฟอนต์ **Leelawadee UI** (`ThaiFont_SDF.asset`) มาตรฐานภาษาไทย
- มีระบบ **`ThaiFontAdjuster.cs`** จัดตำแหน่งสระบน วรรณยุกต์ และพยัญชนะหางยาว (ป, ผ, ฝ, ฟ) ไม่ให้ลอยสูงหรือซ้อนทับกัน

---

## 🎮 การควบคุม (Controls)

| ปุ่ม | การทำงาน |
|---|---|
| **W / ↑** | เร่งความเร็ว (Accelerate) |
| **S / ↓** | เบรก / ถอยหลัง (Brake / Reverse) |
| **A / D / ← / →** | เลี้ยวซ้าย / ขวา (Steer) |
| **Spacebar** | เบรกมือ (Handbrake) |
| **1 / 2 / 3 / 4 / 5** | เลือกมุมกล้อง (FPS, TPS, TPS2, Birdeye, NoClip) |
| **C** | สลับมุมกล้องวนลูป (Cycle Camera) |
| **R** | รีเซ็ตมุมกล้อง (Reset Camera) |
| **คลิกซ้ายค้างที่หน้าจอ** | หมุนมุมกล้อง 360° รอบรถ (Orbit Camera) |
| **คลิกซ้ายค้างที่หัว Leaderboard** | ลากย้ายตำแหน่งหน้าต่าง Leaderboard (Drag & Drop) |

---

## 📁 โครงสร้างโปรเจกต์ (Project Structure)

```
Assets/
├── WebUI/                  ← ไฟล์ HTML, CSS, JS สำหรับตกแต่ง UI
│   ├── index.html
│   ├── style.css
│   └── app.js
├── Scripts/                ← โค้ด C# ทั้งหมดของเกม
│   ├── CarController.cs    ← ฟิสิกส์รถ AWD, ESP, TCS, Downforce
│   ├── AICarController.cs  ← AI ขับรถ, Waypoints, หลบหลีก, Nitro, Prank
│   ├── RaceManager.cs      ← จัดการแข่งขัน 3 โหมด, Grid 50 คัน, สุ่มสี
│   ├── MenuManager.cs      ← หน้า Title, Lobby, TikTok Lobby, UI Switcher
│   ├── TikTokLiveManager.cs← จัดการแชท TikTok 'a', Gift ไนตรัส, แกล้งรถ
│   ├── RacerOverheadUI.cs  ← ป้ายชื่อและรูปโปรไฟล์ลอยเหนือหลังคารถ
│   ├── ChaseCameraController.cs ← กล้อง 5 โหมด + Orbit Drag
│   ├── LeaderboardUI.cs    ← กระดานอันดับ Real-time + คลิกสลับกล้อง
│   ├── UIDragController.cs ← ระบบลากย้ายหน้าต่าง UI
│   ├── LapTimer.cs         ← ระบบนับรอบและจับเวลา
│   ├── HUDController.cs    ← เข็มไมล์ดิจิทัลตรงกลาง, สถานะรอบ, ตัวนับถอยหลัง
│   ├── HtmlUIController.cs ← ตัวอ่าน HTML/CSS ทำงานในเกม 100% (No Ports)
│   ├── ThaiFontAdjuster.cs ← อัลกอริทึมแก้ปัญหาสระลอย/วรรณยุกต์ลอย
│   └── ThaiTextAutoFix.cs  ← ปรับแต่งสระ/วรรณยุกต์อัตโนมัติบน TextMeshPro
├── Cars/                   ← โมเดล 3D Ferrari 458 Spider และชิ้นส่วนล้อ
├── Fonts/                  ← ฟอนต์ภาษาไทย Leelawadee UI และ ThaiFont_SDF
└── Scenes/
    └── RaceTrackScene.unity← Scene หลักของเกม
```

---

## 🚀 ความต้องการของระบบ (Requirements)
- **Unity Editor**: `6000.6.0f1` หรือใหม่กว่า
- **Render Pipeline**: Universal Render Pipeline (URP 17.6+)
- **OS**: Windows 10 / 11 (64-bit)

---

## 📄 License
This project is open-source and created for interactive streaming & racing game development.
