# Hướng dẫn tự sửa GUI

GUI của game **được dựng bằng code lúc chạy**, không nằm sẵn trong scene. Vì vậy:

- Đổi **ảnh** → thay file PNG (không cần code).
- Đổi **màu, font, vị trí, kích thước, chữ** → sửa vài con số trong code (các dòng ghi bên dưới).
- Muốn **thử trực tiếp** → chỉnh trong Play mode, ưng rồi chép số vào code (mục 6).

## 1. File nào làm gì

| File (`Assets/Scripts/UI/`) | Nội dung |
|---|---|
| `UiFactory.cs` | Phong cách chung: màu, font, cách vẽ nút / panel / chữ |
| `MainMenu.cs` | Màn hình chính: tiêu đề, nút PLAY / SETTINGS / QUIT |
| `GameHud.cs` | Trong trận: thanh trên, bảng KINGS, nút CONFIRM, pause, banner, màn đổi lượt, màn thắng, menu tạm dừng |
| `SettingsPanel.cs` | Bảng Settings |
| `CardHand.cs` | Tay bài và bảng xem thẻ (chuột phải) |

## 2. Đổi ảnh (dễ nhất)

Thay file trong `Assets/Resources/GUI/`, **giữ nguyên tên file**. Unity tự nhận lại, không cần sửa code.

| Thư mục | Ảnh |
|---|---|
| `ui_buttons/` | `btn_start`, `btn_resume`, `btn_retry`, `icon_pause`; nút nền trống có chữ do game ghi lên: `btn_blank_green/blue/orange/red` |
| `ui_panels/panel.png` | Nền mọi khung (thanh trên, bảng, cửa sổ). Ảnh trắng xám, game tự tô màu |
| `sfx_text/` | Chữ manga: `sfx_bonk`, `sfx_zap`, `sfx_sweep`, `sfx_whoosh`, `sfx_ko`, `sfx_win`… |
| `fx/` | `speed_lines.png` (tia khi trúng đòn), `anger_vein.png` (dấu 💢 khi tức) |
| `fonts/` | Font Lilita One |

Ảnh thẻ bài nằm ở `Assets/AIModels/CardSprites/CardArt_King/Knight/Mage/Scout.png`.

**Lưu ý với ảnh nút nền trống và `panel.png`:** đây là ảnh 9-slice, phần góc giữ nguyên còn phần giữa co giãn. Nếu vẽ lại thì giữ góc bo nằm trong khoảng 90 px (nút) hoặc 70 px (panel) tính từ mép ảnh. Hai con số này được đặt trong `Assets/Scripts/Editor/GuiSpriteImporter.cs`.

## 3. Đổi màu và font — `UiFactory.cs`

```csharp
PanelColor  = new(0.16f, 0.22f, 0.45f, 0.96f);  // dòng 15: màu nền khung (xanh navy)
TextOutline = new(0.07f, 0.06f, 0.16f, 1f);     // dòng 16: viền chữ
Accent      = new(1f, 0.83f, 0.22f, 1f);         // dòng 19: màu nhấn (tiêu đề vàng)
TextColor   = new(0.94f, 0.94f, 0.97f, 1f);      // dòng 20: màu chữ thường
```

Màu viết theo dạng (đỏ, xanh lá, xanh dương, độ đậm), mỗi số từ 0 đến 1.

Đổi font: chép file `.ttf` vào `Assets/Resources/GUI/fonts/`, rồi sửa tên ở dòng 24 (`"GUI/fonts/LilitaOne-Regular"`, không ghi đuôi `.ttf`).

## 4. Đổi vị trí và kích thước — hàm `Place(...)`

Mọi phần tử được đặt bằng một dòng dạng:

```csharp
.Place(new Vector2(neoX, neoY), new Vector2(lệchX, lệchY), new Vector2(rộng, cao));
```

