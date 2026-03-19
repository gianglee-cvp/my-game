
link demo : https://gianglee-cvp.itch.io/my-game
# 🚀 Tank Battle

**Tank Battle** là một game bắn tank 2D được phát triển bằng **Unity**.
Người chơi điều khiển một chiếc tank chiến đấu qua các đợt kẻ địch, thu thập item rơi ra khi tiêu diệt quái và sử dụng coin để mua tank mới trong shop.

Game tập trung vào **phản xạ nhanh, chiến thuật di chuyển và quản lý tài nguyên (coin, item, đạn đặc biệt).**

---

# 🎮 Gameplay Overview

Trong game, người chơi sẽ:

* Điều khiển tank di chuyển trên bản đồ
* Bắn hạ các đợt kẻ địch
* Thu thập item rơi ra khi kẻ địch bị tiêu diệt
* Sử dụng **đạn đặc biệt** để gây sát thương mạnh
* Thu thập **coin** để mua tank mới trong shop
* Đánh bại **boss** ở các wave khó

Game sẽ ngày càng khó hơn khi số lượng và sức mạnh của kẻ địch tăng lên.

---

# 🎯 Mục tiêu

* Sống sót qua các wave kẻ địch
* Tiêu diệt boss
* Thu thập coin để nâng cấp tank
* Sử dụng item hợp lý để chiến thắng

---

# 🕹️ Điều khiển

| Phím           | Chức năng        |
| -------------- | ---------------- |
| **W A S D**    | Di chuyển tank   |
| **Chuột**      | Hướng bắn        |
| **Chuột trái** | Bắn đạn thường   |
| **Space**      | Bắn đạn đặc biệt |

### Cơ chế bắn

* **Chuột trái** luôn bắn **đạn thường**
* **Space** bắn **đạn đặc biệt**
* Hai loại đạn **hoạt động độc lập**

---

# 💥 Hệ thống đạn

## 🔹 Đạn thường

* Bắn bằng **chuột trái**
* Không giới hạn số lượng
* Tốc độ bắn cố định
* 3 loại đạn
* **GB_piercer_rocket_root  : hiệu ứng đẩy lùi và bắn xuyên mục tiêu**
* **GB_plougher_bullet_root  : hiệu ứng nổ , sát thương lan sang các enemy trong vùng nổ**
* **Tesla_tank_Bullet  : Gây sát thương và có hiệu ứng choáng làm địch đứng yên và không bắn được nữa**

## ⭐ Đạn đặc biệt

Đạn đặc biệt chỉ có khi người chơi nhặt **item Star**.

Mỗi lần nhặt Star sẽ nhận:

```
10 viên đạn đặc biệt
```

**Đạn đặc biệt có sát thương cao hơn , quỹ đạo vòng vèo bám đuổi mục tiêu**

  

---

# 🎁 Hệ thống Item

Item **không spawn sẵn trên map**.

Item **chỉ xuất hiện khi kẻ địch bị tiêu diệt**.

---

## 🪙 Coin

* Rơi ra khi tiêu diệt enemy
* Dùng để **mua tank trong shop**

---

## ❤️ HP (Cross)

* Hồi máu cho tank

---

## 🛡 Shield

* Kích hoạt **khiên bảo vệ**
* Giúp tank tránh sát thương trong một khoảng thời gian

---

## ⭐ Star

* Cung cấp **10 viên đạn đặc biệt**
* Cho phép người chơi bắn bằng **Space**

---

# 👾 Kẻ địch

Trong game có nhiều loại kẻ địch:

### Enemy thường

* Di chuyển và bắn về phía người chơi

### Enemy tấn công xa

* Có thể bắn từ khoảng cách xa

### Enemy thả bom

* Có thể tạo ra các vụ nổ nguy hiểm

### Boss

* Xuất hiện ở các wave lớn
* Máu nhiều và sát thương cao
* Có các hiệu ứng bắn đạn đặc biệ : bắn nhiều đạn bắn nhiều hướng, bắn đạn đặc biệt 

---

# 🌊 Hệ thống Wave

Game được chia thành **các wave kẻ địch**.

Mỗi wave:

* Số lượng enemy tăng
* Enemy mạnh hơn
* Có thể xuất hiện **boss**

---

# 🛒 Shop

Người chơi có thể sử dụng **coin** để mua **tank mới** trong shop.

Tank mới có thể có:

* Máu cao hơn
* Sát thương mạnh hơn
* Khả năng chiến đấu tốt hơn

---

# 💡 Mẹo chơi

* Luôn di chuyển để tránh đạn từ kẻ địch
* Sử dụng **đạn đặc biệt** khi gặp nhiều enemy hoặc boss
* Nhặt **shield** trước khi lao vào combat
* Tích lũy **coin** để mua tank mạnh hơn
* Không nên dùng hết đạn đặc biệt quá sớm

---

# ⚙️ Technical Notes

* Game được phát triển bằng **Unity Engine**
* Ngôn ngữ lập trình: **C#**
* Item được **drop ngẫu nhiên khi enemy chết**
* Hệ thống bắn hỗ trợ **đạn thường và đạn đặc biệt hoạt động độc lập**

---

# 📦 Build & Run

1. Clone project:

```
git clone https://github.com/gianglee-cvp/my-game.git
```

2. Mở project bằng **Unity**

3. Chạy scene chính của game.

---

# 👨‍💻 Author

**Lê Trường Giang**
Sinh viên – Đại học Bách Khoa Hà Nội
Unity Developer
