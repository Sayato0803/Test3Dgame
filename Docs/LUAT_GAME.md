# Luật game

Luật thiết kế đã chốt (2026-10-25) và **code hiện tại chạy đúng theo bản này**. Muốn đổi luật: sửa file rồi báo mình cập nhật code.

Ký hiệu: ⚙️ = con số chỉnh được trong Inspector (không cần sửa code). 📝 = điểm mình tự chọn khi luật chưa nói rõ, bạn nên xác nhận.

---

## 1. Tổng quan

- 2 người chơi **hot-seat** (chung 1 máy). Mỗi lần đổi người có màn che để người kia không nhìn trộm.
- Mỗi người có 1 **bàn 10x10**. King của mình giấu trên bàn mình; attacker của mình đặt lên **bàn đối thủ**.
- **Thắng** khi mọi King **thật** của đối thủ chết (King giả từ lá Clone không tính).

## 2. Quân

| Quân | Máu | Tầm đánh | Số lượng | Đặt ở |
|---|---|---|---|---|
| King | 10 ⚙️ | – | Đúng 2 ⚙️ (đặt ở Deploy) | Bàn mình |
| Knight | – | 3 ô ⚙️ | Không giới hạn, mỗi lá Knight đặt 1 quân | Bàn đối thủ |
| Mage | – | 4 ô ⚙️ | Không giới hạn, mỗi lá Mage đặt 1 quân | Bàn đối thủ |

- **Tầm đánh** tính theo hướng mặt, **gồm cả ô đang đứng**. Ô ngoài bàn bỏ qua. Xoay quân đang cầm: **R** hoặc lăn chuột (mỗi lần 90°).
- Mỗi ô có 2 chỗ: 1 King và 1 attacker. Attacker được đứng lên ô có King (ẩn), nhưng 2 attacker không chung ô, 2 King không chung ô.
- Attacker **ở lại trên bàn** qua các lượt; chủ quân được nhấc lên đặt lại / xoay trong phase Attack của mình (trừ khi bị choáng).
- Attacker **bị loại khỏi trận** khi bị thổi bay (Bomb, Tornado) lúc đang đứng ở **viền bàn** (hàng / cột 0 hoặc 9). Muốn quân mới phải rút được lá Knight / Mage. King không bao giờ bị loại kiểu này.

## 3. Lượt chơi

- **Turn** = 1 vòng đủ của cả 2 người (tăng khi người đi trước vào Attack). Battle Report ghi `Turn N - P1: ...` (giữ **Tab** để xem).
- Người đi trước mặc định là P1 ⚙️.

| Phase | Ai | Làm gì |
|---|---|---|
| **Deploy** (chỉ đầu trận) | P1 rồi P2 | Đặt đủ 2 King lên bàn mình (tay bài chỉ có 2 lá King) |
| **Setup** | Người tới lượt | Được dời King của mình sang ô khác (không bắt buộc) |
| **Attack** | Người tới lượt | Đặt / dời / xoay attacker trên bàn đối thủ, **dùng lá phép**, dùng Scout |
| **End (Results)** | Người tới lượt | Xem kết quả battle, bấm END TURN |

Luồng: Deploy P1 → Deploy P2 → Attack P1 → End P1 → Setup P2 → Attack P2 → End P2 → Setup P1 → …
(Lượt đầu tiên của người đi trước không có Setup.)

## 4. Battle (khi bấm CONFIRM ở phase Attack)

Thứ tự xử lý:

1. **Bẫy Tornado** của bên bị đánh nổ trước (theo thứ tự đặt).
2. Liệt kê các **đòn**: mỗi cặp (attacker của mình, King còn sống của đối thủ) là 1 đòn nếu
   - tầm đánh phủ ô King: **+1 damage**;
   - attacker đứng **đúng ô King** (đè bẹp): **thêm +1** (tổng 2, vì ô đứng cũng nằm trong tầm);
   - DMG Boost: **+1** mỗi đòn.
   - Nhiều attacker trúng cùng 1 King thì **cộng dồn**.
