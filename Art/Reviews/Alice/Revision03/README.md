# Алиса — проба PixelLab, стойка

2 ноября 2026, по просьбе владельца. Сервис PixelLab (пробный тариф), инструмент `create_image_pro` через MCP. Задача — калибровка стойки для этапа 0 из [требований к Алисе](../../../../Docs/AliceCharacterArt.md). Это ревью, не игровой ассет.

## Запрос

- Холст 48×56, 16 вариантов за один вызов, стоимость 10 генераций из 40 пробных.
- `max_colors` = 30, прозрачный фон.
- Референс — вид сбоку из `Art/Concepts/Alice/alice-concept-v02.png` (`reference-side-crop.png`), пометка: «character design, outfit, hair and colours (right-facing side view)».
- Job `84481c27-ad96-4d3d-90bb-35eb021802f8`, asset `b5ded2de-1396-5d3b-9041-e4a8cc47669d`.
- Описание (точно):

> Side-view 2D platformer player sprite, idle standing pose, full body in strict right-facing profile. Alice, a 15-year-old girl with natural teenage proportions (not chibi, not anime): shoulder-length honey-blonde wavy hair, dark charcoal ribbon bow on her head, muted blue Victorian calf-length dress with puffed short sleeves and white collar, ivory apron with a big bow tied at the back, dirt stains on the hems, dark grey stockings, brown lace-up boots. She holds a long thin steel sewing needle (about two thirds of her height, eye of the needle visible) loosely in her right hand, point down. Body about 46 pixels tall from boot soles to top of head, feet resting flat on the bottom row of the canvas. Dark gothic fantasy, detailed hand-placed pixel art like Blasphemous, crisp clusters, dark outline, no anti-aliasing, no dithering noise, no text.

## Файлы

- `candidates/stand-00.png` … `stand-15.png` — все 16 вариантов в исходном размере 48×56.
- `stand-sheet-x6.png` — все варианты ×6 на сером фоне.
- `stand-pick-x10.png` — варианты 1, 3, 9, 13 ×10 на тёмном фоне.

## Замер (автоматически, по каждому файлу)

| Проверка | Результат | Требование |
| --- | --- | --- |
| Пиксельная сетка | Чистая, 1 пиксель = 1 пиксель | Раздел 4 |
| Цвета | Ровно 30 в каждом варианте; у вариантов палитры разные | ~30, общая закреплённая палитра |
| Полупрозрачность | 0 пикселей | Только 0 или 100 % |
| Высота с волосами и бантом | 48–52 px | Тело ~46, бант до 48–50 |
| Ширина | 24–27 px | Корпус в 12 px коллайдера, юбка и волосы могут выходить |
| Зазор под ступнями | 1–4 px | 0 — исправляется сдвигом |

## Оценка

- Задача, на которой сорвались обе попытки image_gen, решена: настоящая сетка нужного размера, ограниченная палитра, без полупрозрачности.
- Облик держится: все 16 вариантов — узнаваемо одна Алиса по концепту v02.
- До уровня Blasphemous далеко: кластеры шумные, платье и передник сливаются, нет уверенного контура, лицо почти пустое.
- Голова крупная — около 1:3,5 к росту, ближе к чиби, чем к естественным пропорциям подростка из требований.
- Игла видна только нижним концом, около 10–12 px вместо ~32 px; ушка не видно.
- На тёмном фоне голубое платье теряется: критерий читаемости 6 под вопросом.

Вывод: годится как основа для ручной чистки в Aseprite, не как финальный спрайт. Этап 0 не принят.

## Проба Pro Flash

Тот же день, `create_image_pro_flash`, холст 64×64, `max_colors` = 30, одна картинка за 5 генераций, 23 секунды. Образец стиля — `candidates/stand-01.png`, у которого взята только палитра (`color_palette` включён, `outline`, `detail`, `shading` выключены), чтобы модель не переносила шум и пропорции варианта Pro. Job `7923512b-b1cb-4b76-824b-c71954edc9b1`, image `6a1fd65c-cc77-5944-9866-e692a8777a63`.

Описание (точно):

> Side-view 2D platformer player sprite, idle standing pose, strict right-facing profile, full body. Alice, a slender 15-year-old girl with realistic teenage proportions: small head, head is one sixth of her height, long legs, not chibi, not anime. Shoulder-length honey-blonde wavy hair, dark charcoal ribbon bow, muted blue Victorian calf-length dress with puffed short sleeves and white collar, ivory apron with a bow at the back, grey stockings, brown lace-up boots. In her right hand she holds a long thin steel sewing needle, clearly visible, about 30 pixels long, held diagonally downward in front of her, with a readable eye at the top. Figure 46 pixels tall from boot soles to top of head, feet on the bottom of the canvas. Detailed dark gothic pixel art in the style of Blasphemous: clean readable clusters, strong dark outline separating the apron from the dress, high contrast between blue dress and ivory apron, no noise, no anti-aliasing.

Файлы: `flash-stand-01.png` (исходник 64×64), `compare-pro-vs-flash-x10.png` (слева Pro, вариант 1; справа Pro Flash).

