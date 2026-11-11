# Revision24: комбо с поворотом корпуса

10 ноября 2026. Владелец: анимации Алисы плоские, обычные удары нужны живые, «как в Blasphemous»: два взмаха перед собой (туда и обратно) и завершающий с полного разворота наискось от плеча. Они заменяют «удар, подъём, завершающий» (GDD 0.47). Алисе можно поворачиваться как угодно; меч только не меняет руку. Приём «удар с разворота» позже будет заменён другим.

Состояние: **первый удар принят владельцем** после покадрового просмотра (10 ноября 2026; одна правка — клинок после удара не качается). Общие правила, выведенные из него, — руководство по PixelLab, раздел 4.10. Второй и третий удары собраны тем же способом и ждут просмотра владельца.

## Что нашёл отсмотр старых клипов

13 клипов покадрово в увеличении 6×. Тело всегда в строгом профиле, бьёт одна вытянутая рука; удар длится один кадр, дальше 4–5 почти одинаковых кадров; замах стоит столбом; след — линия в 1 px; меч — жёсткая наклейка; комбо каждый раз возвращается в стойку. Брак: кулак-«молоток» (завершающий 1–5, удар в воздухе 1–4, стойка, жест), обрубок клинка (завершающий 6, удар в воздухе 5), клинок меняет длину в удержании, клинок на лице (завершающий 9–10, разворот 0), меч без кисти в прыжке 0–3 и падении, скачущий угол клинка в беге.

## Генерации

Все — `animate_image_pixminimax`, по подписке; `first_frame_url` = `upload:2b9b5563-8f03-4558-8745-3dc20cb69e13` (`Revision03/pipeline/stand/stand-noneedle.png`, жива до 13 ноября 2026), `view` side, `direction` east, `subject_description` и `initial_pose` — из руководства, 4.3. Баланс до — 716, после первого удара — 725, после всего комбо — 734.

| Набор | Seed | Job | Кадров | Цена | Итог |
| --- | --- | --- | --- | --- | --- |
| `s1A` | 2511 | `9c865fd6-45ef-43dc-b638-2a683a2c3563` | 16 | 2 | Замах хороший; после удара встаёт спиной в полный рост. Отброшен |
| `s1B` | 2512 | `960516f8-3e93-414c-85fe-5e6d72cef572` | 16 | 2 | То же, руки в боки спиной к камере. Отброшен |
| `s1C` | 2513 | `e3bba2dd-bf0c-4f4e-ace1-c77e98d8e0d8` | 16 | 2 | Годен, запасной |
| `s1D` | 2514 | `433f3786-9c8d-4392-aa80-8baafac986b6` | 16 | 2 | **Взят** |
| `s1in` | 2521 | `94edb1b5-ba6e-4469-9f57-447383d90fca` | 4 | 1 | **Взят**: вход из стойки в замах, `last_frame_url` — кадр 3 набора `s1D` |

`s1A`, `s1B` (последний кадр — стойка):

> a fast, heavy flat horizontal slash with her whole body turning, the first blow of a combo. Anticipation: she sinks into her knees and twists her torso open so her chest faces the viewer, her right arm (the one nearest the viewer) drawn far back behind her at shoulder height, elbow bent, fist clenched; she holds this coil for a beat. Drive: she pushes off the back foot, hips turn first, then shoulders, the right arm whips flat at chest height from behind her, round toward the viewer and forward to full reach to the right. Follow-through: momentum carries the arm on across her body, her torso keeps turning until her back is half toward the viewer, the right fist ends by her left shoulder, skirt and hair swing round after her; she holds. Recovery: she unwinds and straightens into her standing pose. Her left arm swings out as a counterweight. Empty hands, clenched fists, no weapon, nothing else is drawn.

`s1C`, `s1D` (последний кадр — стойка):

> a fast, heavy flat horizontal slash with her body turning, the first blow of a combo. Anticipation: she drops into a wide stance and twists her chest toward the viewer, her right arm (nearest the viewer) drawn far back behind her at shoulder height, fist clenched; a short hold. Drive: hips turn first, then shoulders, the right arm whips flat at chest height from behind her, round toward the viewer and forward to full reach to the right. Follow-through: the arm carries on across her body, the right fist ends low by her far hip, right shoulder forward, her back only a quarter toward the viewer; her head keeps looking right in profile, she stays low with bent knees and holds this wound-up pose for several frames, skirt and hair swinging after. Recovery: she unwinds back the way she came and straightens into her standing pose. She never stands with her back to the viewer. Empty hands, no weapon.