3. Các đòn chạy theo **event counter**: thứ tự lúc attacker được đặt / dời lần cuối (đặt trước đánh trước). Quân bị hiệu ứng đẩy đi (đè nảy, Bomb, Tornado) giữ số cũ. Attacker bị choáng hoặc bị loại giữa chừng thì các đòn sau của nó mất.
4. Với mỗi đòn: **hiệu ứng trên King bị đánh chạy trước** (Shield, Reflect), **rồi tới hiệu ứng của attacker** (Shield của attacker chặn choáng, Bomb của attacker nổ khi nó bị choáng).
5. King trúng đòn → **lộ** (đối thủ thấy) tới hết lượt đó. Đòn bị Shield / Reflect chặn vẫn làm King lộ.
6. Cộng damage của Bomb (nếu có) vào kết quả. King trúng bom bị thổi bay **sau** khi đã tính xong damage.
7. Attacker đè bẹp King → **nảy** sang 1 ô ngẫu nhiên trong vùng 3x3 quanh King (ô không có attacker và không có King còn sống). Không còn ô thì đứng yên.
8. King hết máu → bị đánh văng khỏi bàn, rời ô.

Các lá có hiệu lực tức thì (Chest, Healing, Clone) chạy ngay lúc dùng, không xếp hàng theo event counter.

**Ẩn / lộ King**
- King chỉ lộ trong lượt bị đánh. Sang lượt mới (End → Setup) mọi King còn sống **ẩn lại**, dù đứng yên.
- Chủ quân nhấc King lên dời chỗ → ẩn lại.
- Bị dịch chuyển (Rift) hoặc thổi bay (Tornado, Bomb) → ẩn lại.

## 5. Thẻ Scout (thẻ quân, không phải lá phép)

- Mỗi người **1 lần / trận** ⚙️, dùng ở phase Attack.
- Lá Scout nằm trên tay (trừ Deploy) tới khi dùng, và chiếm 1 chỗ trong giới hạn 8 lá. Ngoài phase Attack thì bấm vào lá chỉ bị rung (không dùng được).
- Chọn vùng 2x2 trên **bàn đối thủ**: lộ mọi King còn sống trong vùng tới hết lượt.
- Làm lộ King đang có Suicide Bomb → bom nổ.

## 6. Rút bài

- Deploy: tay bài chỉ có 2 lá King.
- Đầu mỗi lượt (Setup; lượt đầu của người đi trước là Attack ngay sau Deploy) phải bấm nút **DRAW** trước, chưa bấm thì không đặt / dời quân, dùng bài hay Confirm được.
- Mỗi lượt Draw **1 lá, 1 lần** (kể cả lượt đầu). Tay mở đầu: mỗi người có sẵn **1 Knight + 1 Mage** (+ lá Scout).
- Chỉ lá vừa rút (Draw / Chest) mới chạy hoạt ảnh bay vào tay; lá đã có sẵn hiện thẳng (kể cả sau màn che đổi người).
- Tay bài hiện ở mọi phase của người đang tới lượt; bấm lá không dùng được lúc đó thì lá rung + nháy đỏ, không đánh được.
- Mỗi lá rút ra có **25%** ⚙️ là lá quân (Knight hoặc Mage, 50/50), còn lại là 1 lá phép ngẫu nhiên trong danh sách `_deck` của `GameFlow` ⚙️.
- Bộ bài **vô hạn**. **Chưa** giới hạn số lá mỗi loại, **chưa** có cost. Lá quân chưa dùng thì giữ trên tay qua các lượt.
- Tay bài tối đa **8 lá** ⚙️ (lá phép + Knight + Mage + Scout = đúng số lá đang hiện trên tay; không tính King lúc Deploy). Tay đầy thì lượt rút không rút thêm, phần dư bị bỏ; đang 8/8 thì **không dùng được Chest**. Góc dưới trái hiện số lá đang cầm, vd `3/8`.

## 7. Lá phép