- **neo:** điểm bám trên màn hình, từ 0 đến 1. `(0,0)` = góc dưới trái, `(1,1)` = góc trên phải, `(0.5,0.5)` = giữa màn hình.
- **lệch:** dịch khỏi điểm neo, tính bằng pixel trên màn hình chuẩn 1920×1080 (dương = sang phải / lên trên).
- **rộng, cao:** kích thước, cũng tính bằng pixel chuẩn 1920×1080. Màn hình khác cỡ thì game tự co giãn theo.

Ví dụ: nút CONFIRM ở `GameHud.cs` dòng 130:

```csharp
.Place(new Vector2(1f, 0f), new Vector2(-40f, 40f), new Vector2(300f, 90f));
//      bám góc dưới phải   cách mép 40 px          rộng 300, cao 90
```

Các dòng hay chỉnh:

| Phần tử | Vị trí |
|---|---|
| Thanh trên (tên phase + gợi ý) | `GameHud.cs` dòng 111 |
| Bảng "P1 KINGS / P2 KINGS" | `GameHud.cs` dòng 122 |
| Nút CONFIRM | `GameHud.cs` dòng 130 |
| Nút pause | `GameHud.cs` dòng 133 |
| Bảng BATTLE REPORT | `GameHud.cs` dòng 139 |
| Banner đổi phase | `GameHud.cs` dòng 150 |
| Màn thắng: ảnh WIN, chữ, REPLAY, MENU | `GameHud.cs` dòng 185, 188, 190, 192 |
| Thanh máu King | `GameHud.cs` dòng 270 |
| Menu chính: tiêu đề, phụ đề, cột nút | `MainMenu.cs` dòng 100, 104, 106 |
| Tay bài | `CardHand.cs` dòng 48. Cỡ thẻ, khoảng cách, độ cao: chỉnh thẳng trong Inspector (scene `GridTest` → object có component `CardHand`) |

Nút trong cột dọc (menu chính, pause, settings) không dùng `Place`. Chiều cao nút nằm ở `.Height(110f)` cuối dòng, còn khoảng cách giữa các nút ở `UiFactory.Vertical(list, 26f)`.

## 5. Đổi chữ và kiểu nút

- **Chữ trên nút:** tham số thứ 3 của `UiFactory.Button(...)`, ví dụ `"PLAY"`, `"CONFIRM"`. Cỡ chữ là số ngay sau đó.
- **Tên phase và dòng gợi ý:** `Assets/Scripts/Mechanics/Phases/Phases.cs`, các mục `PhaseName`, `Hint()`, `SubmitLabel`.
- **Mô tả thẻ (chuột phải):** chỉnh trong Inspector của `CardHand` (các ô King/Knight/Mage/Scout Text).
- **Kiểu nút:** đuôi `.Skin(...)`:
  - `.Skin("btn_blank_green", true)`: nút nền trống, giữ chữ do game ghi. Màu có sẵn: green / blue / orange / red.
  - `.Skin("btn_start")`: nút ảnh đã có sẵn chữ, game ẩn chữ đi.
  - Bỏ hẳn `.Skin(...)`: nút vuông đơn giản theo màu ở mục 3.
- **Cỡ chữ manga bay lên (BONK/ZAP…):** số `1.4f` ở `Assets/Scripts/Visual/CartoonFx.cs` dòng 332 (độ rộng tính bằng mét trong thế giới).

## 6. Thử trực tiếp rồi chép số vào code

1. Bấm Play.
2. Trong cửa sổ Hierarchy, mở `HudCanvas` (trong trận) hoặc `MenuCanvas` (menu chính) và chọn phần tử cần chỉnh, ví dụ `Submit`, `TopBar`, `Scoreboard`.
3. Kéo hoặc sửa số trong Rect Transform ở Inspector cho tới khi ưng. Lúc này **Pos X / Pos Y** chính là "lệch", còn **Width / Height** là "rộng, cao".
4. **Ghi lại các số.** Khi thoát Play mode, Unity xoá mọi thay đổi này.
5. Chép các số vào dòng `Place(...)` tương ứng ở mục 4.

## 7. Âm thanh

Xem `Assets/Audio/Sfx/README.txt`. Tóm tắt: kéo file vào ô tương ứng trong `Assets/Resources/Audio/SfxLibrary.asset`.