`s1in`:

> she quickly drops into a wide stance, stepping the back foot back, and twists her chest toward the viewer while her right arm (nearest the viewer) pulls back behind her at shoulder height with a clenched fist, the left forearm comes across her chest. A smooth, quick wind-up with no blur, no streaks, no motion lines. Empty hands, no weapon, nothing else is drawn.

Наблюдения: рука и в `s1C`/`s1D` не уходит «через тело к дальнему бедру» — остаётся вытянутой вперёд, но корпус повёрнут спиной на четверть, и вместе с серпом это читается взмахом. Первый кадр после стойки во всех четырёх вариантах — сплошной смаз; кадр самого рывка (чистый кадр `run-05`) — полоса цвета кожи через всё тело, выброшен: рывок передаёт серп.

## Сборка

1. `build-combo.ps1` — `strip-smears.ps1` → `cleanup.ps1 -SkipNeedle` → `specks.ps1`; чистые кадры `alice/<набор>-clean/run-NN.png` (сырой кадр NN+1).
2. `combo.json` — кадры клипа по порядку: чистый кадр, центр кулака, угол клинка, серп.
3. `render-combo.ps1 [-GameDir <папка Trial>]` — меч, кисть поверх рукояти, серп; результат в `out/<клип>/`.

Первый удар, `alice_trial_slash_00..11`, разбиение фаз прежнее 4 / 3 / 5:

| Кадры | Источник | Что происходит |
| --- | --- | --- |
| 0–1 | `s1in` 0, 1 | Шаг в широкую стойку, грудь к камере, меч уходит назад |
| 2–3 | `s1D` 2, 4 | Замах: кулак у плеча сзади, клинок назад-вверх |
| 4 | `s1D` 6 | Выпад, спина на четверть к камере, серп через всё тело |
| 5 | `s1D` 7 | Остаток серпа |
| 6–8 | `s1D` 8, 10, 12 | Удержание выпада; клинок неподвижен вместе с рукой (правка владельца: наклон вниз и обратно читался «желе») |
| 9–11 | `s1D` 13, 14, 15 | Возврат в стойку |

Точки кулака сняты по пятнам кожи (крайнее левое в замахе, крайнее правое в выпаде). `Revision23/render-sword.ps1` эти кадры больше не трогает (параметр `-Skip`).

## Проверено

Файлы: 12 кадров отсмотрены в увеличении 8× (кисть, рукоять, острие, серп) — `strike1-x5.png`, `strike1.gif`. Кадры записаны в `Assets/_Alice/Art/Characters/Alice/Trial` с прежними именами. **В Play не проверялось**: зона попадания и тайминг фаз не менялись, второй и третий удары в игре пока старые.

## Удары 2 и 3

Игра начинает следующий удар в начале возврата предыдущего (`PlayerAttack.FixedUpdate`), поэтому каждый удар сгенерирован от позы, в которой кончается предыдущий; код не менялся. Опорные позы: **выпад** (кадр 9 набора `s1D`) и **открытая поза** (кадр 4 набора `s1D`: грудь к камере, правая рука отведена).

| Набор | Seed | Job | Кадров | Цена | От → до | Итог |
| --- | --- | --- | --- | --- | --- | --- |
| `s2A` | 2531 | `b2c04352-6ce7-46c8-b2a6-57e28bff7d7a` | 12 | 2 | выпад → открытая поза | **Взят**: закрутка спиной к камере, удар в профиль, раскрытие грудью к камере |
| `s2B` | 2532 | `83bb0bbe-8ff9-43af-b7d9-9da1d8aa8bce` | 12 | 2 | то же | Отброшен: почти без поворота, кадр рывка — сплошной смаз |
| `s2out` | 2541 | `765a4c3f-bf78-4718-94f8-187bd5dd84f8` | 4 | 1 | открытая поза → стойка | **Взят**: возврат в стойку после второго удара |
| `s3A` | 2551 | `44b8cf51-7db3-4995-81bf-3f76bfbc1205` | 16 | 2 | открытая поза → стойка | **Взят**: оборот лицом к камере — влево — спиной — вправо, кулак над головой, рубящий удар |
| `s3B` | 2552 | `5d824029-ca19-464b-91f4-4ae029aecb5e` | 16 | 2 | то же | Отброшен: оборот в другую сторону (сразу спиной) |

