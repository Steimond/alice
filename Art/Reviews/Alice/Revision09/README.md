# Revision09 — маленькая Алиса

5 ноября 2026, по ходу M3, сервис PixelLab. Кадры уменьшенной грибом Алисы вместо её спрайта в половинном масштабе (GDD 12: рисунки персонажей не уменьшают).

## Стойка

| Попытка | Инструмент, цена | Job, seed | Итог |
| --- | --- | --- | --- |
| Стойка 32×32, образец — `style-crop32.png` (голова и корпус большой стойки) | `create_image_pro_flash`, 5 | `d6c302fe-2c7a-45be-b1ad-31e5b5a92739`, 10001 | **Отброшена:** волосы рыжие, шумная |
| То же, другой seed | `create_image_pro_flash`, 5 | `04006e72-f1ee-4894-a4d0-6e21bb4838db`, 10002 | Хороша, но 32 px — выше маленькой двери (29 px). Взята образцом для следующих |
| Перерисовка уменьшенной скриптом стойки (`stand-half32.png`) | `edit_image_pro_flash`, 5 | `3f49ae3f-154a-421b-ac13-ec6a86e040f0`, 10003 | **Отброшена:** осталась мутной, как вход |
| Стойка 28×28, образец — верх попытки 10002 | `create_image_pro_flash`, 5 | `94b4cd52-49a4-4600-bae8-148bc4ddfd30`, 10002 | Форма хорошая, волосы жёлтые. После `palette-map.ps1` к палитре большой стойки — **взята** (`stand-28-alice.png`, 27 px) |
| Стойка 24×24 | `create_image_pro_flash`, 5 | `147a9dc9-6c14-4b74-a8f0-a737cc4cdacd`, 10002 | **Отброшена:** фигура развалилась |

Запрос стойки: «A tiny shrunken girl Alice for a dark 18+ fairy-tale 2D platformer, seen exactly from the side, standing in profile facing right, arms relaxed: honey-blonde wavy hair with a dark bow, muted blue Victorian dress with an ivory apron, grey stockings, brown boots, empty hands. Full body, she fills the full height of the image, feet touching the bottom of the image, centred. Transparent background, no floor, no shadow.»

## Анимации

`animate_image_pixminimax`, view side, direction east, `subject_description`: «a tiny shrunken girl Alice in a muted blue Victorian dress with an ivory apron, honey-blonde wavy hair with a dark bow, brown boots, empty hands». Вход — стойка на холсте 48×48 (`small-stand-48.png`).

| Что | Job, seed, цена | Кадры в игру | Итог |
| --- | --- | --- | --- |
| Бег от стойки | `7e11217f-415c-42d4-972d-14b9f1a4b784`, 10011, 1 | — | Разгон, а не цикл; кадр 4 (середина шага) взят входом для цикла |
| Цикл бега, первый и последний кадр — кадр 4 | `d7dc389c-dd9a-42ce-ab85-edff16adbd87`, 10014, 1 | 1–8 → `small_run_00..07` | Замкнутый цикл |
| Дыхание, стойка в начале и конце | `17f18c8f-6138-480b-b5f9-fdae86990dd1`, 10012, 1 | 1–8 → `small_idle_00..07` | Тихое дыхание |
| Прыжок на месте, стойка в начале и конце | `316bcb01-6e2d-46fe-887c-6b2524a7fbb5`, 10013, 1 | 2–3 подъём, 4–5 верх, 6 падение, 7 приземление | Генератор дорисовал тонкий штрих вроде иглы и крошки — убраны скриптом |
| Уменьшение: первый кадр — большая стойка без иглы, последний — маленькая на том же холсте 64×64 | `d920f0f9-f4b7-41b8-9001-2e9c1309396b`, 10015, 1 | 1–8 → `shrink_00..07` | С первого раза: вжимает плечи и плавно сжимается, ноги на месте |

Запрос уменьшения: «She shrinks: her whole body quickly gets smaller and smaller in place, feet staying on the same spot on the ground, she flinches and hunches her shoulders in surprise, skirt and hair shrinking with her, until she is a tiny girl half her height standing in profile. Side view facing right, empty hands, nothing else is drawn, no smoke, no effects.»

Всего 30 генераций (баланс до 238 из 2000, после — 268).

## Обработка

`small-frames.ps1`: палитра большой стойки, жёсткая альфа, остаётся только самая большая связная часть (штрихи и крошки уходят), холст 128×80 с ногами на нижней строке и тем же центром, что у полноразмерных кадров. Кадры в воздухе тоже стоят на нижней строке: высоту даёт физика. Результат — `Assets/_Alice/Art/Characters/Alice/Trial/alice_trial_small_*` и `alice_trial_shrink_*`. `final-strip.png` — все кадры подряд.

Иглы у маленькой Алисы нет: действия ей недоступны по сюжету (GDD 5: «без полноценного боя»), а удары, если их нажать, пока рисуются полноразмерными кадрами в половинном масштабе.

## Проверка в Play (комната M3_D)

- Переход уменьшения в замедлении — плавный, ноги на полу.
- Стойка и дыхание — маленькая Алиса рядом с грибом ниже дверцы.
- Подброшенная в воздух — кадры подъёма и верхней точки.
- Бег по нажатой D — кадры цикла.
- Ошибок в консоли нет; EditMode 22/22, PlayMode 31/31.
