# Learning Progress

## Grid board
Introduced (AI explained):
- World space / Transform; đổi ô <-> world bằng cellSize + origin.
- Camera.ScreenPointToRay; Physics.Raycast (cần Collider) vs Plane.Raycast (toán học).
- Raycast trúng collider đầu tiên; Layer/LayerMask để chỉ trúng ô.
- C# event / Observer pattern (event Action<T>, +=, Invoke).
- Instantiate: sinh ô bằng code trong vòng lặp.
- Hành vi đặt tướng TFT (kéo thả, snap, đổi chỗ, hex) và mapping sang các phần grid.
- Công thức hit.point -> (x,y) qua góc bàn + Floor, và (x,y) -> tâm ô (+0.5). Learner chưa tự suy ra (nói không biết).
- 3 cách hiển thị ô: overlay quad (a1 100 quad cố định / a2 pooling), Texture2D 10x10 Point filter, Shader/Shader Graph — giải thích chi tiết theo yêu cầu.
Learner reasoning:
- Chọn mỗi ô là GameObject có script Cell, nhân bản từ 1 ô ("chỉ cần code cho 1 ô").
- Chọn Physics.Raycast vì thực dụng, dễ dùng.
- Đặt nhân vật: kiểm tra ô trống -> gửi thông tin cho hệ thống xử lý riêng -> model đặt ở giữa ô; model tạm lấy từ các nút theo tên.
- Tự liên hệ vùng đánh Knight với 8 ô lân cận kiểu Game of Life.
- Game design: quân đặt trúng ô King đối phương được bonus damage.
- "Hệ thống xử lý riêng" = hàm trong GridSystem nhận/gửi tín hiệu (player đặt quân, King trong vùng đánh).
- Đổi hướng: dữ liệu ô là ma trận 2D, hiển thị trên 1 model bàn 3D (lý do nêu: tối ưu). AI đính chính: lợi ích chính là tra cứu grid[x,y], không phải hiệu năng.
- Đề xuất: lưới 10x10 cạnh s định nghĩa bằng code là gốc, model bàn làm sau theo kích thước lưới. AI giải thích: vẫn cần đổi điểm chạm -> ô vì input chuột ở không gian màn hình/world; lưới code-defined chính là corner + s của công thức.
- Chọn: ô vuông, kéo thả là cơ chế đặt chính; damage tính ở Battle phase.
- Chọn hiển thị a1 (100 quad/bàn) vì đơn giản, chấp nhận 200 object trong Hierarchy.
- Implementation approved & done: Cell.cs, GridUnit.cs, GridSystem.cs, GridPlacer.cs (giao dạng file, không chép vào project). Chưa chạy trong Unity; chỉ type-check bằng stub.
Needs reinforcement: công thức world <-> cell (mới được giải thích, chưa áp dụng); chưa nối lựa chọn ô-là-GameObject với việc tra cứu theo (x,y); thời điểm kiểm tra phạm vi (lúc đặt vs Battle phase) chưa rõ.

## Preview tầm đánh khi kéo
Introduced (AI explained):
- transform.forward là vector hướng trong world; RoundToInt(forward.x/z) -> bước ô (trục + dấu), khử sai số float.
- Alpha của màu chỉ có tác dụng khi material URP để Surface Type = Transparent.
Learner reasoning:
- Lấy ô hover + facing để chọn trục x/z và tính các ô phía trước.
- Tầm tính từ ô đang đứng (Mage 3, Knight 2 — Knight đổi luật từ 1 ô trước mặt); ô ngoài bàn bỏ qua, xét tiếp; tầm lưu trong GridPlacer.
- Ban đầu đề xuất cờ _isPreview; sau khi được chỉ ra cờ không cho biết ô nào cần xoá, chọn nhớ danh sách ô đã tô.
- Ô hover lấy màu preview thay xanh lá.
- AI đã sửa GridPlacer.cs rồi learner yêu cầu undo; code được gửi trong chat để learner tự gõ vào project (2026-10-19). Trạng thái trong project: chưa có (learner tự áp dụng). AI additions đã duyệt: giữ đỏ khi không hợp lệ, King giữ hover cũ, material Transparent do learner tự chỉnh.
- (2026-10-20) Learner tự gõ code preview vào project (tầm đổi thành Mage 4, Knight 3). Unity treo do package AI Assistant (Unity.AI.Toolkit), không do script.
- (2026-10-20) Learner hỏi cơ chế chuột: AI giải thích Input.mousePosition -> ScreenPointToRay -> Raycast collider bàn -> TryWorldToCell; MouseOverUi (lật trục y GUI); state machine _dragging/_draggingNew; spawn từ nút OnGUI; snap/Lerp; Place vs CancelDrag.
Needs reinforcement: đổi vector hướng -> bước ô (learner nói không biết, mới được giải thích).

