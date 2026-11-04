# Ревизия 04: проба 18+ сцен, врагов, босса, тайлсета и фона в PixelLab

3 ноября 2026, пробный тариф, по просьбе владельца. Цель — до подписки проверить, рисует ли PixelLab кровь и казни, которые GDD требует показывать прямо (рейтинг 18+). 6 генераций.

## Казнь в стиле Алисы

`create_image_pro_flash`, 96×64, 5 генераций. Образец стиля — `../Revision03/flash-stand-01.png` («только стиль: контур, палитра, детализация, тени; эту девочку не рисовать»). Job `ddecb216-8717-48b8-a1d6-a74539752ca5`. Описание (точно): «Side-view 2D platformer scene for a dark, brutal 18+ game, same pixel art style as the style image. Two playing-card soldiers from Alice in Wonderland: flat rectangular card bodies with a red heart suit printed on the chest, thin dark arms and legs, small card-shaped heads. On the left a card executioner stands calmly holding a long bloody axe. On the right a kneeling prisoner card soldier has just been beheaded: the headless body slumps forward, the severed head lies on the floor, thick dark red blood sprays from the neck and spreads into a large pool on the ground. Strict side view, full bodies standing on the bottom edge, dark outline, muted desaturated colours with saturated red blood, no background.»

Результат: `gore-execution-flash.png`, увеличение — `gore-execution-flash-x8.png`.

- Отказа нет, запрос оплачен и выполнен. Обезглавленное тело карточного солдата с червой на груди, фонтан крови из шеи, отрубленная голова, лужа крови — показано прямо, как требует GDD.
- Стиль близок к Алисе: тёмный контур, приглушённые цвета, насыщенная кровь.
- Палача генератор не нарисовал: из двух фигур осталась одна. Сцены из нескольких участников надо собирать из отдельных спрайтов, а не просить одной картинкой.

## Дешёвые объекты без образца стиля

`create_image_pixflux`, 128×80, 1 генерация, без образца стиля. Job `a767d6fd-bc4c-4024-9169-670b059d2e50`. Описание: головы на пиках, повешенный карточный солдат, кровь на земле.

Результат: `gore-props-pixflux.png`, `gore-props-pixflux-x6.png`. Запрос принят, но смягчён: вместо голов — мультяшные черепа, вместо повешенного тела — карта на верёвке; палитра и стиль чужие. Для игровых ассетов не годится.

## Выводы

1. Кровь, обезглавливание и трупы PixelLab рисует без отказов. По справке сервиса насилие в контексте взрослых игр разрешено, а отклонённые фильтром запросы не оплачиваются.
2. Для нашего стиля нужен Pro Flash с образцом стиля Алисы: 5 генераций за изображение до 96 px, 6 — до 208 px.
3. Многофигурные сцены — из отдельных спрайтов персонажей и объектов, кровь — отдельными спрайтами и анимациями.

## Остальные проверки на подписке

3 ноября 2026, тариф Pixel Apprentice, 38 генераций. Образец стиля во всех Pro Flash — тот же `flash-stand-01.png` (upload `aac556ce-87cb-4a29-ba4c-13ad661c559d`, хранится 3 дня). Сравнение масштаба с Алисой — `review-scale-x4.png`.

