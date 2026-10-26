# King Hunt: giới thiệu dự án (cho người / AI cùng phát triển)

File này giúp người mới (hoặc Claude của người mới) nắm dự án trong 5 phút. Luật chi tiết **không** chép ở đây, xem các file bên dưới.

| File | Nội dung | Vai trò |
|---|---|---|
| `Docs/LUAT_GAME.md` | Luật đầy đủ: quân, phase, battle, 9 lá phép, đếm thời hạn | **Nguồn chuẩn.** Code chạy đúng theo file này. Đổi luật thì sửa file này trước rồi mới sửa code |
| `Docs/HUONG_DAN_GUI.md` | GUI nằm ở scene / prefab nào, đổi ảnh / màu / chữ / âm thanh | Chỉnh giao diện không cần code |
| `Docs/GIOI_THIEU_DU_AN.md` | File này: kiến trúc code, quy ước, việc còn dở | |

---

## 1. Game là gì

Battleship + card game, 3D cartoon, **2 người hot-seat** (chung 1 máy, có màn che khi đổi lượt).

- Mỗi người giấu **2 King** (10 máu) trên bàn 10x10 của mình.
- Đặt **Knight** (tầm 3) / **Mage** (tầm 4) lên bàn đối thủ để dò và đánh King đang ẩn. Tầm đánh là 1 đường thẳng theo hướng mặt, gồm cả ô đang đứng.
- Bấm CONFIRM thì battle chạy: King trúng đòn mất máu và **lộ** trong lượt đó. Sang lượt mới, King ẩn lại và chủ quân được dời King.
- Rút bài mỗi lượt: lá quân (Knight / Mage) hoặc **lá phép** (DMG Boost, Reflect, Shield, Chest, Rift, Tornado, Healing, Suicide Bomb, Clone).
- Hạ hết King thật của đối thủ là thắng. Cú kết liễu có cảnh quay riêng.

## 2. Chạy thử

- Repo: `https://github.com/Sayato0803/Test3Dgame`, nhánh đang làm: **`Board-copy2`**.
- **Unity 6000.0.75f1**, URP 17, Input System (Active Input Handling = Both), DOTween (có sẵn trong `Assets/DOTween`).
- Bấm Play ở scene nào cũng vào `MainMenu` trước (do `Assets/Scripts/Editor/PlayFromMainMenu.cs`). Scene trận đấu là `Assets/Scenes/GridTest.unity`.
- Test: `Window > General > Test Runner > EditMode` (`Assets/Tests/Editor/`).
- Package MCP (`com.coplaydev.unity-mcp`, `com.ivanmurzak.unity.mcp*`) là để AI điều khiển Unity Editor. Không cần chúng để chơi.

## 3. Kiến trúc code

Code game nằm trong namespace `GridBattle`, ở `Assets/Scripts/`.

| Thư mục / file | Vai trò |
|---|---|
| `GridSystem/GridSystem.cs` | 1 bàn 10x10: `Cell[,]`, đổi world ↔ ô, `CanPlace` / `Place` / `Remove`, event `OnUnitPlaced` / `OnUnitRemoved` |
| `GridSystem/Cell.cs` | 1 ô có 2 chỗ: 1 King + 1 attacker. `UnitKind` |
| `GridSystem/GridUnit.cs` | Component trên mỗi quân: loại, chủ, ô đang đứng, tầm, `PlacedAt` (event counter để xếp thứ tự đòn), status |
| `GridSystem/AttackRange.cs` | Hàm tĩnh, logic thuần: ô trong tầm theo hướng mặt, đi qua cổng Rift (`Walk`). Có test |
| `GridSystem/GridPlacer.cs` | Input chuột: raycast vào bàn, kéo thả / xoay quân, preview tầm, chọn mục tiêu cho lá (`BeginCard`). Hỏi `Phases` xem thao tác có hợp lệ không |
| `Mechanics/Phases/Phases.cs` | **Bộ não.** State machine `GamePhase` (Deploy → Setup → Attack → End → GameOver), đổi lượt, màn che, `ResolveBattle` (tính damage rồi mới chạy hoạt ảnh), ẩn / lộ King. Nằm trên GameObject `GameFlow` trong `GridTest` |
| `Mechanics/Phases/PhasesCards.cs` | `partial class Phases`: rút bài (`Draw`), kiểm tra mục tiêu, áp hiệu ứng từng lá (`switch` theo `CardType`), status (Shield / Reflect / Bomb / Boost / Clone), Rift, bẫy Tornado, đếm thời hạn (`TickStatuses`) |
| `Mechanics/Phases/Player.cs`, `KingStats.cs` | Dữ liệu người chơi (bàn, camera, tay bài, quân) và máu / lộ của King |
| `Mechanics/Cards/CardData.cs` | ScriptableObject cho 1 loại lá (tên, mô tả, art). `Target` và `PickCount` suy ra từ `Type`. Asset nằm ở `Assets/Data/Cards/` |
| `Mechanics/CameraRig.cs` | 1 camera bay giữa các góc nhìn (bàn nhà, bàn đối thủ, cảnh kết liễu) |
| `Mechanics/UnitReactions.cs` | Hoạt ảnh của quân: trúng đòn, choáng, đè bẹp, văng xuống biển, chết, ẩn / hiện |
| `Visual/` | `CartoonFx` (chữ manga, khói, hạt), `CardFx` + `StatusFx` (hiệu ứng lá phép), `CharacterRig`, `ToonStyle`, `OutlineNormals`, môi trường động |
| `UI/` | `GameHud` (thanh trên, CONFIRM, Battle Report giữ Tab, pause, màn thắng), `CardHand` (tay bài kiểu Master Duel), `MainMenu`, `SettingsPanel`, `TutorialPanel`, `SceneFader` |
| `Audio/` | `Sfx` (gọi `Sfx.Play(SfxId.X)`) + `SfxLibrary.asset` gán file âm thanh |
| `Editor/` | Tool dựng môi trường cartoon, import sprite GUI (9-slice), PlayFromMainMenu |
| `Assets/CardScripts/` | Namespace `CardScripts`, từ nhánh `Card` cũ. `CardHand` vẫn dùng `CardView`. Còn lại (`CardManager`, `FlyingCube`, `CardEntityFlight`, scene `CardScene`) là bản thử cũ |

