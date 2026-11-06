# Revision12 — свёрток для всех предметов

5 ноября 2026, сервис PixelLab. Все предметы в мире выглядят как свёрток с верёвочкой, который чуть поблёскивает (GDD 0.28).

| Что | Инструмент, цена | Job, seed | Итог |
| --- | --- | --- | --- |
| Свёрток, вариант A | `create_image_pro_flash` 32×32, 5; образец стиля — кусок кресла 32×32 | `f96e6d91-44c2-4a83-afc2-ee44e952cabd`, 10041 | **Взят:** ткань, перевязанная крест-накрест, бантик сверху, чёткий контур |
| Свёрток, вариант B | то же, 5 | `20adc9d1-9f19-4fc6-ac0f-12f67e80d203`, 10042 | Отброшен: мутный, верёвка не читается |
| Блеск, первый и последний кадр — вариант A | `animate_image_pixminimax`, 8 кадров, 1 | `eff909d1-d7db-449e-888f-45dcf6989a29`, 10043 | С первого раза: искорка проходит по верёвке и вспыхивает четырёхлучевой звёздочкой над узлом; сам свёрток не меняется |

Всего 11 генераций (баланс 285 → 296 из 2000).

Запрос свёртка: «A small wrapped bundle for a dark 18+ fairy-tale 2D platformer, seen from the side, lying on the floor: a parcel of worn greyish-ivory cloth folded around something, tied crosswise with a thin rough brown string knotted on top with a small bow, slightly stained and frayed. Only the bundle is drawn, nothing else. About 20 pixels wide and 14 pixels tall, bottom touching the bottom of the image, centred. Transparent background, no floor, no shadow.»

Запрос блеска: «The bundle lies still. A tiny soft glint of light slides once across its top along the string to the knot and twinkles briefly as a small four-pointed sparkle, then fades. The bundle itself does not move or change shape at all.»

`bundle-frames.ps1`: кадры 0–7 (8-й равен 0), жёсткая альфа, общая обрезка по всем кадрам вместе со звёздочкой — свёрток не сдвигается. Результат — `Assets/_Alice/Art/World/Items/item_bundle_00..07`. В игре после 8 кадров блеска свёрток 14 кадров лежит спокойно (по 0,12 с), чтобы блеск был намёком, а не мигалкой.

## Маленький свёрток (6 ноября 2026)

Первый свёрток (22×14 px) оказался слишком большим для анимации подбора (Revision13): у груди он закрывал лицо. Новый — `create_image_pro_flash` 24×24, образец стиля — первый свёрток, вырезанный под 24×24 (`bundle-crop24.png`), запрос «It is small, about 15 pixels wide and 11 pixels tall…»: вариант `23100a47-dae5-4cbb-b3c9-6fad81093517` (seed 10061) взят, `de17473f-111d-4da4-ae95-e3e30be46dbe` (10062) отброшен — пустой контур. Блеск — `252e7970-5e85-43cd-88f4-8d56609dea1a` (10063), 1 генерация. `bundle-frames.ps1 -Source raw\glint-small` заменил кадры `item_bundle_00..07` с теми же именами (GUID сохранены); итог 18×20. Ещё 11 генераций.

Проверено в Play (первый свёрток): свёрток в комнате B лежит на полу и поблёскивает; Interact открывает его, веер получен, посередине экрана значок и «White Rabbit's Fan»; на русском — «Засахаренная конфетка» шрифтом Fusion Pixel, кириллица чёткая.