| Что | Инструмент, цена | Job / ID | Файлы | Итог |
| --- | --- | --- | --- | --- |
| Карточный солдат с копьём | `create_image_pro_flash` 64×64, 5 | `2e9783fc-aeb3-479b-aeb4-61275b39ce3c` | `enemy-card-soldier.png` | Стиль и масштаб совпадают с Алисой, сразу пригоден как основа врага |
| Его выпад копьём | `animate_image_pixminimax`, 8 кадров, 1 | `067417d4-ff18-4e4b-843d-5e4b0b44efbe` | `enemy-attack/`, `enemy-attack-strip-x4.png` | Читаемый замах, выпад и возврат; наконечник копья слегка меняет форму |
| Его смерть с кровью | `animate_image_pixminimax`, 8 кадров, 1 | `81032c81-863b-4fa4-b834-ac8eb262945f` | `enemy-death/`, `enemy-death-strip-x4.png` | Разрез, фонтан крови, падение копья, тело в луже — без отказа и смягчения |
| Босс: Кухарка с тесаком и перечницей | `create_image_pro_flash` 128×128, 6 | `5a3fcdd6-fef8-4237-a27e-682f6f9e9397` | `boss-cook.png` | Около 120 px, вдвое выше Алисы; детальнее и реалистичнее Алисы, но в той же палитре |
| Тайлсет камня, стандарт | `create_sidescroller_tileset` 16 px, 1 | `40cacb44-42c0-4976-9115-ab13d3700210` | `tileset-standard.png`, `.json` | 16 wang-плиток с картой соединений; мох и кровь сверху, детализация средняя |
| Тайлсет камня, pro | `create_sidescroller_tileset` pro 16 px, 15 | `5750d8d4-0c12-450c-a52c-0c447fdd9fdd` | `tileset-pro.png`, `.json` | Богаче текстура кладки, но мох потерян и кромка с оранжевыми бликами |
| Фон: кухня Герцогини | `create_image_pro_flash` 256×144, 9 | `ef4ac9f1-d086-4bf2-8565-9db860eb0024` | `background-kitchen.png`, `-x4.png` | Сильное настроение: очаг, туши на крюках, кровь на столах; по краю слева и снизу светлая рамка в 1 px |

Описания запросов (точно):

- Солдат: «Side view enemy sprite for a dark brutal 18+ 2D platformer, same pixel art style as the style image. A playing-card soldier from Alice in Wonderland: flat rectangular card body, white card with a red heart suit printed on the chest and a worn bloodstained edge, thin dark arms and legs, a small grim face, holding a long spear with a heart-shaped blade. Standing guard pose facing right, full body standing on the bottom edge, dark outline, muted desaturated colours with saturated red accents, no background.»
- Выпад: «quickly draws the spear back, then lunges forward and thrusts the spear straight ahead to the right, then pulls it back to the guard pose».
- Смерть: «is slashed across the body, dark red blood sprays out of a deep cut through the card, it drops the spear, staggers back and collapses onto the ground, lying still in a growing pool of blood».
- Босс: «Side view boss sprite for a dark brutal 18+ 2D platformer, same pixel art style as the style image. The Duchess's Cook from Alice in Wonderland as a huge grotesque boss: a massive stout woman in a filthy apron spattered with blood, sleeves rolled up, holding a heavy cleaver in one hand and a pepper grinder in the other, cruel face. Facing left, full body standing on the bottom edge, about twice the height of the girl in the style image, dark outline, muted desaturated colours with saturated red blood, no background.»
- Тайлсет: платформа «dark damp stone brick, cracked, muted grey-brown», верх «dead moss and dried blood stains», transition 0.25, highly detailed, detailed shading.
- Фон: «Background art for a dark brutal 2D side-scrolling platformer, same pixel art style as the style image. The Duchess's kitchen in a twisted Alice in Wonderland: a huge gloomy stone kitchen, iron pots over a fire, hanging meat hooks, butcher tables with blood, pepper smoke haze, dim warm firelight against cold shadows. Flat side view, no characters, muted desaturated colours, full scene with background.»

Листы собраны скриптом System.Drawing без изменения пикселей (увеличение ближайшим соседом).

### Сочная кровь в смерти солдата

Первый вариант смерти даёт одноцветную буро-красную кровь (`8C3027` и пара соседних тонов), заметно беднее казни. Ещё два прогона (`animate_image_pixminimax`, 8 кадров, по 1 генерации, seed 1 и 2), описание точно: «is slashed deep across the body; thick glossy blood bursts out in heavy spurts, many shades from black-red shadows to dark crimson to bright red highlights, droplets and clots fly and splatter; blood runs down the card, it drops the spear, staggers and collapses, lying still as a shaded dark pool of blood spreads under it».

- `enemy-death2/` — job `4dc12a44-68fc-4bf7-b626-c0d43dd11f61`, `enemy-death3/` — job `6dacc107-5d75-41f7-9156-03c2520eb8e3`. Кровь стала объёмной, с бликами, каплями и сгустками, но чистого неонового красного (`FE0402`, `EF0200`).
- `blood-palette.ps1` переводит такие пиксели (R ≥ 0x60, G и B ≤ 0x22) по яркости в шесть тонов крови из `gore-execution-flash.png` (`A01511` … `570201`), а белые блики внутри брызг — в `CC6351`. Собственные красные солдата (капюшон, черва, конечности) содержат заметные G и B и не меняются.
- Результат: `enemy-death2-blood/`, `enemy-death3-blood/` и полосы `-strip-x4.png`. Лучший — `death3`: дуга брызг, разворот при падении, лужа под телом.