**Luồng 1 lượt:** chuột → `GridPlacer` → hỏi `Phases.CanPickUp / CanDropOn` → `GridSystem.Place` → event → `Phases` ghi nhận quân. Dùng lá: `CardHand` → `GridPlacer.BeginCard` → `PhasesCards` áp hiệu ứng. Bấm CONFIRM: `GameHud.Submit` → `Phases.ResolveBattle` → `UnitReactions` / `CartoonFx` / `Sfx` chạy hoạt ảnh → End → đổi người.

**Code cũ còn sót, không dùng trong trận:** `Assets/MageScript.cs`, `Mechanics/Shoot.cs`, `FireballBehavior.cs`, `BulletDeflect.cs`, `ChangeCamera.cs`, `SampleScene`.

## 4. Thêm 1 lá phép mới

1. Thêm giá trị vào `enum CardType` (`Mechanics/Cards/CardData.cs`). Nếu lá cần kiểu mục tiêu khác thì sửa `Target`, nếu cần thì thêm `CardTarget` mới.
2. Thêm `case` hiệu ứng trong `PhasesCards.cs`. Nếu lá có thời hạn thì đếm thêm trong `TickStatuses`. Nếu lá tác động lúc battle thì sửa `ResolveBattle` trong `Phases.cs`.
3. Chuột phải `Assets/Data/Cards` → `Create > GridBattle > Card`, điền tên / mô tả / art (để trống art thì lá hiện nền màu + tên).
4. Kéo asset vào mảng `Deck` của component `Phases` trên `GameFlow` (scene `GridTest`).
5. Ghi luật vào `Docs/LUAT_GAME.md` mục 7. Logic tính toán thuần (đường đánh, vùng nổ…) thì thêm test vào `Assets/Tests/Editor/CardRuleTests.cs`.

## 5. Quy ước

- Comment và chữ trong Inspector viết **tiếng Việt**. Chữ hiện trong game viết **tiếng Anh**, vì font Arco không có dấu tiếng Việt.
- Con số luật / nhịp hoạt ảnh để `[SerializeField]` cho chỉnh trong Inspector, không hard-code.
- GUI nằm trong scene / prefab. Script chỉ bật tắt panel và ghi chữ. Đừng dựng UI bằng code.
- Luật thuần (không cần scene) viết thành hàm tĩnh để test được bằng EditMode test.
- **Hai người làm chung:** mỗi người 1 nhánh. Tránh 2 người cùng sửa `GridTest.unity` hoặc cùng 1 prefab một lúc, vì file scene merge rất dễ hỏng. Nếu bắt buộc phải sửa cùng chỗ thì báo nhau trước.

## 6. Việc còn dở / ý tưởng tiếp theo

- Lá **Brawler**: có trong thiết kế, chưa làm.
- Bộ bài: chưa có cost, chưa giới hạn số lá mỗi loại, chưa giới hạn tay bài.
- Bảng phân loại thẻ (`LUAT_GAME.md` mục 8) chưa được code dùng (dự định: màu viền lá, giới hạn theo nhóm).
- Các điểm luật chờ xác nhận: `LUAT_GAME.md` mục 9.
- Art: 9 lá phép đã có art riêng.
- Chưa có: chơi với máy (AI), chơi online, lưu trận.
