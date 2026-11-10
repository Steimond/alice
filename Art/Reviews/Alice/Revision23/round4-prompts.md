# Четвёртый круг: запросы генераций

Все вызовы — `animate_image_pixminimax`, 10 ноября 2026, по подписке. Общие поля:

- `first_frame_url` = `last_frame_url` = `upload:2b9b5563-8f03-4558-8745-3dc20cb69e13` — `Revision03/pipeline/stand/stand-noneedle.png` (64 × 64, загрузка живёт 3 дня, до 13 ноября 2026);
- `view` = side, `direction` = east;
- `subject_description`: «a teenage girl Alice in a muted blue Victorian dress with an ivory apron, honey-blonde wavy hair with a dark bow, brown boots, empty hands»;
- `initial_pose`: «standing in profile facing right, arms relaxed»;
- 12 кадров (завершающий и казнь — 16), 2 генерации на вызов. Баланс до круга — 676, после — 716.

Сырые кадры — `alice/<набор>-raw/00..NN.png` (кадр 00 — входной).

## Первая формулировка — «поворот корпуса» (отброшена вся, кроме казни)

Генератор на «back / chest toward the viewer» разворачивает Алису целиком спиной или лицом к камере; меч тогда читается в другой руке.

**Удар 1** — `slashA` seed 2411, job `5cf61ff8-f842-4996-8a58-048930265505`; `slashB` seed 2412, job `2dbf928d-0b6e-4de6-ab01-1de438195350`:

> a heavy diagonal forehand slash that sweeps around her body in depth, not a flat up-and-down chop. Anticipation: she twists her shoulders away so her back half-turns toward the viewer, the near arm (closer to the viewer) lifts the fist high above and behind her near shoulder, elbow up, and holds. Drive: she steps forward on the front foot, hips then shoulders unwind toward the viewer, the near arm sweeps diagonally down from high behind her, across the front of her chest and belly. Follow-through: the fist ends low in front of her far hip, the near arm wrapped across her waist, chest turned toward the viewer, she holds a beat. Recovery: she straightens back into her standing pose. She keeps facing right, the far arm stays by her side. Empty hands, no weapon.

Итог: оба — спиной к камере в замахе, лицом в доводке.

**Удар 2** — `cutA` seed 2421, job `98bcd942-7ba7-4c65-a33c-5078cd059024`; `cutB` seed 2422, job `b62d7387-16f3-4804-bc78-cc1fde12d639`:

> a heavy rising backhand cut that sweeps around her body in depth, the second blow of a combo. Anticipation: the near arm (closer to the viewer) crosses in front of her body, the fist drops low beside her far hip, shoulders turned toward the viewer, knees bent, she holds. Drive: she pushes up off the back foot and unwinds, the back of the near hand leading, the fist sweeps out in front of her and up in a wide rising diagonal arc. Follow-through: the fist ends high above and behind her near shoulder, elbow high, her back half-turned toward the viewer, up on her toes, she holds a beat. Recovery: she lowers the arm and settles back into her standing pose. She keeps facing right, the far arm stays by her side. Empty hands, no weapon.

Итог: оба — в конце спиной к камере.

**Завершающий** — `finA` seed 2431, job `c21ad14f-f6e6-482a-be57-5f9152da1a7f`; `finB` seed 2432, job `91d79361-7f4f-403f-80dc-d8894fd7633a`, 16 кадров:

> the slow, crushing finishing blow of a combo: a delayed horizontal sweep all the way around her body at chest height. Anticipation: she twists her torso far away from the right, her back turned toward the viewer, the near arm (closer to the viewer) drawn far back behind her at chest height, fist behind her back; she holds this coiled pose for a long beat. Drive: she explodes, a deep lunge step forward, hips then shoulders whip around toward the viewer, the near arm swings flat at chest height out from behind her, across in front of her toward the viewer and forward, fully extended to the right. Follow-through: the arm keeps going round and wraps across her chest, fist by her far shoulder, crouched, weight forward, she holds a moment. Recovery: she straightens back into her standing pose. She keeps facing right. Empty hands, no weapon.