Владелец оценил тона `death3`, но крови оказалось слишком много — по количеству лучше первый вариант. Поэтому ещё три прогона (seed 3, 4, 5, по 1 генерации) с умеренным описанием, точно: «is slashed across the body, a short spurt of glossy blood sprays from a deep cut through the card, shaded from black-red to crimson with small bright highlights, a few drops fall; it drops the spear, staggers back and collapses onto the ground, lying still in a small pool of blood». Затем `blood-palette.ps1`.

- `enemy-death4/` — job `6e0c729b-bb8b-4109-b4e0-a976420a569e`: падает вперёд, копьё остаётся в руке.
- `enemy-death5/` — job `54f3b3c4-e5b0-42bf-8208-d53db74b5b1f`: короткий выплеск, падение назад.
- `enemy-death6/` — job `801c730e-2c51-41c6-8a57-79afa4c5001b`: густой выплеск из разреза, капли, копьё падает отдельно, тело ложится назад в небольшую лужу. Ближе всего к первому варианту по количеству. Сначала владелец выбрал его как смерть карточного солдата, но в игре, в движении, крови оказалось мало. **Итоговый выбор владельца 3 ноября 2026 — `death3`**: в покадровом просмотре крови казалось много, а в игре он смотрится лучше. В игре используются кадры `enemy-death3-blood/1..8.png` (0 — входной кадр стойки); `soldier-frames.ps1` берёт их по умолчанию, другой вариант — параметр `-Death`.

Вывод: насыщенность и тона крови в анимациях держит скрипт, поэтому запрос описывает объём и брызги, а цвет приводится к единой палитре крови.

### Солдат в прототипе M2

Шаг солдата: `animate_image_pixminimax`, 8 кадров, 1 генерация, job `61b33f74-ef4e-4d3b-b1f7-7499a6d0b7bd`, последний кадр закреплён на стойке, чтобы цикл замыкался. Описание точно: «marching forward in place with stiff steps, the spear held upright beside the body, a seamless walk cycle». Кадры — `enemy-walk/`, полоса — `enemy-walk-strip-x4.png`.

`soldier-frames.ps1` переносит в `Assets/_Alice/Art/Characters/Enemies/CardSoldier/` стойку (`enemy-attack/0`), шаг (`enemy-walk/1..8`), выпад (`enemy-attack/1..8`) и смерть (`enemy-death3-blood/1..8`). В исходниках 64×64 тело стоит не по центру (около x 24), ноги на строке 60, а копьё и труп доходят до правого края. Скрипт сдвигает всё на (+16, +1) на холст 80×64: тело оказывается на опорной точке спрайта, а под ногами остаются две строки для лужи крови, на которые набор кадров опускает рисунок. Полупрозрачные пиксели отбрасываются. Подключение в игре описано в `Docs/TechnicalSetup.md`, раздел «Боевой прототип M2».

## Итог проверки

1. Враги, их атаки и кровавые смерти — сильная сторона: 5 генераций на облик и по 1 на каждую анимацию из 8 кадров. Смерть с кровью дешевле и проще получается текстовой анимацией, чем по скелету.
2. Боссы получаются выразительными; к общему стилю их, возможно, придётся подтягивать общей палитрой в скрипте чистки, как у Алисы.
3. Тайлсеты — слабое место: пригодны для прототипа, до уровня фона и персонажей не дотягивают. Pro за 15 генераций не лучше стандарта за 1. Плитки, вероятно, лучше делать через Pro Flash по образцу фона и нарезать скриптом.
4. Фон 256×144 — потолок одного изображения, а экран игры 640×360. Полноэкранные фоны придётся собирать из нескольких частей или слоёв параллакса; простое увеличение даст пиксели крупнее, чем у персонажей.
5. Повторяемые находки для конвейера: светлую рамку по краю фона и пляшущую форму оружия между кадрами снимает скрипт чистки.

## Солдат: реакция на удар и смерти по типу удара (GDD 0.18)

3 ноября 2026, `animate_image_pixminimax` от стойки солдата, всего 4 генерации, кровь — `blood-palette.ps1`, перенос в игру — `soldier-frames.ps1`.

