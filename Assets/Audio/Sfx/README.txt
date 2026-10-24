THAY ÂM THANH TRONG GAME
========================

1. Chép file âm thanh (.wav / .mp3 / .ogg) vào thư mục này: Assets/Audio/Sfx/
2. Trong Unity, chọn file Assets/Resources/Audio/SfxLibrary.asset
3. Trong Inspector, kéo file vào ô "Clip" của âm muốn thay. Chỉnh "Volume" (0..2) nếu to/nhỏ quá.
4. Ô "Clip" để trống = game dùng âm tổng hợp sẵn (như cũ).

Mẹo: file ngắn (< 1 giây) nên đặt Load Type = Decompress On Load trong Inspector của file để phát tức thì.

DANH SÁCH ÂM (tên ô -> phát khi nào)
-----------------------------------
Pop           quân xuất hiện từ lá bài / quân mới bị huỷ khi kéo
Yelp          quân bị nhấc lên (giật mình), nhảy lên đè bẹp
Thud          quân rơi xuống chạm bàn, nảy khi bị đánh văng
Whoosh        quân lao lên đánh, King xoay người biến mất (ẩn)
CameraWhoosh  camera bay sang góc khác
Punch         đòn trúng King, chữ K.O. đập xuống
Squish        King bị đè bẹp
Boing         nảy trên "bánh kếp", King bị bắn lên trời, King hiện ra
SlideWhistle  King bay vút lên trời / bị đánh văng
Ding          ngôi sao loé trên trời, thẻ Trinh sát tìm thấy King
CardHover     rê chuột qua lá bài
CardFlip      bấm lá bài, mở xem chi tiết lá bài (chuột phải)
CardDeal      chia lá bài lên tay
Deny          không được dùng lá bài lúc này, Trinh sát không thấy gì, rê vào nút QUIT ở menu
UiHover       rê chuột qua nút
UiClick       bấm nút, bấm màn hình đổi lượt
Chime         banner đổi phase
Fanfare       màn hình thắng
Tick          (xoay quân khi đang cầm)
Splash        King rơi xuống biển
Poof          khói "bụp" (King ẩn / hiện, tan rã)
Magic         Mage kết liễu King (tan rã phép)
Slash         Knight chém
Zap           tia phép của Mage bắn ra
