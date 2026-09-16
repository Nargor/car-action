# 🏁 Car Race Game (car-action) — Project Notes

> **GitHub Repository**: [https://github.com/Nargor/car-action](https://github.com/Nargor/car-action)
> **สร้างด้วย**: Unity 6000.6.0f1, URP (Universal Render Pipeline)
> **Unity Project Path**: `C:/Users/tomdi/My project/`
> **Workspace**: `D:/works/carrace/`
> **อัปเดตล่าสุด**: 2026-09-14

---

## 🔗 ลิงก์โปรเจกต์บน GitHub
- **Repository URL**: `https://github.com/Nargor/car-action`
- **Clone URL**: `https://github.com/Nargor/car-action.git`
- **Branch**: `main`

---

## ⏸️ ระบบ PAUSE MENU (กดปุ่ม ESC)
- กด **`ESC`** ระหว่างการแข่งขันเพื่อหยุดเกมชั่วคราว (`Time.timeScale = 0f`)
- หากกด `ESC` ขณะที่หน้าต่าง Scoreboard (TAB) เปิดอยู่ ระบบจะปิด Scoreboard ก่อน
- หน้าต่าง Pause มีปุ่มควบคุมครบครัน:
  1. **RESUME**: คืนค่าเวลา `Time.timeScale = 1f` แข่งต่อทันที (หรือกด `ESC` ซ้ำ)
  2. **RESTART**: รีเซ็ตการแข่งขันรอบเดิมใหม่ทันที
  3. **MAIN MENU**: หยุดการแข่ง เคลียร์รถแข่งทั้งหมด และพากลับสู่หน้า Title Screen
  4. **EXIT GAME**: ปิดเกมออกจากโปรแกรม

---

## 🏗️ ไฟล์สคริปต์ BUILD อัตโนมัติ (คลิกเดียว Build ได้เลย)
มีไฟล์ batch วางไว้ที่ Root ของโปรเจกต์และ Workspace:
1. **`build.bat`**: ดับเบิ้ลคลิกเพื่อ Build เกมเวอร์ชัน **Windows PC (Standalone 64-bit)**
   - ผลลัพธ์: `Builds/PC/car-action.exe`
   - บันทึก Log: `Builds/build_windows.log`
2. **`build_web.bat`**: ดับเบิ้ลคลิกเพื่อ Build เกมเวอร์ชัน **WebGL (WebAssembly / Browser)**
   - ผลลัพธ์: `Builds/WebGL/index.html`
   - บันทึก Log: `Builds/build_webgl.log`

---

## 🛡️ การตั้งค่า `.gitignore`
- ป้องกันโฟลเดอร์ `Builds/`, `Build/`, `*.exe`, `*.wasm`, `*.data`, และไฟล์ `*.log` ไม่ให้ถูก push ขึ้น GitHub
- ไฟล์สคริปต์ `build.bat`, `build_web.bat` และไฟล์ซอร์สโค้ดทั้งหมดถูกเก็บไว้บน GitHub ครบถ้วน