| Что | Job | Кадры в игре | Описание (точно) |
| --- | --- | --- | --- |
| Вздрагивание, 4 кадра, последний закреплён на стойке | `822b9cf8-decc-4310-9798-7d1a0be3a437` | `card_soldier_hurt_00..03` (кадры 1–4) | «is hit in the chest and flinches sharply backward, head snapping back, staggers half a step, then recovers into the same guard pose holding the spear upright» |
| Смерть от укола, 12 кадров, 2 генерации | `d25c9f62-cce2-4ce4-b163-4865c7e76d74` | `card_soldier_stab_00..09` (кадры 3–12) | «is stabbed through the chest by a thin needle, leaving only a small hole; a thin stream of glossy dark blood trickles from the wound down the card; it clutches the wound, drops the spear, sinks to its knees, writhes and twitches in agony, then slumps onto the ground and lies still while a small pool of blood slowly spreads» |
| Обезглавливание, 8 кадров | `9876ddaa-4916-40df-a95c-572ea246c776` | `card_soldier_behead_00..06` (кадры 2–8) | «is beheaded by a fast spinning blade: the head flies off backward and rolls on the ground, a fountain of glossy dark blood sprays from the neck, the headless body stands for a moment, drops the spear, then collapses in a pool of blood» |

В первые два кадра смерти от укола генератор сам дорисовал иглу в теле, в первый кадр обезглавливания — белый росчерк клинка; эти кадры не используются. Рассечение — выбранный ранее `death3`, он же смерть по умолчанию. Полосы: `enemy-hurt-strip-x4.png`, `enemy-stab-death-strip-x4.png`, `enemy-behead-strip-x4.png`.

В игре смерть от укола проверена через меню `Alice/Automation`: третий укол убивает солдата, он оседает и корчится около 1,3 с. Рассечение и обезглавливание ждут взмаха и удара с разворота у Алисы.

## Солдат: открытие после парирования и казнь

3 ноября 2026, `animate_image_pixminimax`, 3 генерации, кровь — `blood-palette.ps1`, перенос — `soldier-frames.ps1`.

| Что | Job | Кадры в игре | Описание (точно) |
| --- | --- | --- | --- |
| Открыт парированием, 8 кадров, последний закреплён на стойке | `4ad87604-ee38-43eb-87b3-f36a2b9af2f8` | `card_soldier_opened_00..06` (кадры 2–8) | «its spear is knocked aside by a parry: it reels backward off balance with the spear swung wide away from the body, arms flailing, chest left open, staggers dazed for a moment, then slowly regains the guard pose» |
| Казнь контратакой, 12 кадров, 2 генерации | `edec2d88-b446-41e1-9083-321bf9b0959c` | `card_soldier_execution_00..09` (кадры 3–12) | «is executed with a needle driven deep into its heart from the front: it jerks upright, the spear falls from its hands, it convulses with the wound in its chest, glossy dark blood gushes out of the heart and runs down the card, then the blow is pulled out with a spurt of blood and it falls forward onto its face, dead in a spreading pool of blood» |

В первом кадре открытия генератор нарисовал белую дугу удара, в первых двух кадрах казни — летящую иглу; эти кадры не используются. С третьего кадра казни игла сидит в сердце, что совпадает с удержанной иглой Алисы в контратаке. Проверено в игре через `Alice/Automation`: парирование выпада, открытие, контратака и казнь.

## Солдат: казнь «разорвать карту»

3 ноября 2026, `animate_image_pixminimax`, 12 кадров, 2 генерации, job `ab777e4b-407c-4a63-98d6-86289f0a9573`. Описание (точно): «is executed: its flat card body is seized by the top edge and pulled forward, a thin needle stabs through the heart in the middle of the card, then the card body is ripped in half from top to bottom, the two torn paper halves split apart with glossy dark blood gushing from the tear, the spear drops, and the torn soldier collapses in two halves into a pool of blood». Кровь — `blood-palette.ps1`, полоса — `enemy-tear-strip-x4.png`.

В игре `card_soldier_tear_00..14`: три кадра стойки, затем кадры 1–12, всё растянуто на 1,3 с казни Алисы, чтобы игла входила в черву вместе с её ударом, а карта рвалась, когда она разводит руки. Проверено в арене `Prototype_Combat` через `Alice/Automation`.
