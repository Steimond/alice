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
- `enemy-death6/` — job `801c730e-2c51-41c6-8a57-79afa4c5001b`: густой выплеск из разреза, капли, копьё падает отдельно, тело ложится назад в небольшую лужу. Ближе всего к первому варианту по количеству. **Выбран владельцем 3 ноября 2026** как смерть карточного солдата: кадры `enemy-death6-blood/1..8.png` (0 — входной кадр стойки).

Вывод: насыщенность и тона крови в анимациях держит скрипт, поэтому запрос описывает объём и брызги, а цвет приводится к единой палитре крови.

### Солдат в прототипе M2

Шаг солдата: `animate_image_pixminimax`, 8 кадров, 1 генерация, job `61b33f74-ef4e-4d3b-b1f7-7499a6d0b7bd`, последний кадр закреплён на стойке, чтобы цикл замыкался. Описание точно: «marching forward in place with stiff steps, the spear held upright beside the body, a seamless walk cycle». Кадры — `enemy-walk/`, полоса — `enemy-walk-strip-x4.png`.

`soldier-frames.ps1` переносит в `Assets/_Alice/Art/Characters/Enemies/CardSoldier/` стойку (`enemy-attack/0`), шаг (`enemy-walk/1..8`), выпад (`enemy-attack/1..8`) и смерть (`enemy-death6-blood/1..8`). В исходниках 64×64 тело стоит не по центру (около x 24), ноги на строке 60, а копьё и труп доходят до правого края. Скрипт сдвигает всё на (+16, +1) на холст 80×64: тело оказывается на опорной точке спрайта, а под ногами остаются две строки для лужи крови, на которые набор кадров опускает рисунок. Полупрозрачные пиксели отбрасываются. Подключение в игре описано в `Docs/TechnicalSetup.md`, раздел «Боевой прототип M2».

## Итог проверки

1. Враги, их атаки и кровавые смерти — сильная сторона: 5 генераций на облик и по 1 на каждую анимацию из 8 кадров. Смерть с кровью дешевле и проще получается текстовой анимацией, чем по скелету.
2. Боссы получаются выразительными; к общему стилю их, возможно, придётся подтягивать общей палитрой в скрипте чистки, как у Алисы.
3. Тайлсеты — слабое место: пригодны для прототипа, до уровня фона и персонажей не дотягивают. Pro за 15 генераций не лучше стандарта за 1. Плитки, вероятно, лучше делать через Pro Flash по образцу фона и нарезать скриптом.
4. Фон 256×144 — потолок одного изображения, а экран игры 640×360. Полноэкранные фоны придётся собирать из нескольких частей или слоёв параллакса; простое увеличение даст пиксели крупнее, чем у персонажей.
5. Повторяемые находки для конвейера: светлую рамку по краю фона и пляшущую форму оружия между кадрами снимает скрипт чистки.