### 7.1 Cách dùng
- Chỉ dùng trong **phase Attack của mình**, trước CONFIRM.
- Click lá → click ô mục tiêu; chuột phải / Esc để huỷ (lá quay về tay). Ô không hợp lệ tô đỏ, không chọn được.
- Battle Report chỉ ghi **tên lá** đã dùng, không ghi tọa độ (để không lộ vị trí King).

### 7.2 Chi tiết từng lá

"Quân của mình" = King của mình (chọn trên bàn mình) hoặc Knight / Mage của mình (chọn trên bàn đối thủ).

| Lá | Mục tiêu | Hiệu ứng | Thời hạn |
|---|---|---|---|
| **DMG Boost** | 1 attacker của mình | +1 damage mỗi đòn trúng. Dùng nhiều lá lên cùng quân thì cộng dồn | Hết sau battle kế tiếp của mình |
| **Reflect** | 1 King của mình | Đòn **đầu tiên** trúng King: King không mất máu, attacker bị **choáng 2 turn** | Tới khi phản 1 đòn, hoặc hết sau battle kế tiếp của đối thủ |
| **Shield** | 1 quân của mình | **King:** chặn **toàn bộ** damage của đòn đầu tiên (kể cả +1 đè bẹp và DMG Boost). **Attacker:** chặn 1 lần bị choáng hoặc bị bom thổi bay | King: tới khi chặn 1 đòn hoặc hết sau battle kế tiếp của đối thủ. Attacker: tới khi chặn 1 lần hoặc hết sau battle kế tiếp của mình 📝 |
| **Chest** | Bản thân | Rút thêm **2 lá** (cũng có 25% ra lá quân) | Tức thì |
| **Rift** | 2 ô trên **bàn đối thủ**: cổng A rồi cổng B | Cổng **2 chiều**. Đòn của Knight / Mage đi vào 1 cổng thì ra ở cổng kia, giữ hướng; **ô cổng không bị đánh và không tốn tầm**, đòn đi tiếp phần tầm còn lại. Mỗi cổng chỉ đi qua 1 lần trong 1 đòn (để đường đánh không lặp vô hạn); nhiều quân đều đánh qua được. Nếu A đang có King còn sống (B không có King): King dịch sang B, ẩn lại, cả 2 bên thấy hiệu ứng dịch chuyển | 3 turn |
| **Tornado** | Vùng 2x2 trên bàn mình | **Bẫy**, đối thủ không thấy. Nổ khi đối thủ CONFIRM battle kế tiếp, **trước** mọi đòn: mọi quân trong vùng (kể cả King của mình, King giả, attacker đối thủ) bị thổi bay sang ô **trống hoàn toàn** ngẫu nhiên **ngoài vùng**; attacker đang ở viền bàn thì bị loại; King ẩn lại; attacker giữ hướng và vẫn đánh từ ô mới | 1 lần, ở battle kế tiếp của đối thủ |
| **Healing** | 1 quân của mình **cần hồi** | **King** (chưa đầy máu): +2 HP, không vượt 10. **Attacker** (đang choáng): hết choáng | Tức thì |
| **Suicide Bomb** | 1 quân của mình | **Trên King:** nổ khi King **bị lộ** (do battle hoặc Scout); attacker đối phương trong vùng bị thổi bay (ở viền thì bị loại) và **choáng 2 turn**. **Trên attacker:** nổ khi attacker đó **bị choáng**; King đối phương trong vùng mất **1 máu** (cộng vào damage battle) rồi bị thổi bay. Quân giữ bom không mất máu. Vùng nổ: **turn 1–2: 3x3, turn 3–4: 5x5, từ turn 5: 7x7**, tâm là ô quân giữ bom. Nổ dây chuyền được, mỗi bom nổ 1 lần | 3 turn |
| **Clone** | 1 King của mình, rồi 1 ô không có King | Tạo King giả: HP = HP hiện tại của King gốc, nhận damage / lộ / chết y như King; đối thủ không phân biệt được. **Không** tính vào điều kiện thua, không tính vào bảng KINGS. Bị tấn công **lần thứ 2** (mỗi attacker đánh trúng tính 1 lần, kể cả đòn bị Shield/Reflect chặn): King giả **nổ tung** thay vì nhận damage, attacker vừa đánh nó bị **văng khỏi bàn, loại khỏi trận** (Shield không chặn) | Tới khi hết HP, nổ, hoặc sau 3 turn |
| ~~Brawler~~ | | Chưa làm | |

