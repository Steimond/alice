# Revision10 — укус гриба и открывание маленькой двери

5 ноября 2026, по ходу M3, сервис PixelLab. Гриб теперь применяется у самой маленькой двери (GDD 0.26): Алиса откусывает гриб, уменьшается, дверь открывается.

| Что | Инструмент, цена | Job, seed | Кадры в игру | Итог |
| --- | --- | --- | --- | --- |
| Открывание маленькой двери от `../Revision08/raw/little-door.png` | `animate_image_pixminimax`, 4 кадра, 1 | `79625296-30f3-420b-8b84-4dac4c22c93f`, 10021 | 0–4 → `Assets/_Alice/Art/World/Gates/gate_little_door_00..04` | С первого раза: створка уходит внутрь, открывая чёрный проём; рама неподвижна |
| Укус гриба, стойка без иглы в начале и конце | `animate_image_pixminimax`, 8 кадров, 1 | `7c0782e0-539f-4300-889e-a532b9bd15cd`, 10022 | 1–8 → `Assets/_Alice/Art/Characters/Alice/Trial/alice_trial_bite_00..07` | С первого раза: подносит кусочек гриба ко рту, откусывает, опускает руку |

Всего 2 генерации (баланс 268 → 270 из 2000).

Запрос двери: «The little wooden door swings slowly open inward on its hinges, revealing a pitch-black opening behind it, and stays open. The grey stone arch frame does not move at all. Nothing else is drawn.»

Запрос укуса: «She brings her near hand up to her mouth holding a small piece of red mushroom, takes a quick bite and chews once, then lowers her empty hand back to her side and stands as before. Feet stay planted, no step, she keeps facing right in profile. Small motion only.» Здесь предмет назван намеренно: гриб должен появиться в руке.

`to-game.ps1`: у всех кадров двери одна обрезка — по закрытой двери, чтобы арка не сдвигалась. Укус переносится на холст 128×80 со сдвигом полноразмерных кадров (x+36, y+20), в палитре стойки; красный кусочек гриба далёк от палитры и сохраняет свой цвет. Прежний одиночный `gate_little_door.png` удалён.

Иглы во время укуса в руке нет: стойка без иглы — вход генерации. В игре игла исчезает на время укуса и уменьшения.

Проверено в Play (комнаты D и E): Interact перед дверью → укус → уменьшение → дверь открывается; маленькая не прыгает и не бьёт; отойдя от двери, вырастает и снова прыгает; повторный укус у уже открытой двери, проход в E, там рост вернулся и дверь с той стороны тоже открыта.