## Phase / lượt
Introduced (AI explained): state machine, enum làm trạng thái; options A/B/C (learner xin gợi ý).
Learner reasoning: tự liệt kê 3 phase Setup/Attack/End; tự mô tả luồng lượt; đặt tính damage ở End; chọn hot-seat sau khi được chỉ ra 1 màn hình không giấu được; chọn A1/B1/C1 (không nêu lý do).
Bước 1 (khung phase + Player + chặn GridPlacer + Submit/màn che) approved 2026-10-21; code gửi trong chat; sau đó learner yêu cầu AI tự ghi vào nhánh Board-copy (đã ghi, chưa commit). AI additions đã duyệt: đăng ký quân qua OnUnitPlaced, màn che OnGUI, GameOver trong enum, Phases chuyển camera. AI thêm khi viết (báo trong report): nhấc quân ưu tiên quân được phép (King dưới attacker đối thủ), Submit khoá khi đang kéo, nút Submit chặn raycast. Type-check bằng Unity stub (csc, C# 9): pass. Chưa chạy trong Unity.
- (2026-10-21) Test trong Unity: GridPlacer chưa gán Phases, p2 Display Name sai (AI chỉ ra qua screenshot). Bug thiết kế của AI: Attack dùng camera bàn nhà -> thêm Player.AttackCameraIndex, Attack/End dùng camera đó (approved, ghi vào Board-copy, type-check pass).
Needs reinforcement: lý do chọn A1/B1/C1 chưa được learner diễn đạt.

- (2026-10-21) Bước 2: learner yêu cầu sửa thẳng (bỏ qua checkpoint). AI tự chọn và ghi vào Board-copy (chưa commit, type-check pass): KingStats component (Health public 10, IsRevealed, SetVisible tắt Renderer con) do Phases AddComponent; AttackRange static (hướng c) dùng chung preview + battle; GridUnit.Range gán lúc spawn; ResolveBattle tính damage + lộ + GameOver; King chết nằm lại, hiện, không nhận thêm damage; RefreshKingVisibility (chủ quân / đã lộ / đã chết / GameOver). Các lựa chọn này là của AI, learner chưa diễn đạt lý do.

Offered trước đó (chưa duyệt): nâng primitive lên nửa chiều cao; GridPlacer bỏ qua phần tử null trong _grids.

## Merge Card + Game, menu, camera, phản ứng quân (2026-10-23)
Learning mode paused: learner yêu cầu AI sửa trực tiếp, bỏ qua checkpoint. Learner duyệt qua câu hỏi: click thẻ -> quân bay theo chuột; 1 camera bay giữa 5 góc; tắt auto-run CardSceneBuilder; uGUI HUD, hiệu ứng trúng đòn, pause menu, bỏ thẻ Mirror/Treasure.
AI implemented (đã chạy thử trong Unity qua MCP, flow Deploy->Attack->End->GameOver không lỗi): CardHand, CameraRig, UnitReactions, GameHud, MainMenu, SettingsPanel, GameSettings, SceneFader, UiFactory; sửa GridPlacer/Phases/CardView/CardSceneBuilder; scene MainMenu mới; GridTest thêm Main Camera + GameUI. Learner chưa giải thích lại các phần này.

## Đè bẹp / văng King / cartoon / âm thanh (2026-10-23)
Learning mode vẫn paused (sửa trực tiếp). Learner chốt luật: attacker thả trúng King ẩn -> King -1 máu + lộ; attacker nảy sang ô ngẫu nhiên trong 3x3 (AI thêm: bỏ ô có attacker/King ẩn khác; chỉ King ẩn mới bị đè). King chết bị đánh văng khỏi bàn. Chọn: toon shader + viền, môi trường dựng trong Unity, hạt cartoon, âm thanh tổng hợp bằng code; asset CC0 qua Blender chưa làm (Blender chưa chạy).
AI implemented + chạy thử trong Unity (đè bẹp, nảy, văng, GameOver): Sfx, Toon/SkyGradient shader, ToonStyle, CartoonAssets, CartoonFx, EnvironmentMotion, CartoonEnvironmentBuilder; Phases.TrySquash.

## Outline, thẻ mới, bàn xám, văng ngang, đè ở battle (2026-10-23)
Learning mode paused. Learner chốt: model mới = Blender nâng cấp model hiện có (chưa làm: Blender chưa kết nối MCP); thẻ = cắt 3 thẻ từ newModel.jpg; đè bẹp chỉ trong battle, đè mọi King còn sống dưới chân (thay bonus +1), nảy 3x3.
AI implemented + test trong Unity: OutlineNormals (pháp tuyến mượt trong tangent), CardArt_*.png + import không nén/mipmap, mô tả trên thẻ, bàn xám, KnockOut văng ngang -> rơi xuống biển (Splash), ResolveBattle tính damage trước rồi mới animate/dời quân.
- (2026-10-23) Model mới qua Blender 5.1 (MCP): King + quyền trượng, râu mượt; Knight + mũ tròn, tấm che khe, chùm lông, khiên gỗ, kiếm màu phẳng; Mage + râu dài, ria, lông mày, mũ sao + chóp cong, gậy màu phẳng (ngọc xanh ngọc). Bản gốc ở ModelBackup/*.original.fbx, file .blend v2 ở ModelBackup/.
- (2026-10-23) Learner chốt: King/Mage nâng cấp sâu như Knight; luật lộ: lộ tới khi chủ quân dời sang ô KHÁC (đặt lại ô cũ vẫn lộ); hiệu ứng ẩn: bụp khói + chui xuống. AI làm + test: vương miện răng cưa mới, khuy tròn, râu King; gậy gỗ cong ôm ngọc, râu 3 lọn cho Mage; KingStats.Reveal/RevealedAt; UnitReactions.SetBurrowed (PlayBurrow/PlayEmerge, chỉ FX khi người xem thấy); Sfx.Poof; CartoonFx.SmokePoof.
- (2026-10-23) Learner chốt: ẩn King = xoay 1 vòng rồi biến mất để lại vệt khói (thay chui xuống); chết: đè bẹp -> bắn lên trời, Mage -> tan rã phép, Knight -> kiếm chém văng ngang; ưu tiên Đè > Mage > Knight. AI làm + test: DeathKind, LaunchToSky, Disintegrate, SmokeTrail, làn khói ẩn (chỉ chủ quân thấy), MagicBolt (Mage bắn tia), Slash (Knight), Sfx Magic/Slash/Zap.
- (2026-10-23) Learner báo lỗi "vua vẫn hiện dù không di chuyển". Sửa luật: King chỉ lộ trong lượt bị đánh; hết lượt (End -> Setup) mọi King còn sống ẩn lại dù đứng yên (HideAllKings); nhấc King lên cũng ẩn. Test qua 2 lượt: OK.

## Hệ thống lá phép theo Update/CARD_SYSTEM_PROMPT.md (2026-10-25)
Learning mode paused (sửa trực tiếp, làm hết 4 bước một lần). Learner chốt: lá phép gộp vào CardHand uGUI; chỉ dùng trong phase Attack của mình; Turn = 1 vòng của cả 2 người.
AI implemented + test trong Unity (Play mode qua MCP, cả 9 lá): CardData (SO, Target suy ra từ Type), 9 asset ở Assets/Data/Cards gán vào Phases._deck, Player.Hand, PhasesCards.cs (partial Phases: rút/dùng bài, status, Rift/Tornado/Clone/Bomb), ResolveBattle xử lý đòn theo event counter (GridUnit.PlacedAt), AttackRange có portals, GridPlacer.BeginCard (chọn mục tiêu, camera về bàn nhà), lá tạm (nền màu + tên). Learner chưa giải thích lại các phần này.
