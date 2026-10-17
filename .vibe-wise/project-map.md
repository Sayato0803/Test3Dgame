# Project Map

## Purpose
Game đối kháng thể loại battleship kết hợp card game (Unity). Hai bên giấu King trên lưới 10x10, lần lượt đặt Mage/Knight để tìm và hạ King đối phương; sau battle được cầm King lên đổi vị trí.

## Requirements
- Luật setup (learner nêu): mỗi người chơi đặt 2 King, sau đó đặt 1 Mage + 1 Knight. Chưa quyết: Mage/Knight setup đặt lên bàn nào; ai đếm/giới hạn số quân.
- Lưới 10x10 mỗi bên, đặt nhân vật lên ô kiểu TFT/battleship.
- Camera tổng quan khi đặt King; camera bám nhân vật đang chọn khi đặt lên bàn đối phương.
- Battle phase: Mage/Knight tấn công theo phạm vi riêng.
- Kết thúc lượt: cầm King lên đổi vị trí.
- Animation chạy khi có sự kiện (tấn công, trúng đòn...).

## Components
Verified in repo (Assets/):
- MageScript.cs, Scripts/Shoot.cs: bắn fireball bằng phím Space (2 file nội dung giống nhau).
- Scripts/FireballBehavior.cs (FireboltBehavior): đạn có lifetime, đổi hướng.
- Scripts/BulletDeflect.cs (BulletDeflector): trigger làm đạn rẽ hướng.
- Scripts/ChangeCamera.cs (CameraManager): bật/tắt giữa 2 camera.
- Models: King.fbx, Knight.fbx, Mage.fbx (đã rig, chưa có Animator Controller), Table.fbx; Prefab/fireball.prefab; Entity/*.prefab.
- Scenes/SampleScene.unity.
Grid board: chưa có trong project (bản sample AI viết trước đó đã được gỡ theo yêu cầu).

## Components (grid, implemented as files — chưa nằm trong project)
- Cell.cs: dữ liệu 1 ô (King slot, Attacker slot).
- GridUnit.cs (AI addition): component trên quân, giữ Kind + ô đang đứng.
- GridSystem.cs: Cell[,], Corner/cellSize, TryWorldToCell / CellToWorld, CanPlace/Place/Remove, quad mỗi ô, BoxCollider layer Grid, event OnUnitPlaced/OnUnitRemoved.
- GridPlacer.cs: raycast LayerMask, kéo thả, nút OnGUI tạm, log event.

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
- Chosen: ô vuông; kéo thả kiểu TFT là cơ chế đặt chính; kiểm tra phạm vi + damage ở Battle phase.
- Chosen (game design): quân đặt trúng ô King đối phương được bonus damage; GridSystem phát/nhận tín hiệu (event).
- Unknown: cách ô biết toạ độ và tìm ô lân cận; định nghĩa "trống"; hệ thống xử lý riêng là gì.
