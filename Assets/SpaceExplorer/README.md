# Asset cho Lab 1 — Space Explorer

Bộ hình cho bài Lab 1: điều khiển tàu bằng phím mũi tên, bắn laser, né thiên thạch, thu thập sao và hiển thị điểm.

## Danh sách

| File | Công dụng |
| --- | --- |
| Sprites/Spaceship.png | Tàu người chơi, mũi hướng lên (+Y), nền trong suốt |
| Sprites/Asteroid.png | Thiên thạch, nền trong suốt; đổi rotation và scale để tạo biến thể |
| Sprites/Star.png | Sao vàng thu thập, nền trong suốt |
| Sprites/Laser.png | Đạn laser cyan hướng +Y, nền trong suốt |
| Backgrounds/Space.png | Nền không gian cho Menu, Gameplay và End Game |
| UI/Button_Normal.png | Nút trạng thái thường |
| UI/Button_Hover.png | Nút khi rê chuột/chọn |
| UI/Button_Pressed.png | Nút khi nhấn |
| UI/Panel.png | Khung Instructions hoặc kết quả |

## Sử dụng trong Unity

1. Đợi Unity import và biên dịch script Editor. Nếu ảnh đã được import trước script, chọn **Tools > Space Explorer > Reimport Artwork**.
2. Kéo sprite vào Scene để tạo SpriteRenderer. Bộ import đặt Sprite Single, Bilinear, không mipmap, không nén. Dùng material Sprite-Unlit-Default nếu muốn hình giữ màu mà không phụ thuộc đèn URP 2D.
3. Với camera Orthographic Size = 5, tỉ lệ 16:9, dùng scale tàu khoảng 0.3, thiên thạch 0.15–0.35, sao 1–1.4, laser 1. Nền scale khoảng 1.07 và Sorting Order = -100; đối tượng gameplay = 0.
4. Tàu: Rigidbody2D (Gravity Scale = 0) và PolygonCollider2D. Chỉnh collider sát thân để tránh va chạm quá rộng ở cánh.
5. Thiên thạch: Rigidbody2D (Gravity Scale = 0) và CircleCollider2D hoặc PolygonCollider2D.
6. Sao/laser: Collider2D bật Is Trigger; ít nhất một đối tượng trong cặp va chạm có Rigidbody2D.
7. UI Image dùng Button_Normal; Image Type = Sliced. Button Transition = Sprite Swap, Highlighted Sprite = Button_Hover, Pressed Sprite = Button_Pressed. Thêm chữ bằng TextMeshPro: Play, Instructions, Main Menu, Quit. Panel dùng Image Type = Sliced.
8. Dùng sao nhỏ trong HUD cùng chữ Score. Tạo panel Instructions với hướng dẫn phím mũi tên và phím bắn bạn chọn.

Đây là bộ asset hình ảnh và cấu hình import; scene, prefab và logic gameplay chưa được triển khai. Đề lab mô tả cả trừ điểm khi va chạm và kết thúc game khi chạm thiên thạch; khi viết gameplay cần chọn quy tắc nhất quán.

## Nguồn và cách tạo

Tàu, thiên thạch và nền được tạo bằng công cụ imagegen tích hợp. Các prompt đầy đủ lưu trong PROMPTS.md. Sao, laser và UI được vẽ bằng script Tools/CreateLabAssets.ps1 trong dự án (System.Drawing). Không có asset tải từ bên thứ ba.

Đã kiểm tra kích thước file, nền alpha của sprite/UI và xem trực tiếp ba ảnh imagegen. Chưa kiểm thử hiển thị trong Unity Editor.

