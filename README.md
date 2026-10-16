# Space Explorer – Lab 1 (2D Unity Game)

Người chơi điều khiển một phi thuyền bay giữa các thiên thạch, nhặt sao để ghi điểm.
Game có 3 scene: **Main Menu → Gameplay → End Game**, chuyển cảnh bằng hiệu ứng fade.

Engine: Unity 6000.6.4f1, URP 2D, **Input System mới** (Project Settings › Active Input Handling = Input System Package).

---

## 1. Cách dựng project (1 click)

1. Mở project bằng Unity, đợi Unity biên dịch script xong.
2. Trên thanh menu chọn **Space Explorer › Build Project**.
3. Script [`SpaceExplorerBuilder`](Assets/Editor/SpaceExplorerBuilder.cs) dựng giao diện theo bản thiết kế trong
   `DesignPreviews/` (`Menu.png`, `Gameplay.png`), dùng artwork trong `Assets/SpaceExplorer/`
   (Spaceship, Asteroid, Star, Laser, nền Space) và tự tạo:
   - `Assets/Art/Generated/*.png` – hình UI vẽ bằng code: khung/nút bo góc, viền sáng, phím mũi tên,
     icon loa, icon pause, chấm sao nền
   - `Assets/Art/SpriteUnlit.mat` – material sprite unlit (không cần đặt đèn 2D)
   - `Assets/Prefabs/` – `Laser`, `Asteroid`, `Star`
   - `Assets/Scenes/` – `MainMenu`, `Gameplay`, `EndGame`
   - Danh sách scene trong Build Profile: MainMenu (0), Gameplay (1), EndGame (2)
4. Mở scene **MainMenu** và bấm **Play**.

> Có thể chạy lại menu này bất cứ lúc nào; scene và prefab sẽ được tạo lại.
> **Lưu ý:** mọi chỉnh sửa tay trong 3 scene/prefab sẽ bị ghi đè khi chạy lại.

### Asset & kích thước
Ảnh trong `Assets/SpaceExplorer/` được [`SpaceExplorerAssetImporter`](Assets/SpaceExplorer/Editor/SpaceExplorerAssetImporter.cs)
import thành Sprite (256 PPU, nền 100 PPU, UI có viền 9-slice 12 px). Builder đặt scale:

| Đối tượng | Scale | Collider |
|---|---|---|
| Spaceship | 0.3 | `PolygonCollider2D` theo đường viền sprite |
| Asteroid | 0.25 × ngẫu nhiên 0.6–1.4 | `CircleCollider2D` (trigger) |
| Star | 1.2 | `CircleCollider2D` (trigger) |
| Laser | 1 | `BoxCollider2D` (trigger) |
| Background | tự phủ kín camera (`BackgroundFitter`) | – |

Muốn đổi hình: thay file PNG cùng tên trong `Assets/SpaceExplorer/` rồi chạy lại **Space Explorer › Build Project**.

### Giao diện (UI/UX) theo `DesignPreviews/`
- **Bảng màu:** nền navy tối, nhấn **cyan** (`#55E6FF`) cho viền/nút chính, **vàng** cho sao/điểm.
- **3 kiểu nút** (`ButtonFx`): *Primary* nền cyan đặc + viền sáng mờ (PLAY, PLAY AGAIN, RESUME, CLOSE);
  *Secondary* nền tối viền cyan (INSTRUCTIONS, MAIN MENU); *Subdued* xám nhạt (QUIT).
  Rê chuột / chọn bằng phím: nút sáng lên và phóng to 4%; nhấn: thu nhỏ; có tiếng click.
- **Bàn phím:** điều hướng menu bằng mũi tên + Enter (nút đầu tiên được chọn sẵn).
- **Main Menu:** logo + tên game góc trái, tiêu đề lớn "SPACE / EXPLORER", phi thuyền lớn, thiên thạch và sao
  trôi nhẹ bên phải (`UIFloat`), gợi ý phím góc dưới trái, nút loa góc phải.
- **Gameplay HUD:** khung SCORE (icon sao, số điểm nảy lên khi cộng điểm), khung LIVES (icon tàu, mạng mất bị mờ),
  tên game ở giữa trên, nút loa + pause góc phải, thanh hướng dẫn MOVE / SHOOT ở dưới (tự mờ sau 5 giây).
- **Pause:** Esc / P hoặc nút pause → lớp phủ PAUSED với RESUME / MAIN MENU.
- **Âm thanh:** hiệu ứng bắn, nhặt sao, nổ, bị đâm, click được tạo bằng code (`SoundManager`), bật/tắt bằng nút loa
  (lưu bằng `PlayerPrefs`).
