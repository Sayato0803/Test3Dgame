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
