# Hướng dẫn tự sửa GUI

GUI nằm sẵn trong scene và prefab: **chỉnh trực tiếp trong Unity Editor, không cần sửa code.** Script chỉ bật/tắt panel và ghi chữ lúc chạy.

## 1. Cái gì nằm ở đâu

| Chỗ | Nội dung |
|---|---|
| Scene `GridTest` → `GameUI/HudCanvas` | Trong trận: thanh trên, bảng KINGS, nút CONFIRM, pause, banner, màn đổi lượt, màn thắng, menu tạm dừng |
| Scene `GridTest` → `GameUI/CardHandCanvas` | Vùng tay bài (`Hand`) và bảng xem thẻ (`CardPreview`, chuột phải) |
| Scene `MainMenu` → `MenuCanvas` | Tiêu đề, phụ đề, nút PLAY / SETTINGS / QUIT |
| `Assets/Prefab/UI/SettingsPanel.prefab` | Bảng Settings, dùng chung cho menu chính và menu pause |
| `Assets/Prefab/UI/Card.prefab` | 1 lá bài trên tay (ảnh nằm ở con `Body`, vệt sáng ở `ShineSweep`) |
| `Assets/Prefab/UI/KingHpBar.prefab` | Thanh máu trên đầu King |
| `Assets/Prefab/UI/DamageText.prefab` | Số damage bay lên (cỡ chữ, màu, viền) |
| `Assets/Resources/UI/SceneFader.prefab` | Màn đen khi chuyển scene |

Các panel ẩn sẵn (pause, settings, màn thắng…) bị tắt trong Hierarchy. Bật tạm để xem, **nhớ tắt lại** trước khi lưu scene.

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

## 3. Đổi màu, font, vị trí, kích thước, chữ

Chọn phần tử trong Hierarchy rồi chỉnh trong Inspector:

- **Vị trí / kích thước:** Rect Transform (Pos X/Y, Width/Height, Anchor). Màn hình chuẩn 1920×1080, màn khác cỡ tự co giãn (Canvas Scaler).
- **Màu nền khung:** component `Image` → Color. **Màu / font / cỡ chữ:** component `Text`. **Viền chữ:** `Outline`, `Shadow`.
- **Nút trong cột dọc** (menu chính, pause, settings): chiều cao ở `Layout Element`, khoảng cách ở `Vertical Layout Group` của object cha.
- **Kiểu nút:** đổi ảnh ở `Image` → Source Image (ảnh trong `Assets/Resources/GUI/ui_buttons/`).
- **Nút bấm gọi hàm gì:** `Button` → On Click (). Ví dụ CONFIRM gọi `GameHud.Submit`.

Thông số không thuộc 1 phần tử cụ thể (thời gian fade, banner, hitstop, slow-mo, màu thanh máu…) nằm ở Inspector của component `GameHud`, `CardHand`, `MainMenu`, `SettingsPanel`, `SceneFader`, `ButtonPop`.

## 4. Chữ do game tự ghi

Chữ đang hiển thị trong scene ở các ô sau chỉ là chữ mẫu, game ghi đè lúc chạy:

- **Tên phase và dòng gợi ý:** `Assets/Scripts/Mechanics/Phases/Phases.cs`, các mục `PhaseName`, `Hint()`, `SubmitLabel`.
- **Mô tả thẻ (chuột phải):** Inspector của `CardHand` (các ô King/Knight/Mage/Scout Text).
- **Cỡ chữ manga bay lên (BONK/ZAP…):** số `1.4f` ở `Assets/Scripts/Visual/CartoonFx.cs`, hàm `Comic` (độ rộng tính bằng mét trong thế giới).

## 5. Thử trực tiếp

Chỉnh trong Play mode để xem ngay, nhưng **Unity bỏ mọi thay đổi khi thoát Play mode**. Ưng rồi thì thoát Play mode và chỉnh lại y như vậy (hoặc chuột phải component → Copy Component, thoát Play, → Paste Component Values).

## 6. Âm thanh

Xem `Assets/Audio/Sfx/README.txt`. Tóm tắt: kéo file vào ô tương ứng trong `Assets/Resources/Audio/SfxLibrary.asset`.