- Project dùng **Linear color space**, nên các khung modal để nền đặc (alpha 1): alpha một phần trông nhạt hơn nhiều.

---

## 2. Điều khiển

| Phím | Hành động |
|---|---|
| Mũi tên / WASD | Di chuyển phi thuyền theo mọi hướng |
| Space / Left Ctrl | Bắn laser |
| Esc / P | Tạm dừng / tiếp tục |
| Mũi tên + Enter | Chọn nút trong menu |

## 3. Luật chơi & tính điểm

| Sự kiện | Điểm |
|---|---|
| Nhặt sao | **+10** |
| Bắn nổ thiên thạch | **+2** |
| Đâm vào thiên thạch | **−5** và mất 1 mạng |

- Bắt đầu với **3 mạng**. Hết mạng → **Game Over** → chuyển sang scene End Game.
  (Muốn "đâm 1 lần là thua" như đề: đặt `Starting Lives = 1` trên object `GameManager`.)
- Khi bắt đầu (2 giây) và sau khi bị đâm (1.5 giây), phi thuyền nhấp nháy và không bị trừ mạng.
- Điểm không bao giờ âm. High score được lưu bằng `PlayerPrefs`.
- Càng chơi lâu, số thiên thạch càng tăng (4 → tối đa 12).

---

## 4. Các scene

### MainMenu
| Object | Vai trò |
|---|---|
| `Main Camera` | Camera orthographic size 5, nền màu không gian tối |
| `StarfieldBackground` | Nền sao cuộn parallax 3 lớp (trang trí) |
| `Canvas` › `Menu` › `PlayButton` | Gọi `MainMenuController.Play()` → fade sang Gameplay |
| `Canvas` › `Menu` › `InstructionsButton` | Gọi `ShowInstructions()` → ẩn `Menu`, bật panel hướng dẫn |
| `Canvas` › `Menu` › `QuitButton` | Thoát game (trong Editor thì dừng Play Mode) |
| `Canvas` › `InstructionsPanel` | Lớp phủ tối + khung hướng dẫn, nút `CLOSE` gọi `HideInstructions()` |
| `EventSystem` | Dùng `InputSystemUIInputModule` để UI nhận chuột/phím |

### Gameplay
| Object | Vai trò |
|---|---|
| `Spaceship` | Người chơi: `SpriteRenderer`, `Rigidbody2D` (Dynamic, gravity 0), `PolygonCollider2D`, `PlayerController` |
| `GameManager` | Giữ điểm, mạng, cập nhật HUD, xử lý Game Over |
| `Spawner` | Sinh thiên thạch và sao ở vị trí ngẫu nhiên, tránh chỗ người chơi đang đứng |
| `Canvas` › `ScorePanel`, `LivesPanel` | HUD: điểm (icon sao) và mạng (3 icon tàu) |
| `Canvas` › `SoundButton`, `PauseButton` | Bật/tắt âm thanh, tạm dừng |
| `Canvas` › `HelpStrip` | Gợi ý phím MOVE / SHOOT, mờ dần sau 5 giây (`FadeAfterDelay`) |
| `Canvas` › `PauseOverlay` + `PauseController` | Màn hình tạm dừng (`Time.timeScale = 0`) |
| `StarfieldBackground` | Nền sao cuộn |

### EndGame
| Object | Vai trò |
|---|---|
| `ScoreText` | Điểm của lượt vừa chơi (đọc từ `GameSession.LastScore`) |
| `HighScoreText` | High score hoặc thông báo "NEW HIGH SCORE!" |
| `PlayAgainButton` / `MainMenuButton` / `QuitButton` | Chơi lại / về menu / thoát |

---

## 5. Các thành phần game (Game Elements)

### Spaceship – [`PlayerController.cs`](Assets/Scripts/Gameplay/PlayerController.cs)
- Đọc bàn phím qua `Keyboard.current` (Input System), chuẩn hóa vector để đi chéo không nhanh hơn.
- Di chuyển bằng cách gán `Rigidbody2D.linearVelocity` (có gia tốc mượt), giữ trong khung camera.
- Nghiêng nhẹ (banking) khi di chuyển ngang – chỉ để đẹp.
- Bắn: `Instantiate` prefab `Laser` tại mũi tàu, có cooldown 0.2 giây.
- `OnTriggerEnter2D`: chạm `Star` → cộng điểm; chạm `Asteroid` → trừ điểm, mất mạng, bất tử tạm thời.

### Laser – [`Laser.cs`](Assets/Scripts/Gameplay/Laser.cs)
- `Rigidbody2D` Dynamic (gravity 0) bay thẳng lên; collider là **trigger**.
- Chạm thiên thạch → thiên thạch nổ, +2 điểm, laser biến mất. Tự hủy sau 1.5 giây.

