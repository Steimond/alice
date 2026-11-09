# Revision16 — главное меню

8 ноября 2026, сервис PixelLab, по просьбе владельца: главное меню в стиле Алисы, логотип «Alice», «не стесняйся генерить ассеты». 29 генераций. Всё в `menu/`.

## Логотип

`create_image_pro_flash` 256 × 96, по 9 генераций. Образец стиля — `Revision03/flash-stand-01.png` (`upload:7c9295b3-6efb-486c-ac10-2b148b19e35f`, «Style only… Do not draw this girl.»). Описание (точно): «Game title logo for a dark 18+ fairy-tale 2D platformer: the single word "Alice" written large in ornate Victorian storybook lettering (capital A, then lowercase l, i, c, e), thick letters of tarnished antique gold with dark brown outlines and engraved shading, a thin thorny black rose vine curling around the letters with one dark red rose, the dot over the i is a small red playing-card heart, a tiny brass key hangs on a chain from the A. Only these five letters, no other words, no numbers. The letters fill the whole width of the image, centred. Transparent background, no plaque, no frame, no shadow.»

| Вариант | Job | Seed | Итог |
| --- | --- | --- | --- |
| A | `8ee57f90-daf2-4aab-8a6d-614715662cb4` | 1601 | Запасной: тёплое золото, ключ слева, буквы мельче (`logo-a.png`) |
| B | `18d8a765-5650-46c9-a8fc-03b2c8739da9` | 1602 | **Взят**: крупные читаемые буквы тусклого золота, ключ висит на A, роза справа (`logo-b.png`) |

Оба написали слово без ошибок. Сравнение — `logos-x3.png`.

**Сборка** — `compose-logo.ps1`: буквы с жёсткой альфой на табличке с ровной заливкой (14, 12, 22), срезанными углами и рамкой в два тона золота; под буквами шрифтом 5 × 7 цветом (15, 13, 24) — строка владельца «Alisa K. 25.01.22» (пасхалка). Разница около одного уровня яркости: обычным глазом не видна, проявляется при поднятых яркости и контрасте. Проверка — `menu_logo-contrast.png` (уровни 0…21 растянуты на 0…255), `menu_logo-contrast-x2.png`. Сначала была разница 3, потом 2: на снимке игры строку было видно и так — уменьшено до (1, 1, 2). Результат — `menu_logo.png`, 253 × 117.

## Фон

`create_image_pixen` 320 × 180 (в игре вдвое, на весь экран 640 × 360), по 1 генерации, `detail` = highly detailed, `view` = side.

| Файл | Job | Seed | Итог |
| --- | --- | --- | --- |
| `bg-a.png` | `0eeb59ac-418f-400e-8b36-99103f31123d` | 1611 | Отброшен: слева бессмысленная надпись, Алиса темноволосая |
| `bg-b.png` | `2f83b326-040d-4628-8116-a715c1198203` | 1612 | Отброшен: надпись, Алиса мультяшная, светлый тон |
| `bg-c.png` | `85fcd22c-891e-4905-a1a6-aba4614892ad` | 1613 | **Взят**: без текста, слева тёмная ниша в корнях под логотип и меню, Алиса с медовыми волосами в синем платье среди часов, карт, ключей и мебели |
| `bg-d…f.png` | `db4591d2…`, `b0b2baf9…`, `56ec2575…` | 1614–1616 | Совпали с `bg-c` пиксель в пиксель: seed у Pixen на этом размере не влияет |

Первое описание: «dark gothic Wonderland scene for a game main menu background: Alice falling down the rabbit hole…». Второе (взятое, точно): «dark gothic main menu background, no text, no letters, no words, no numbers anywhere: a deep earthen rabbit hole shaft seen from the side with twisted roots, on the right side a slender girl with honey-blonde hair in a muted blue Victorian dress and white apron falls slowly, small and far, among floating antique furniture, a grandfather clock, a pocket watch, playing cards, a teacup and brass keys, lit by a pale cold shaft of light from above; the whole left half is a dark empty void of deep plum and black shadow framed by roots, nothing in it; muted desaturated palette, dark blue, purple, brown, grim and melancholic, detailed pixel art». Листы — `sheet1.png`, `sheet2.png`.

## Курсор

`create_image_pro_flash` 32 × 32, 5 генераций, job `ccaf2a3e-d5da-4f24-8f33-81db28ab902a`, seed 1621: «A small ornate antique brass key lying horizontally, seen exactly from the side, the bow (handle) with a heart-shaped loop on the left and the bit on the right, pointing right, for a game menu cursor…». Обрезан по нарисованному с жёсткой альфой — `menu_cursor.png`, 25 × 11.

## В игре

`menu_background.png` (= `bg-c.png`), `menu_logo.png`, `menu_cursor.png` — в `Assets/_Alice/Art/UI/Menu`, импорт без сжатия с точечным фильтром. Проверено в Play: главный экран, слоты (пустые и занятый), действия слота и вопрос перед удалением, настройки на русском и английском, запуск новой игры и продолжение. Снимки при подкрученных яркости и контрасте показывают строку владельца, обычный снимок — нет.