| Проверка | Pro, вариант 1 | Pro Flash |
| --- | --- | --- |
| Цвета / полупрозрачность | 30 / 0 | 30 / 0 |
| Габарит с волосами | 24×52 | 30×54 — на 4 px выше предела с бантом |
| Зазор под ступнями | 1 px | 4 px |
| Голова к росту | около 1:3,5 | около 1:4 |
| Игла | 10–12 px, без ушка | около 18 px, видна целиком, ушка нет |
| Передник и платье | сливаются, шум | разделены, кластеры чище |
| Лицо | почти пустое | читается глаз и профиль |

Pro Flash заметно чище и читаемее за половину цены, но это по-прежнему не уровень Blasphemous: пропорции ещё детские, игла короче 32 px, рост на несколько пикселей больше нужного. Для дальнейших проб выгоднее Pro Flash; Pro оправдан, только когда нужен выбор из многих вариантов.

## Проба анимации бега

Тот же день. Первый кадр — `flash-stand-01.png`. Два аниматора по 8 кадров, по 1 генерации каждый; бесплатный тариф выполняет одну задачу за раз. Кадр 0 в обоих наборах — исходная стойка без изменений.

| | `animate_image` | `animate_image_pixminimax` |
| --- | --- | --- |
| Job | `0336e8fe-3f18-42e7-a185-32f79fd35c45` | `d2bfa9c7-518c-40c3-a67e-64aed0d8370f` |
| Кадры | `run-animate/` | `run-pixminimax/` |
| Лента ×6 | `run-animate-strip-x6.png` | `run-pixminimax-strip-x6.png` |
| Движение | Шаг, а не бег; вытянутая вперёд пустая рука | Настоящий бег с длинным шагом и подъёмом колена |
| Игла | Пропадает в кадрах 1–7, возвращается в 8 | Держится в кадрах 1–5, пропадает в 6–7, в 8 уходит за спину |
| Облик | Держится во всех кадрах | Держится, в кадрах 2–3 и 8 силуэт шире и грубее |
| Цвета на кадр / всего | 30–36 / 37 | 29–45 / 54 |
| Линия опоры | Ровная, нижний пиксель на одной строке во всех кадрах | Ровная, в кадре 8 на 1 px выше |
| Цикл | Нет: последний кадр не переходит в первый | Нет: то же |

Описание движения для `animate_image` (точно): «running in place to the right, side view, looping run cycle: legs alternate in long strides, arms swing, dress and hair bounce, needle held in right hand pointing down and back». Для PixMiniMax — тот же текст без «side view»; заданы `view` = side, `direction` = east, `subject_description` = «a teenage girl in a blue Victorian dress and ivory apron holding a long sewing needle», `initial_pose` = «standing upright, needle lowered in right hand».

Вывод: главный риск — потеря персонажа между кадрами — подтвердился частично. Лицо, волосы, платье и рост держатся, а игла как отдельный предмет теряется у обоих аниматоров. Это совпадает с решением требований держать иглу отдельным слоем `needle` и дорисовывать её вручную. PixMiniMax даёт пригодную основу движения (кадры 1–5), но не замкнутый цикл и не единую палитру: после генерации нужны сведение к общей палитре и ручная сборка цикла. Старт со стойки даёт разгон, а не цикл; для цикла надо подавать кадр середины бега первым и последним.

## Проба цикла бега

`animate_image_pixminimax`, 8 кадров, 1 генерация. Первым и последним кадром подан `run-pixminimax/run-03.png` (середина шага, игла в руке). Job `c1f4aec3-515d-457d-ac83-79a3637bd710`. Те же `view`, `direction` и `subject_description`; `initial_pose` = «mid-run stride, front leg forward, needle in right hand pointing forward and down». Описание (точно): «one full seamless running cycle in place to the right: two strides, left leg then right leg, arms swing opposite to legs, dress and hair bounce, the sewing needle stays in the right hand pointing forward and down in every frame; ends in exactly the starting pose».

Файлы: `run-loop/` (кадры 0–8; кадр 8 повторяет кадр 0), `run-loop-strip-x6.png`, `run-loop-x4.gif` (кадры 0–7, по 100 мс, повтор без конца).

- Цикл замыкается: кадр 8 совпадает с кадром 0 по габариту 47×53 и положению; два полных шага, ноги чередуются.
- Линия опоры ровная во всех кадрах, нижний пиксель на одной строке.
- Цвета: 40–45 на кадр, 45 всего — больше 30, общая палитра нужна до чистки.
- Игла держится в кадрах 0–1 и 7–8. В кадре 4 она переходит в дальнюю руку, в кадрах 2–6 укорачивается или висит вдоль ноги — вручную дорисовывать.
- В кадрах 3–5 корпус поворачивается к камере на три четверти: передник уходит назад, виден воротник спереди. Для профиля в платформере это ошибка, правится вручную.
- Тайминг 100 мс на кадр взят для просмотра; настоящая длительность задаётся по скорости бега из данных (раздел 6 требований).

Вывод по циклу: за 1 генерацию получен замкнутый бег с узнаваемой Алисой и ровной опорой. Пригоден как основа движения; для приёмки нужны общая палитра, отдельный слой иглы и правка поворота корпуса в 3–4 кадрах.