`s2A`, `s2B` (`initial_pose`: «lunging to the right with her right arm stretched straight forward, her back a quarter toward the viewer»):

> a fast flat backhand slash, the second blow of a combo, sweeping back the other way. Anticipation: staying low, she folds her right arm (the outstretched one) across in front of her body, the fist tucked by her far hip, her back turning more toward the viewer; a very short hold. Drive: she unwinds, hips first, then shoulders, the back of the right hand leading, the arm whips flat at chest height out to the right at full reach and on round toward the viewer. Follow-through: her chest opens toward the viewer, the right arm ends swept out behind her at shoulder height, fist clenched, wide low stance, the left forearm across her chest; she holds this open pose. Her head keeps looking right the whole time. She stays low with bent knees, never standing up straight, never standing with her back to the viewer. Skirt and hair swing after the turn. Empty hands, no weapon.

`s2out` (`initial_pose`: «wide low stance, chest turned toward the viewer, right arm drawn back behind her at shoulder height»):

> she relaxes out of the fighting pose: lowers the drawn-back right arm, draws her feet together and turns back into her calm standing pose in profile facing right, skirt and hair settling. A smooth, unhurried recovery with no blur, no streaks, no motion lines. Empty hands, no weapon, nothing else is drawn.

`s3A`, `s3B` (та же `initial_pose`, что у `s2out`):

> the heavy finishing blow of a combo out of a full spin. Spin: from this coiled pose she whirls a full turn on the spot, turning to face the viewer, then to the left, then her back to the viewer, and round to face right again, skirt and hair flaring wide, her right arm carried round with her and rising, the fist high above her right shoulder as she comes round; she hangs there for a beat. Blow: she steps forward and chops diagonally down from above her right shoulder to low in front of her left knee, bending deep, shoulders driving through. Impact: she holds the low, bent pose with the arm down in front for several frames. Recovery: she straightens slowly into her standing pose facing right. Empty hands, clenched fists, no weapon.

Второй удар, `alice_trial_cut_00..10`, фазы 4 / 3 / 4:

| Кадры | Источник | Что происходит |
| --- | --- | --- |
| 0–2 | `s2A` 1, 3, 5 | Закрутка спиной к камере; рука с мечом перед телом, клинок виден у дальнего бедра |
| 3 | `s2A` 6 | Раскрутка, клинок выходит из-за тела справа |
| 4 | `s2A` 7 | Рука вытянута вперёд; дуга серпа по дальней стороне, за головой |
| 5 | `s2A` 8 | Грудь к камере, рука с мечом уходит влево; серп по ближней стороне поверх тела |
| 6 | `s2A` 9 | Остаток серпа |
| 7 | `s2A` 11 | Открытая поза — отсюда начинается третий удар |
| 8–10 | `s2out` 1, 2, 4 | Возврат в стойку |

Третий удар, `alice_trial_finisher_00..15`, фазы 5 / 3 / 8:

| Кадры | Источник | Что происходит |
| --- | --- | --- |
| 0–3 | `s3A` 1–4 | Полный оборот, юбка разлетается; меч несётся вокруг и поднимается |
| 4 | `s3A` 6 | Кулак над головой, клинок отведён назад |
| 5 | `s3A` 8 | Удар: Алиса согнута, клинок у пола, серп сверху вниз (кадр 7 с белой дугой генератора выброшен) |
| 6 | `s3A` 9 | Остаток серпа |
| 7–10 | `s3A` 10, 11, 12, 12 | Удержание внизу, клинок неподвижен |
| 11–13 | `s3A` 13, 14, 15 | Подъём, клинок идёт вверх вместе с рукой |
| 14–15 | `s3A` 16, 16 | Стойка |