### Asteroid – [`Asteroid.cs`](Assets/Scripts/Gameplay/Asteroid.cs)
- Khi sinh ra: kích thước, hướng, tốc độ và tốc độ xoay đều **ngẫu nhiên**.
- Cứ 2–4 giây đổi hướng ngẫu nhiên (±60°) → chuyển động ngẫu nhiên thật sự.
- Nảy lại khi chạm mép màn hình. `Rigidbody2D` Kinematic + collider trigger.

### Star – [`Star.cs`](Assets/Scripts/Gameplay/Star.cs)
- Trôi chậm, xoay, nhấp nháy (phóng to/thu nhỏ theo hàm sin). Trôi ra ngoài thì quay lại từ phía đối diện.
- `Points` = 10 (chỉnh được trên prefab). `Collect()` tạo hiệu ứng lấp lánh rồi hủy.

### Hệ thống va chạm (Physics & Colliders)
| Object | Rigidbody2D | Collider |
|---|---|---|
| Spaceship | Dynamic, gravity 0 | Circle, **không** trigger |
| Asteroid | Kinematic | Circle, trigger |
| Star | Kinematic | Circle, trigger |
| Laser | Dynamic, gravity 0 | Box, trigger |

Va chạm được phát hiện bằng `OnTriggerEnter2D`; loại object được nhận diện bằng
`TryGetComponent<Asteroid/Star>()` thay vì tag, nên không cần cấu hình Tag Manager.

---

## 6. Script hỗ trợ (`Assets/Scripts/Core` và `UI`)

| Script | Vai trò |
|---|---|
| [`SceneNames`](Assets/Scripts/Core/SceneNames.cs) | Hằng số tên scene, tránh gõ chuỗi sai |
| [`SceneFader`](Assets/Scripts/Core/SceneFader.cs) | Chuyển scene mượt: fade đen → `LoadSceneAsync` → fade sáng. Tự tạo và `DontDestroyOnLoad` |
| [`GameSession`](Assets/Scripts/Core/GameSession.cs) | Truyền điểm từ Gameplay sang EndGame (biến static) + lưu high score (`PlayerPrefs`) |
| [`ScreenBounds`](Assets/Scripts/Core/ScreenBounds.cs) | Tính vùng nhìn thấy của camera trong world space |
| [`PopEffect`](Assets/Scripts/Core/PopEffect.cs) | Hiệu ứng nổ/lấp lánh: sprite phóng to và mờ dần |
| [`StarfieldBackground`](Assets/Scripts/Gameplay/StarfieldBackground.cs) | Lớp sao cuộn parallax 3 lớp phía trên ảnh nền |
| [`BackgroundFitter`](Assets/Scripts/Core/BackgroundFitter.cs) | Phóng ảnh nền vừa khít camera ở mọi tỉ lệ màn hình |
| [`MainMenuController`](Assets/Scripts/UI/MainMenuController.cs) | Xử lý các nút ở Main Menu |
| [`EndGameController`](Assets/Scripts/UI/EndGameController.cs) | Hiển thị điểm, xử lý nút ở End Game |
| [`QuitHelper`](Assets/Scripts/UI/QuitHelper.cs) | `Application.Quit()`, trong Editor thì dừng Play Mode |
| [`SoundManager`](Assets/Scripts/Core/SoundManager.cs) | Tạo và phát hiệu ứng âm thanh bằng code, lưu trạng thái tắt tiếng |
| [`ButtonFx`](Assets/Scripts/UI/ButtonFx.cs) | Hiệu ứng hover / chọn / nhấn cho nút + tiếng click |
| [`SoundToggleButton`](Assets/Scripts/UI/SoundToggleButton.cs) | Nút loa: bật/tắt tiếng, đổi icon |
| [`PauseController`](Assets/Scripts/UI/PauseController.cs) | Tạm dừng / tiếp tục / về menu |
| [`UIFloat`](Assets/Scripts/UI/UIFloat.cs) | Hiệu ứng trôi nhẹ + xoay cho hình trang trí ở menu |
| [`FadeAfterDelay`](Assets/Scripts/UI/FadeAfterDelay.cs) | Làm mờ thanh hướng dẫn sau vài giây |

## 7. Luồng game

```
MainMenu --Play--> Gameplay --hết mạng--> EndGame --Main Menu--> MainMenu
   |                                         |--Play Again--> Gameplay
   +--Instructions (panel)                   +--Quit
```
Mọi lần chuyển scene đều đi qua `SceneFader.LoadScene(...)` (fade 0.5s ra + 0.5s vào).