## Конвейер без ручной дорисовки

Владелец не рисует (GDD 0.17), поэтому проверен путь, где каждый шаг — генерация или скрипт. Всё в `pipeline/`; скрипты PowerShell лежат рядом и повторяют результат. Стоимость 8,1 генерации.

1. **Стойка без иглы** — `create_image_pro_flash`, 64×64, 5 генераций; образец стиля — `flash-stand-01.png` целиком (палитра, контур, детали, тени). Job `4765e866-2b8e-4ad6-9dbf-073ce4810454`. Описание (точно): «Side-view 2D platformer player sprite, idle standing pose, strict right-facing profile, full body, the same girl as the style image: Alice, slender 15-year-old girl, shoulder-length honey-blonde wavy hair, dark charcoal ribbon bow, muted blue Victorian calf-length dress with puffed short sleeves and white collar, ivory apron with a bow at the back, grey stockings, brown lace-up boots. Both hands are EMPTY, holding nothing, arms relaxed at her sides, the near (right) hand slightly forward with a loose fist as if gripping a thin pole. No weapon, no needle, no stick. Feet together on the bottom of the canvas. Clean readable clusters, dark outline, high contrast between blue dress and ivory apron, no anti-aliasing.» Результат: `stand/stand-noneedle.png`, 30 цветов, та же Алиса.
2. **Скелет стойки** — `POST /v2/estimate-skeleton`, 0,1 генерации: `stand-skeleton.json`.
3. **Позы бега** — `runpose.ps1` строит 8 скелетов из длин костей стойки по таблице углов бедра и колена (касание, подсед, толчок, полёт, вынос), с наклоном корпуса 10° и встречными махами рук. Результат: `run-keypoints.json`, контроль — `run-skeleton-preview.png` (ближняя сторона оранжевая, дальняя голубая).
4. **Бег по скелету** — `animate_with_skeleton_v3`, `view` = side, `direction` = east, 8 кадров, 3 генерации. Job `ff7db834-a8ed-44ee-9a31-f112486cd665`. Описание внешности: «teenage girl Alice with shoulder-length honey-blonde wavy hair and a dark ribbon bow, muted blue Victorian calf-length dress with puffed short sleeves and white collar, ivory apron tied with a bow at the back, grey stockings, brown lace-up boots, empty hands». Кадры: `raw/`, лента — `raw-strip-x6.png`.
5. **Лицо** — у генератора глаз во всех кадрах вышел тёмной полосой. `facefix.ps1` находит смещение головы по совпадению с головой стойки и переносит со стойки лицо (окно 27–37 × 10–19). Результат: `facefixed/`.
6. **Чистка** — `cleanup.ps1`: сведение к палитре стойки (30 цветов) плюс 3 тона стали, линия опоры, перенос на холст 128×80 с пивотом внизу по центру, игла кодом — 1 px, 32 px длиной, с ушком, от ближайшего пикселя кожи у запястья ближней руки. Результат: `final/`, `final-strip-x4.png`, `run-final-x4.gif` (90 мс на кадр, только для просмотра).

| Проверка | Результат |
| --- | --- |
| Палитра | 30 цветов в каждом сыром кадре и во всём наборе — скелетный аниматор палитру не размывает; после чистки 33 (с тремя тонами стали) |
| Полупрозрачность | 0 пикселей |
| Профиль | Строгий во всех кадрах, разворота корпуса к камере нет |
| Облик | Одна и та же Алиса во всех кадрах; высота с волосами 50–53 px — бег ниже стойки |
| Холст и опора | 128×80, нижний пиксель ног на строке 79 во всех кадрах |
| Игла | Тонкая, видна целиком во всех кадрах, растёт из кисти ближней руки |
| Цикл | Позы заданы циклом; полёт (кадры 3 и 7) генератор поставил на землю — отрыв добавляет чистка |

**Доводка, тот же день, без новых генераций.** Наложение скелета на кадры показало, что генератор поставил руки так, как просили: в кадрах 0 и 7 ближняя рука отведена назад и кисть у бедра, вперёд выброшена дальняя. Игла там стоит верно. В `cleanup.ps1` добавлено:

- угол иглы следует за махом плеча ближней руки (`NeedleSwing` = 0,6 от угла плеча вокруг базовых 205°): от 183° при руке назад до 224° при руке вперёд; острие не опускается ниже строки 78;
- над светлым передником игла темнеет на одну-две ступени стали, иначе она пропадает на кадрах 3–4;
- кадры полёта 3 и 7 подняты на 2 px (`AirLift`), остальные стоят на строке 79.

Итоговые `final/`, `final-strip-x4.png` и `run-final-x4.gif` пересобраны; в наборе по-прежнему 33 цвета и нет полупрозрачных пикселей.

Вывод: связка «стойка Pro Flash → бег по скелету → скриптовая чистка» дала цикл бега, где ни один пиксель не поставлен рукой, с единой палитрой, строгим профилем и иглой отдельным слоем. Уровень — добротная инди-графика; приёмка этапа 0 — за владельцем.
