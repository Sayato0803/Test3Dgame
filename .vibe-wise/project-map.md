# Project Map

## Purpose
Game đối kháng thể loại battleship kết hợp card game (Unity). Hai bên giấu King trên lưới 10x10, lần lượt đặt Mage/Knight để tìm và hạ King đối phương; sau battle được cầm King lên đổi vị trí.

## Requirements
- Luật setup (learner nêu trước): mỗi người chơi đặt 2 King, sau đó đặt 1 Mage + 1 Knight.
- Luật lượt (learner nêu 2026-10-21): Play -> Setup phase cả 2 đặt King, nút xác nhận -> biến quyết định ai đi trước (tạm) -> lượt P1 đặt 2 Knight + 2 Mage lên sân P2, nút xác nhận -> Battle phase: King P2 trong tầm đánh -1 máu, chồng tầm thì ăn thêm; King có biến máu -> King P2 chết thì thông báo P1 thắng, không thì sang lượt P2 tương tự.
- Lưới 10x10 mỗi bên, đặt nhân vật lên ô kiểu TFT/battleship.
- Camera tổng quan khi đặt King; camera bám nhân vật đang chọn khi đặt lên bàn đối phương.
- Battle phase: Mage/Knight tấn công theo phạm vi riêng.
- Kết thúc lượt: cầm King lên đổi vị trí.
- Animation chạy khi có sự kiện (tấn công, trúng đòn...).

- Chốt (learner 2026-10-21): mỗi người 2 King, King 10 máu, thua khi chết cả 2. Damage: mỗi attacker có tầm phủ King gây 1 (chồng tầm cộng dồn) + giữ bonus khi đứng đúng ô King (giá trị chưa chốt). Attacker ở lại sau battle; lượt sau được đổi vị trí attacker rồi submit đánh tiếp. Đầu lượt mỗi player được đổi vị trí King. King bị đánh trúng thì lộ vị trí cho bên tấn công; chủ quân đổi chỗ King thì ẩn lại. Bonus đứng đúng ô King = +1, cộng chung với damage tầm. Từ lượt 2 chỉ đổi vị trí 4 attacker có sẵn (thêm quân mới: làm sau).
- Chosen (learner 2026-10-21): hot-seat 1 máy, lần lượt + màn che khi đổi người (bỏ ý split screen / đặt King đồng thời).

## Components
Verified in repo (Assets/):
- MageScript.cs, Scripts/Shoot.cs: bắn fireball bằng phím Space (2 file nội dung giống nhau).
- Scripts/FireballBehavior.cs (FireboltBehavior): đạn có lifetime, đổi hướng.
- Scripts/BulletDeflect.cs (BulletDeflector): trigger làm đạn rẽ hướng.
- Scripts/ChangeCamera.cs (CameraManager): bật/tắt giữa 2 camera.
- Models: King.fbx, Knight.fbx, Mage.fbx (đã rig, chưa có Animator Controller), Table.fbx; Prefab/fireball.prefab; Entity/*.prefab.
- Scenes/SampleScene.unity.
Grid board: Cell/GridUnit/GridSystem/GridPlacer đã nằm trong Assets/Scripts/GridSystem/ (verified 2026-10-19).
Learner tự thêm (verified trong code): khi đang kéo, cuộn chuột xoay quân 90° quanh trục Y; quân spawn ở camera 3/4 xoay 180°; Phases.cs rỗng ở Scripts/Mechanics/Phases/.

## Components (grid, implemented — trong Assets/Scripts/GridSystem/)
- Cell.cs: dữ liệu 1 ô (King slot, Attacker slot).
- GridUnit.cs (AI addition): component trên quân, giữ Kind + ô đang đứng.
- GridSystem.cs: Cell[,], Corner/cellSize, TryWorldToCell / CellToWorld, CanPlace/Place/Remove, quad mỗi ô, BoxCollider layer Grid, event OnUnitPlaced/OnUnitRemoved.
- GridPlacer.cs: raycast LayerMask, kéo thả, nút OnGUI tạm, log event; preview tầm đánh khi kéo Mage/Knight: code đã giao trong chat, learner tự áp dụng (chưa xác minh trong repo).

## Phase system (bước 1 — đã ghi vào nhánh Board-copy, chưa commit, chưa chạy Unity)
- Player.cs (B1): bàn, đối thủ, cameraIndex (bàn nhà), attackCameraIndex (nhìn bàn đối thủ, dùng ở Attack/End), Kings/Attackers.
- Phases.cs (A1+C1): enum GamePhase {Deploy, Setup, Attack, End, GameOver}; CurrentPlayer; CanSpawn/CanPickUp/CanDropOn/CanSubmit; Submit chuyển phase; màn che OnGUI; đăng ký Owner qua OnUnitPlaced.
- GridPlacer hỏi Phases (null = test tự do); GridUnit.Owner, GridUnit.Range.
- Bước 2 (AI chọn, Board-copy, chưa test): KingStats.cs, GridSystem/AttackRange.cs (static GetCells/FacingStep), ResolveBattle damage/lộ/GameOver, RefreshKingVisibility.

## Main Flow
Hiện tại: Input (Space) -> Shoot/MageScript -> spawn fireball -> FireboltBehavior / BulletDeflector.
Chuột -(GridPlacer: Raycast, LayerMask Grid)-> hit.point -(TryWorldToCell)-> (x,y) -(CanPlace)-> Place: ghi Cell[x,y] + snap CellToWorld -> event OnUnitPlaced -> listeners (hiện: Debug.Log) [implemented, chưa test trong Unity]

## Data and Trust Boundaries
Single-player local, không có storage/network được xác minh.

## Build and Deployment
Unity 6000.0.75f1, URP 17.0.4, Input System 1.19 (activeInputHandler = Both).

## Unknowns
- Deferred: xử lý camera bám quân khi kéo thả ở pha đặt lên bàn đối phương.
- Chưa quyết: quân nào được đặt lên bàn nào (King chỉ bàn nhà? Mage/Knight chỉ bàn đối phương?); có đổi chỗ khi thả vào ô có quân không.
- Chosen (chưa implement): dữ liệu ô là mảng 2D Cell[,] (thay cho ô-là-GameObject); hiển thị trên 1 model bàn; chọn ô bằng Physics.Raycast vào collider bàn.
- Chosen: lưới 10x10 cạnh s định nghĩa bằng code là source of truth; model bàn làm sau theo kích thước lưới.
- Chosen (learner): tầm đánh tính từ ô đang đứng theo hướng facing: Mage 3 ô, Knight 2 ô.
- Chosen: ô vuông; kéo thả kiểu TFT là cơ chế đặt chính; kiểm tra phạm vi + damage ở Battle phase.
- Chosen (game design): quân đặt trúng ô King đối phương được bonus damage; GridSystem phát/nhận tín hiệu (event).
- Unknown: cách ô biết toạ độ và tìm ô lân cận; định nghĩa "trống"; hệ thống xử lý riêng là gì.