Итог: настоящий поворот на 360° (рука одна и та же, но спиной, потом лицом к камере). `finA` — самый «объёмный» взмах из всех, но правило «профиль, ближняя рука» он нарушает; оставлен как образец для обсуждения.

**Казнь утки** — `duckA` seed 2441, job `2af99b1b-49ea-4313-9ef7-67f9b35ce4b6`; `duckB` seed 2442, job `1cbf3791-fe37-4901-86fb-43d94e594960`, 16 кадров:

> a brutal two-part finishing move on a tall enemy in front of her, she stays facing right in profile the whole time. Wind-up: she draws the near fist (closer to the viewer) up beside her face, elbow back, and holds. Drive: she lunges forward on the front foot and punches the near fist straight forward at the height of her own head, arm fully extended, and holds it there for several frames, leaning in. Wrench: she plants her feet, leans her whole body back and tears the near fist back and up in a big arc over her near shoulder, elbow high, chest still facing right, never turning around. Recovery: she lowers the arm and straightens back into her standing pose. Only the girl is drawn, nothing in her hands, no weapon, no enemy.

Итог: **`duckA` взят** (замах на три четверти к камере, рывок в профиль); `duckB` в рывке отворачивается спиной.

## Вторая формулировка — «строгий профиль»

Общая вставка: «Her face, (chest,) hips and feet stay in strict side profile facing right in every frame; she never turns her face or her back toward the viewer, only her shoulders rotate (a little).»

**Удар 1** — `slashC` seed 2413, job `a33a4243-da9f-4ffa-949c-8a2298ec04aa`; `slashD` seed 2414, job `4e4b017c-3563-4c62-8f08-a861196e99e7`:

> a heavy diagonal forehand slash. Her face, hips and feet stay in strict side profile facing right in every frame; she never turns her face or her back toward the viewer, only her shoulders rotate a little. Anticipation: the near arm (closer to the viewer) lifts the fist high above and slightly behind her head, elbow up, she leans back, and holds. Drive: she steps forward on the front foot and the near arm sweeps diagonally down and forward in front of her body. Follow-through: the arm keeps going across in front of her belly, the fist ends low beside her back hip with the forearm across her waist, knees bent, she holds a beat. Recovery: she straightens back into her standing pose. The far arm stays by her side. Empty hands, no weapon.

Итог: `slashC` — доводка лицом к камере, отброшен; **`slashD` взят** — профиль во всех кадрах, но доводка вперёд-вниз, а не через пояс.

**Удар 2** — `cutC` seed 2423, job `6185e14e-a306-4efd-bdaf-856f1edad7de`; `cutD` seed 2424, job `e6e7366b-2c12-41c8-81cc-9373c75ea862`:

> a heavy rising backhand cut. Her face, hips and feet stay in strict side profile facing right in every frame; she never turns her face or her back toward the viewer, only her shoulders rotate a little. Anticipation: the near arm (closer to the viewer) crosses in front of her body, the fist low beside her back hip, forearm across her waist, knees bent, and holds. Drive: she pushes up off the back foot, the back of the near hand leading, the fist sweeps forward and up in a wide rising arc in front of her. Follow-through: the fist ends high above and behind her head, elbow high, up on her toes, she holds a beat. Recovery: she lowers the arm and settles back into her standing pose. The far arm stays by her side. Empty hands, no weapon.

Итог: оба — в конце спиной к камере («behind her head» перевешивает запрет).

`cutE` seed 2425, job `5dafa707-3eab-460a-86f3-af37d17e5c72`; `cutF` seed 2426, job `24f37b17-e317-4c09-9eaa-42ac36039d10` — тот же текст с «face, chest, hips and feet» и концом:

