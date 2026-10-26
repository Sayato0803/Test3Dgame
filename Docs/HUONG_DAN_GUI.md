# Hướng dẫn tự sửa GUI

GUI nằm sẵn trong scene và prefab: **chỉnh trực tiếp trong Unity Editor, không cần sửa code.** Script chỉ bật/tắt panel và ghi chữ lúc chạy.

## 1. Cái gì nằm ở đâu

| Chỗ | Nội dung |
|---|---|
| Scene `GridTest` → `GameUI/HudCanvas` | Trong trận: thanh trên, bảng KINGS, nút CONFIRM, pause, màn đổi lượt, màn thắng, menu tạm dừng |
| Scene `GridTest` → `GameUI/CardHandCanvas` | Vùng tay bài (`Hand`) và bảng xem thẻ (`CardPreview`, chuột phải) |
| Scene `MainMenu` → `MenuCanvas` | Tiêu đề, phụ đề, nút PLAY / SETTINGS / QUIT |
| `Assets/Prefab/UI/SettingsPanel.prefab` | Bảng Settings, dùng chung cho menu chính và menu pause |
| `Assets/Prefab/UI/Card.prefab` | 1 lá bài trên tay (ảnh nằm ở con `Body`, vệt sáng ở `ShineSweep`) |
| `Assets/Prefab/UI/KingHpBar.prefab` | Thanh máu trên đầu King |
| `Assets/Prefab/UI/DamageText.prefab` | Số damage bay lên (cỡ chữ, màu, viền) |
| `Assets/Resources/UI/SceneFader.prefab` | Màn đen khi chuyển scene |

Khung kiểu cartoon (bảng màu Resurrect 64, mẫu `Resources/GUI/ui_buttons/Hybrid.png`): `Image` của panel là **viền** (`panel.png`, tô vàng), object con `Paper` là **mặt container** (`panel_fill.png`, mỗi panel một màu: thanh trên xanh dương, bảng điểm xanh ngọc, pause tím…). Đổi màu ở Color của 2 Image này. Tiêu đề cửa sổ nằm trên object `Ribbon` (dải cạnh nhọn, đổi màu ở Color).

**Giữ Tab** hiện Battle Report: lịch sử cả trận, mỗi dòng ghi `Turn N - P1: ...`; số dòng hiện ở ô `Report Lines` của `GameHud`. **R** (hoặc lăn chuột) xoay quân đang cầm 90°. **Enter** = bấm CONFIRM / END TURN.

**Tutorial:** prefab `Assets/Prefab/UI/TutorialPanel.prefab` (nút TUTORIAL ở Main Menu và menu Pause). Mỗi dòng là object `Row` gồm `Keys` (icon phím / chuột lấy từ `Art/InputIcons/.../Outline/PNG_4x`) + `Label`; thêm dòng = Duplicate 1 `Row` rồi đổi sprite và chữ.

**Bảng phím tắt** (mép phải màn hình, luôn hiện trong trận): chữ nằm ở ô `Shortcuts` của `GameHud` (rich text, phím tô `<color=#FFD54A>`), object được dựng lúc chạy trong canvas của HUD. Thêm phím tắt mới thì thêm 1 dòng vào ô này.

**F1** trong trận ẩn / hiện toàn bộ HUD + tay bài (danh sách canvas ở ô `Hide On F1` của `GameHud`). Khi có King bị hạ, các canvas này cũng tự ẩn tới hết cảnh kết liễu.

**Nút chữ** dùng chung 1 kiểu: nền `btn_blank_*` (xanh lá = hành động chính, xanh dương = phụ, cam = quay lại, đỏ = thoát), chữ trắng viền tím than. Mỗi nút có 4 trạng thái qua Button → Transition `Sprite Swap`: `_hover` (Highlighted), `_focus` (Selected), `_pressed` (Pressed), `_disabled`. Khoảng cách chữ tới viền nằm ở Rect Transform của object con `Text` (Left/Right 28, Bottom 14, Top 8); chữ dài tự thu nhỏ nhờ `Best Fit` (Max Size = cỡ chữ gốc).

Các panel ẩn sẵn (pause, settings, màn thắng…) bị tắt trong Hierarchy. Bật tạm để xem, **nhớ tắt lại** trước khi lưu scene.

## 2. Đổi ảnh (dễ nhất)

Thay file trong `Assets/Resources/GUI/`, **giữ nguyên tên file**. Unity tự nhận lại, không cần sửa code.

| Thư mục | Ảnh |
|---|---|
| `ui_buttons/` | `btn_start`, `btn_resume`, `btn_retry`, `icon_pause`; nút nền trống có chữ do game ghi lên: `btn_blank_green/blue/orange/red` |
| `ui_panels/` | `panel.png` viền khung, `panel_fill.png` mặt container, `ribbon.png` dải tiêu đề. Ảnh trắng xám, tô màu bằng Image → Color |
| `sfx_text/` | Chữ manga: `sfx_bonk`, `sfx_zap`, `sfx_sweep`, `sfx_whoosh`, `sfx_ko`, `sfx_win`… |
| `fx/` | `speed_lines.png` (tia khi trúng đòn), `anger_vein.png` (dấu 💢 khi tức) |
| `fonts/` | Đang dùng `ARCO for OSX.otf`. **Đừng dùng `ARCO.ttf`**: bảng mã Symbol, Unity không đọc được nên rơi về Arial. Arco không có dấu tiếng Việt |

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

## 6. Nền màn hình chính (scene `MainMenu`)

| Muốn đổi | Chỉnh ở đâu |
|---|---|
| Vị trí / góc / cỡ 3 quân đứng chào | Object `Showcase` → chọn từng quân, kéo bằng Move/Rotate/Scale tool. Thêm quân: kéo prefab model vào `Showcase` rồi thêm vào ô `Showcase Units` của `MainMenu` (bớt quân: xoá khỏi cả hai) |
| Camera nhìn vào đâu, đứng xa / cao bao nhiêu | Component `MainMenu`: `Look At`, `Orbit Radius`, `Orbit Height`, `Orbit Angle` (hướng đứng quanh điểm nhìn) |
| Camera đung đưa | `Sway Angle` (0 = đứng yên), `Sway Speed` |
| Quân xuất hiện / nhảy | `Spawn Delay`, `Stagger` (lệch nhịp giữa các quân), `Hop Every`, `Hop Height` |
| Cây, đá, hoa, mây, đảo, nước | Object `Environment` (con `Nature`, `Clouds`, `Island`, `Water`): kéo thả / xoá / nhân bản tự do. Mây trôi, cây đung đưa do component trên từng object, chỉnh số trong Inspector |
| Bầu trời | `Window → Rendering → Lighting → Environment → Skybox Material` (`Assets/Art/Materials/Sky_Cartoon.mat`: đổi màu trên material) |
| Ánh sáng, màu nắng | `Directional Light` (màu, cường độ, góc), `Rim Light` (viền sáng sau lưng quân) |
| Độ rực màu, vignette, bloom | `Environment/PostFX` (Volume) → profile `Assets/Art/PostFX_Cartoon.asset` |

Lưu ý: camera do `MainMenu` điều khiển lúc chạy, nên kéo `Main Camera` trong Scene view sẽ bị ghi đè khi Play. Muốn đổi góc thì chỉnh các ô ở trên.

## 7. Âm thanh

Xem `Assets/Audio/Sfx/README.txt`. Tóm tắt: kéo file vào ô tương ứng trong `Assets/Resources/Audio/SfxLibrary.asset`.