**Phối hợp:** Reflect + Shield trên cùng 1 King kích hoạt **cùng lúc** vào cùng đòn đầu tiên và **cùng bị tiêu hao**.

### 7.3 Đếm thời hạn

Mỗi vòng mỗi người có đúng 1 lượt tấn công, nên "N turn" = N vòng.

| Status | Đếm "turn" theo |
|---|---|
| Choáng | Số battle của **chủ attacker** (choáng từ trước battle mới bị trừ; vừa bị choáng trong battle thì chưa trừ) |
| DMG Boost, Shield / Bomb trên attacker, Rift | Số battle của **người dùng lá** |
| Shield / Reflect / Bomb trên King, King giả | Số battle của **đối thủ** |

### 7.4 Ai thấy gì

- Vòng hiệu ứng Shield / Reflect / Bomb / Boost / King giả và vùng bẫy Tornado: **chỉ chủ quân thấy** khi đang tới lượt mình. Đối thủ chỉ biết khi lá kích hoạt (khiên vỡ, sét phản, bom nổ, lốc xoáy).
- Cổng Rift: **cả 2 bên đều thấy**. Preview tầm đánh khi kéo quân cũng đi qua cổng.

---

## 8. Phân loại thẻ (bảng tùy chọn)

Tick `[x]` vào cột thẻ tác động lên. Một thẻ có thể thuộc nhiều cột.

- **Attacker**: thẻ tác động lên Knight / Mage (của mình hoặc đối thủ).
- **King**: thẻ tác động lên King (thật hoặc giả).
- **Bàn / Khác**: thẻ tác động lên ô trên bàn, lên tay bài, hoặc không nhắm vào quân cụ thể.

| Thẻ | Attacker | King | Bàn / Khác | Ghi chú |
|---|:---:|:---:|:---:|---|
| Knight / Mage | [x] | [ ] | [ ] | Lá quân, rút từ bộ bài (25%) |
| DMG Boost | [x] | [ ] | [ ] | Buff attacker của mình |
| Reflect | [x] | [x] | [ ] | Đặt lên King; choáng attacker đối thủ |
| Shield | [x] | [x] | [ ] | King: chặn đòn; attacker: chặn choáng / bom |
| Chest | [ ] | [ ] | [x] | Tác động lên tay bài |
| Rift | [x] | [x] | [x] | Đặt lên ô bàn đối thủ; dịch King; bẻ đòn mọi attacker |
| Tornado | [x] | [x] | [x] | Bẫy trên vùng ô; thổi bay mọi quân |
| Healing | [x] | [x] | [ ] | King: +2 HP; attacker: hết choáng |
| Suicide Bomb | [x] | [x] | [ ] | Gắn lên King hoặc attacker |
| Clone | [ ] | [x] | [x] | Đặt King giả lên ô trống |
| Scout | [ ] | [x] | [x] | Thẻ quân; quét vùng 2x2 bàn đối thủ |
| Brawler | [ ] | [ ] | [ ] | Chưa làm |

> Code chưa đọc bảng này. Muốn dùng để chia nhóm trong game (màu viền lá, giới hạn theo nhóm…) thì báo mình.

---

## 9. Còn cần bạn xác nhận

1. 📝 Shield trên attacker hết hạn sau battle kế tiếp **của chủ attacker** (attacker chủ yếu bị choáng / bị bom trong chính battle của mình).
2. 📝 Damage 1 của Bomb trên attacker **không** bị Shield của King chặn (Shield chỉ chặn đòn đánh).
3. Ảnh lá Scout vẫn in chữ "ONCE PER MATCH". Mô tả khi chuột phải đã đổi thành "3 times per match, once per turn", còn ảnh thì cần vẽ lại.
4. Lá Knight / Mage dùng chung ảnh với lá quân cũ; tay bài hiện số lá theo số lần rút được.