Правка по ходу проверки: в замахе второго удара меч сначала был нарисован над дальним плечом, вплотную к поднятому левому кулаку, и читался в левой руке — перенесён к дальнему бедру.

## Проверено

- Файлы: все кадры трёх клипов отсмотрены в увеличении 5–8×; листы `strike1-x5.png`, `strike2-x5.png`, `strike3-x5.png`; анимации `strike1.gif`, `strike2.gif`, `strike3.gif`; связка `combo-chain.gif` (удар 1 до начала возврата, удар 2 до начала возврата, удар 3 целиком).
- Компиляция: код не менялся; в консоли Unity после запуска ошибок нет.
- Play: сцена `Prototype_Enemies` через `Alice → Play This Scene`, время 0,05, три нажатия атаки, 79 снимков камеры. Три удара идут связкой, серпы видны, крыса получает урон и гибнет от завершающего.

## Второй заход: удар в воздухе, контратака, стойка, парирование

Владелец принял комбо («Отлично выглядит») и попросил привести зону второго удара и оживить ещё 3–4 анимации.

- **Зона второго удара** — как у первого: высота 2,6, центр 1,5 (было 3 и 1,8 от «подъёма»). Изменено через `SerializedObject` в редакторе (`script eval_file`), `AliceCombat.asset`.
- **Время кадра стойки** — 0,2 с вместо 0,3 (`AliceSprites.asset`, тем же способом): 8 кадров дыхания идут 1,6 с.

| Набор | Seed | Job | Кадров | Цена | Итог |
| --- | --- | --- | --- | --- | --- |
| `airA` | 2611 | `e2ad6bca-5b00-4159-b13a-9766139b28ee` | 16 | 2 | Годен, запасной: в двух кадрах белые штрихи, кадр рывка — полоса через тело |
| `airB` | 2612 | `3d5b4e29-c6f9-43d6-8c26-dc58969009ad` | 16 | 2 | **Взят** |
| `ctrA` | 2621 | `e1503713-77bc-489f-8e1f-7fb34362744f` | 16 | 2 | Отброшен: в замахе стоит спиной в полный рост, удар — один тонкий кадр |
| `ctrB` | 2622 | `0a87d86b-9d0b-42d4-b6d4-f9fb42752575` | 16 | 2 | **Взят** |
| `idleA` | 2631 | `6dca8ffe-e10f-4718-b0e8-fcfc2ddb3f42` | 8 | 1 | Годен, запасной: движение слабее |
| `idleB` | 2632 | `d7f6e538-2c65-4d2b-8e43-bbf89a183c2d` | 8 | 1 | **Взят** |

Все — от стойки до стойки, поля те же, что у первого удара. Баланс после — 744.

`airA`, `airB`:

> a flat slash in mid-air during a jump, her body turning. She springs up off the ground, knees drawn up, skirt flaring. Anticipation: in the air she twists her chest toward the viewer, her right arm (nearest the viewer) drawn far back behind her at shoulder height, fist clenched; a short hold. Drive: hips turn first, then shoulders, the right arm whips flat at chest height round toward the viewer and forward to full reach to the right, her back a quarter toward the viewer, legs tucked, skirt and hair swinging after. She holds the reach for a moment, head looking right in profile. Recovery: she drops, lands softly on both feet and straightens into her standing pose facing right. She never shows her full back to the viewer. Empty hands, no weapon.

`ctrA`, `ctrB`:

> a fast, sharp backhand counter-slash with a lunge, right after blocking a blow. Anticipation: she snaps her right arm (nearest the viewer) across in front of her chest, fist by her left shoulder, her right shoulder forward and her back a quarter toward the viewer, knees bent; a very short tense hold. Drive: she lunges forward with a long step and unwinds, hips first, then shoulders, the back of the right hand leading, the arm whips flat at chest height out to the right at full reach. Follow-through: the arm carries on round toward the viewer, her chest opens toward the viewer, the right arm ends swept out behind her, low wide stance; she holds. Her head keeps looking right the whole time, she stays low. Recovery: she straightens into her standing pose facing right. Empty hands, no weapon.

`idleA`, `idleB`:

> she stands still and breathes: her chest and shoulders rise slowly and sink again, the hair and the hem of the skirt sway gently as in a light draught, the fingers of her right hand tighten and loosen. Feet stay planted in place, no step, she keeps facing right in strict profile, arms stay by her sides. Subtle, calm, smooth motion only. Empty hands, no weapon.

Клипы в игре:

| Клип | Кадры и фазы | Источник | Что изменилось |
| --- | --- | --- | --- |
| `alice_trial_airslash_00..11` | 5 / 3 / 4 | `airB` 1–5; 7, 8, 9; 11, 13, 14, 15 | Замах в воздухе грудью к камере, выпад со спиной на четверть, серп поверх тела |
| `alice_trial_counter_00..11` | 3 / 3 / 6 | `ctrB` 2, 4, 5; 7, 9, 10; 11–16 | Кулак у дальнего плеча, выпад, дуга по дальней стороне и серп по ближней, открытая поза |
| `alice_trial_idle_00..07` | цикл, 8 кадров (было 4) | `idleB` 1–8 | Качаются волосы и подол, рука чуть ходит; клинок под прежним углом идёт за кулаком (острие по x: 101 → 96 → 102, без зигзага) |
| `alice_trial_parry_00..11` | 4 / 3 / 2 / 3 | кадры и точки `Revision23` (`parryC`) | Только кисть поверх рукояти вместо «молотка»; позы прежние |

`Revision23/render-sword.ps1` эти клипы больше не трогает (`-Skip`).

## Проверено (второй заход)

- Файлы: все кадры четырёх клипов отсмотрены в увеличении 5–6×; листы `airslash-x5.png`, `counter-x5.png`, `idle-x6.png`, `parry-x5.png` и одноимённые `.gif`.
- Компиляция: код не менялся; после обновления ассетов и `Alice → Prototype → Attach Alice Sprites` в консоли ошибок нет, в наборе 8 кадров дыхания.
- Play: `Prototype_Enemies`, 53 снимка камеры. Удар в воздухе, парирование и контратака (после `Open Nearest Enemy`) проигрываются, серпы видны. Стойку в движении в Play отдельно не разглядывал: на снимках сцена слишком тесная.

## Третий заход: кисть и синяя лента (11 ноября 2026)

Владелец: «в кадрах айдл дыхания кулак опять молотком, там опять рукоятка меча поверх?» — да. Кисть возвращалась поверх рукояти только в радиусе 3 px, а у висящей руки с горизонтальным клинком контур рукояти ложился на предплечье. Затем: «пусть Алиса найдёт синюю ленту и ею сделает рукоятку, синий точно будет видно, а бинт сливается с рукой» (GDD 0.48).

Что сделано, без генераций:

1. Эталон меча перекрашен: 21 пиксель обмотки — в четыре синих тона, контур прежний (`Revision23/shard/sword-final.png`, прежний сохранён как `sword-rag.png`); повороты и ракурсы перестроены (`sword-rotate.ps1`, `sword-foreshorten.ps1`).
2. `render-combo.ps1`: часть рукояти внутри кулака не рисуется; точка хвата переезжает на ближайшую кожу, край кисти ищется вдоль клинка, меч придвигается вплотную — видны три пикселя синей ленты, гарда и клинок; кисть целиком поверх. Кадры, где меч не касается кисти, скрипт называет (`DETACHED`).
3. `Revision23/render-sword.ps1`: та же логика для остальных 206 кадров (бег, прыжок, урон, жест, казни и прочие), иначе кулак там выходил синим квадратом.

Ошибка по ходу: убрав рукоять без привязки, получил зазоры между кулаком и клинком (первый удар 6–9, завершающий 5–13, парирование 5–9) — поймано на листах до показа.

Проверено: семь клипов этой ревизии — каждый кадр в 5–9×, кисть стойки отдельно в 10×, строк `DETACHED` нет; остальные клипы — листами в 3× (поломок нет, кисть по кадрам крупно не разглядывалась). Ассеты обновлены в редакторе, ошибок в консоли нет. В Play после этой правки не запускал.

В прыжке 0–3 и в падении меч был за телом без видимой кисти — исправлено в четвёртом заходе.

## Четвёртый заход: зазор в стойке, прыжок, падение (11 ноября 2026)

