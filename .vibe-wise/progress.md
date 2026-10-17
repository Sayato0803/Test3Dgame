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

## Pending decision
Decision: Luật setup — Mage/Knight setup đặt bàn nào; thành phần nào đếm/giới hạn số quân (GridSystem / GridPlacer / quản lý lượt mới).
Stage: Build checkpoint — awaiting reasoning.
Offered (chưa duyệt): nâng primitive lên nửa chiều cao; GridPlacer bỏ qua phần tử null trong _grids.