> Follow-through: the arm ends raised high in front of her, fist above her forehead, arm reaching up and forward to the right, up on her toes, she holds a beat.

Итог: `cutE` — спиной к камере; **`cutF` взят** — конец на три четверти к камере, кулак над головой; кадр `run-04` (дуга цвета тела) выброшен.

**Завершающий** — `finC` seed 2433, job `cb592db6-734d-494e-a24c-226dcc4d6a46`; `finD` seed 2434, job `8f64a3ea-ae59-4a62-9a4d-b64206c8835a`, 16 кадров:

> the slow, crushing finishing blow of a combo, a delayed flat sweep at chest height. Her face, hips and feet stay in strict side profile facing right in every frame; she never turns her face or her back toward the viewer, only her shoulders rotate. Anticipation: she pulls the near arm (closer to the viewer) far back behind her body at chest height, the fist hidden behind her back, shoulders wound back, knees bent; she holds this coiled pose for a long beat. Drive: she explodes into a deep lunge step forward and the near arm swings flat at chest height out from behind her, forward, fully extended to the right. Follow-through: the arm keeps going and wraps across in front of her chest, fist by her back shoulder, crouched low, weight forward, holds a moment. Recovery: she straightens back into her standing pose. Empty hands, no weapon.

Итог: **`finC` взят** — в оттяжке лицо на три четверти к камере, кулак у бедра сзади, затем удар вперёд (не «через грудь»); кадр `run-08` (белая полоса рывка) выброшен. `finD` скачан, не отсмотрен в листе по кадрам.

**Удар в воздухе** — `airA` seed 2451, job `d6e22474-65f9-4297-be59-6e3559cc1988`; `airB` seed 2452, job `f7c94d0d-184f-42a8-9841-8200ae3ba509`:

> a slash in mid-air during a jump. Her face, chest and hips stay in strict side profile facing right in every frame; she never turns her face or her back toward the viewer. She springs up off the ground, knees drawn up, skirt flaring. Anticipation: in the air she pulls the near arm (closer to the viewer) far back behind her body at chest height, fist hidden behind her back, and holds. Drive: the near arm swings flat and fast out from behind her, forward to full extension at chest height, her body leaning into it. Follow-through: the arm carries on across in front of her chest, fist by her back shoulder. Recovery: she lands softly and straightens back into her standing pose. Empty hands, no weapon.

Итог: `airA` — в замахе на три четверти к камере; **`airB` взят** — профиль, рука за спиной, удар вперёд; `run-05` (полоса) выброшен.

**Контратака** — `counterA` seed 2461, job `79e08a20-8c2f-4442-bb6f-27f6ded6030a`; `counterB` seed 2462, job `5d4d9562-8a3c-424e-a50d-052030c79e1c`:

> a fast flat backhand counter-slash with a lunge. Her face, chest, hips and feet stay in strict side profile facing right in every frame; she never turns her face or her back toward the viewer. Anticipation: the near arm (closer to the viewer) folds across in front of her chest, fist by her back shoulder, elbow forward, knees bent, a short tense hold. Drive: she lunges forward with a big step and the arm whips outward flat at chest height, the back of the hand leading, to full extension to the right. Impact: she holds the extended arm and the deep lunge for a moment. Recovery: she pulls back and straightens into her standing pose. The far arm stays by her side. Empty hands, no weapon.

Итог: **`counterA` взят** — профиль; `run-05` (белая полоса) выброшен. `counterB` — замах на три четверти к камере.

## Наблюдение по всем вызовам

PixMiniMax на всех ударах вытягивает руку вперёд почти горизонтально, «как удар кулаком»: доводки «через пояс», «через грудь к дальнему плечу» не нарисовал ни один профильный вариант. Объём взмаха в профиле тело не даёт — его пытался дать скрипт меча (ракурс, слой, след), и владелец результат не принял (см. README, «Вердикт владельца по четвёртому кругу»).