Владелец: лента «просто белая меняется на синюю» — подтверждено; в дыхании между рукоятью и кистью местами зазор; дефект прыжка и падения чинить.

- **Зазор в стойке.** Рука в стойке наклонная, конец рукояти прямоугольный: в углах просвечивал передник. Теперь лента продолжается ещё на три пикселя назад без контура, кисть возвращается поверх, центр кисти — передний край кулака. То же в `Revision23/render-sword.ps1`.
- **Прыжок, кадры 0–3.** Поднятая рука нарисована в тени коричневым и не распознавалась как кожа, поэтому меч рисовался «за телом» и торчал из-за головы. Это рука с мечом: в кадре 4 она же опускается вперёд. Правки в `Revision23/overrides.json` (`shade`, меч в кулак перед телом, клинок вверх-вперёд вдоль руки: 34°, 34°, 34°, 22°, дальше прежние 10°).
- **Падение.** Рук в кадрах не было. Перегенерировано в два шага от прежнего кадра падения (`upload:51af0ea5-9a77-4a98-b754-3bc0c9d1cf60`, окно 64 × 64 игрового холста):

| Набор | Seed | Job | Кадров | Цена | Итог |
| --- | --- | --- | --- | --- | --- |
| `fallA` | 2711 | `1e8e4f35-9d63-4b4b-85fe-afae8866bf2a` | 4 | 1 | Годен, запасной |
| `fallB` | 2712 | `312fb38f-a7d7-4063-b7f1-ada485f97940` | 4 | 1 | **Взят кадр 4** как новая поза: правая рука отведена назад-вниз, кулак виден |
| `fallLoopA` | 2721 | `53b8ce2f-a69e-493d-acd7-937ac8e9a9ff` | 8 | 1 | **Взят**: цикл от новой позы, волосы и подол полощутся сильнее |
| `fallLoopB` | 2722 | `9a4ad93f-057d-42d4-b51c-f4c8e574f3bf` | 8 | 1 | Годен, запасной |

`fallA`, `fallB` (конец не закреплён):

> while falling, she brings her right arm (the one nearest the viewer) out and holds it low behind her, away from her body, elbow slightly bent, the bare forearm and clenched fist clearly visible against the empty background to the left of her skirt; then she holds that pose. She keeps falling in the same place facing right in profile, skirt and hair streaming upward. Her body, head and legs do not change. Empty hands, no weapon, nothing else is drawn, no motion lines.

`fallLoopA`, `fallLoopB` (первый и последний кадр — кадр 4 набора `fallB`):

> she keeps falling through the air: the skirt, the apron and the hair flutter and ripple upward in the rush of air. Her body, head and legs stay in the same pose and the same place; her right arm stays held out low behind her with the clenched fist clearly visible, it only trembles a little. A smooth seamless loop, subtle motion, no motion lines. Empty hands, no weapon, nothing else is drawn.

`alice_trial_fall_00..07` — кадры 1–8 набора `fallLoopA`, меч в кулаке, клинок назад-вниз вдоль руки. Окно для генерации было вырезано с x = 32, а `cleanup.ps1` кладёт кадры на x + 36: чистые кадры набора один раз сдвинуты на 4 px влево, чтобы Алиса стояла там же, где в прежнем падении (габарит по высоте совпал сам). Баланс после — 748.

Проверено: стойка — кисть в 11× во всех восьми кадрах; прыжок и падение — каждый кадр в 6×; выборка из остальных клипов после новой привязки — в 4×, поломок нет; строк `DETACHED` нет. Play (`Prototype_Enemies`, 28 снимков): стойка и прыжок видны, меч в кулаке. Падение в Play не попало в снимки: прыжок на месте короткий, клип падения в нём не включается.
## Что осталось

- Первые четыре кадра парирования одинаковые (удержание блока) — поза не оживлялась.
- Удержания после удара в контратаке (4 кадра) и в завершающем почти статичны.
- Если атаку дожать поздно, когда Алиса уже почти вернулась в стойку, следующий удар комбо начнётся с позы выпада — небольшой скачок.
- Со старым кулаком остались: бег, прыжок, падение, приземление, жест, урон, подкат, отдых, подбор, казни, приём «удар с разворота» (его владелец хочет заменить другим).