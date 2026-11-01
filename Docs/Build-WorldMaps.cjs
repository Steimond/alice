// Render the design graphs. This is a documentation tool, not Unity gameplay code.
// Requires Node.js and @viz-js/viz 3.25.0. ALICE_VIZ_MODULE may point to its installed directory.
const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');
const { instance } = require(process.env.ALICE_VIZ_MODULE || '@viz-js/viz');
const directory = path.join(__dirname, 'Maps');
const data = JSON.parse(fs.readFileSync(path.join(directory, 'WorldMapData.json'), 'utf8'));
const rooms = new Map(data.rooms.map(room => [room.id, room]));
assert.equal(rooms.size, data.rooms.length, 'Duplicate room IDs');
const abilities = ['A01', 'A02', 'A03', 'A04'];
const flags = [...new Set(data.rooms.flatMap(room => [
  ...(room.boss ? ['boss_' + room.boss] : []), ...(room.switches || []),
  ...(room.actions || []).flatMap(action => action.flags || [])
]))];
const bit = (list, value) => {
  const index = list.indexOf(value);
  assert.notEqual(index, -1, 'Unknown condition: ' + value);
  return 1 << index;
};
const mask = (list, values = []) => values.reduce((result, value) => result | bit(list, value), 0);
const allows = (condition, state) =>
  (state.a & mask(abilities, condition.abilities)) === mask(abilities, condition.abilities) &&
  (state.f & mask(flags, condition.flags)) === mask(flags, condition.flags);

const adjacency = new Map(data.rooms.map(room => [room.id, []]));
for (const edge of data.edges) {
  assert(rooms.has(edge.a) && rooms.has(edge.b), 'Unknown edge endpoint');
  mask(abilities, edge.abilities);
  mask(flags, edge.flags);
  adjacency.get(edge.a).push({ ...edge, to: edge.b });
  if (!edge.oneWay) adjacency.get(edge.b).push({ ...edge, to: edge.a });
}

function explore({ disabledBoss, disabledAbility, omitOptional = false } = {}) {
  const queue = [{ room: 'H00', a: 0, f: 0 }];
  const visited = new Set();
  const reached = new Set();
  for (let cursor = 0; cursor < queue.length; cursor++) {
    const state = { ...queue[cursor] };
    const room = rooms.get(state.room);
    if (omitOptional && room.optional) continue;
    for (const item of room.items || []) if (item !== disabledAbility) state.a |= bit(abilities, item);
    if (room.boss && room.boss !== disabledBoss) state.f |= bit(flags, 'boss_' + room.boss);
    state.f |= mask(flags, room.switches);
    for (const action of room.actions || []) if (allows({ abilities: action.abilities, flags: action.requiresFlags }, state)) state.f |= mask(flags, action.flags);
    const key = state.room + ':' + state.a + ':' + state.f;
    if (visited.has(key)) continue;
    visited.add(key);
    reached.add(state.room);
    for (const edge of adjacency.get(state.room)) if (allows(edge, state)) queue.push({ ...state, room: edge.to });
  }
  return { reached, states: visited.size };
}

function validate() {
  const checks = [];
  for (const room of data.rooms) {
    for (const target of room.views || []) assert(rooms.has(target), 'View target absent: ' + target);
    mask(flags, room.viewWhenFlags);
  }
  assert(!data.edges.some(edge => new Set([edge.a, edge.b]).has('H00') && new Set([edge.a, edge.b]).has('L02')), 'Observation window became a walkable connection');
  const normal = explore();
  assert(normal.reached.has('G08'), 'Main campaign cannot finish');
  assert.equal(normal.reached.size, rooms.size, 'Some rooms cannot be reached');
  checks.push('Основной путь проходим; все ' + rooms.size + ' узлов достижимы.');
  for (const boss of ['dodo', 'pigeon', 'cook', 'hatter', 'queen']) {
    assert(!explore({ disabledBoss: boss }).reached.has('G08'), 'Boss can be bypassed: ' + boss);
  }
  checks.push('Ни одного из пяти боссов нельзя пропустить по имеющимся связям.');
  for (const ability of abilities) {
    assert(!explore({ disabledAbility: ability }).reached.has('G08'), 'Ability not required: ' + ability);
  }
  checks.push('Все четыре способности необходимы для основного маршрута.');
  const noPigeon = explore({ disabledBoss: 'pigeon' });
  assert(!noPigeon.reached.has('K01'), 'Early mushroom bypasses Pigeon');
  checks.push('Гриб до Голубки не позволяет уйти к Герцогине.');
  assert(!explore({ disabledAbility: 'A04' }).reached.has('G01'), 'Garden is reachable without clock');
  checks.push('Служебная галерея и ножницы не дают войти в сад до часов.');
  assert(explore({ omitOptional: true }).reached.has('G08'), 'Optional content blocks main campaign');
  checks.push('Удаление всех необязательных узлов сохраняет проходимость основной ветки.');
  const beforeFinal = explore({ disabledBoss: 'queen' });
  for (const room of data.rooms.filter(room => room.optional)) {
    assert(beforeFinal.reached.has(room.id), 'Optional content requires finishing the game: ' + room.id);
  }
  checks.push('Все необязательные области достижимы до финального боя.');
  const mappedEpisodes = new Set(data.rooms.flatMap(room => room.episodes || []));
  const episodes = fs.readFileSync(path.join(__dirname, 'CampaignEpisodes.md'), 'utf8');
  for (const match of episodes.matchAll(/^### ([PER]\d{2})\./gm)) {
    assert(mappedEpisodes.has(match[1]) || data.outsideEpisodes.includes(match[1]), 'Episode absent from map: ' + match[1]);
  }
  checks.push('Все 44 эпизода привязаны к карте или явно вынесены за её пределы.');
  for (const quest of data.quests) {
    for (const id of [quest.start, ...quest.finds, quest.return]) assert(rooms.has(id), 'Quest room absent: ' + id);
    assert(new Set([quest.start, ...quest.finds, quest.return].map(id => rooms.get(id).region)).size >= 2, 'Quest does not cross regions: ' + quest.id);
  }
  checks.push('Для шести будущих квестовых линий есть места в нескольких областях.');
  checks.push('Обзор берега из H00 — только окно, дополнительного прохода H00–L02 нет.');
  return checks;
}

const escape = text => String(text).replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('>', '&gt;').replaceAll('"', '&quot;');
const quoted = text => JSON.stringify(String(text));
function wrap(text, width = 22) {
  const lines = [''];
  for (const word of String(text).split(' ')) {
    if ((lines.at(-1) + ' ' + word).trim().length > width && lines.at(-1)) lines.push(word);
    else lines[lines.length - 1] = (lines.at(-1) + ' ' + word).trim();
  }
  return lines.join('\n');
}

function nodeDefinition(room, portal = false) {
  const detail = [
    ...(room.items || []), room.boss ? 'БОСС: ' + room.bossName : '',
    room.rest ? 'ОТДЫХ' : '', room.npc ? 'NPC' : '',
    ...(room.quests || []), room.optional ? 'ТАЙНИК' : '',
    room.views?.length ? 'ОБЗОР ' + room.views.join(', ') : ''
  ].filter(Boolean).join(' · ');
  const text = room.id + ' · ' + wrap(room.name) + (detail ? '\n' + wrap(detail, 28) : '');
  const classes = ['room', portal ? 'portal' : '', room.optional ? 'optional' : '', room.boss ? 'boss' : '', room.rest ? 'rest' : '', room.items?.length ? 'ability' : ''].filter(Boolean).join(' ');
  const tooltip = [room.npc, room.reward, room.viewNote, ...(room.episodes || []), ...(room.hiddenArea ? [room.hiddenArea] : [])].filter(Boolean).join(' · ') || room.name;
  const shape = room.boss ? 'doubleoctagon' : room.rest ? 'ellipse' : 'box';
  const fill = portal ? '#eeeae3' : room.boss ? '#f7e7e8' : room.optional ? '#f6f3eb' : '#fffdf8';
  const style = room.optional ? 'rounded,dashed,filled' : 'rounded,filled';
  return quoted(room.id) + ' [label=' + quoted(text) + ', tooltip=' + quoted(tooltip) + ', class=' + quoted(classes) + ', shape=' + quoted(shape) + ', style=' + quoted(style) + ', fillcolor=' + quoted(fill) + (room.boss ? ',color="#9b3545",penwidth=1.8' : '') + '];';
}

function edgeDefinition(edge) {
  const type = edge.type || 'main';
  const color = type === 'shortcut' ? '#367967' : type === 'return' ? '#9b3545' : '#767780';
  const style = type === 'secret' ? 'dashed' : type === 'shortcut' ? 'dotted' : 'solid';
  return quoted(edge.a) + ' -> ' + quoted(edge.b) + ' [dir=' + (edge.oneWay ? 'forward' : 'both') + ',label=' + quoted(edge.label || '') + ',class=' + quoted(type) + ',color=' + quoted(color) + ',fontcolor="#65646a",style=' + quoted(style) + (type === 'shortcut' ? ',constraint=false' : '') + '];';
}

function regionalDot(region) {
  const local = data.rooms.filter(room => room.region === region.id);
  const localIds = new Set(local.map(room => room.id));
  const edges = data.edges.filter(edge => localIds.has(edge.a) || localIds.has(edge.b));
  const external = [...new Set(edges.flatMap(edge => [edge.a, edge.b]).filter(id => !localIds.has(id)))];
  return 'digraph Map {\n' +
    'graph [rankdir=TB,bgcolor="#fffdf8",pad=0.25,nodesep=0.30,ranksep=0.55,splines=polyline,fontname="Arial",fontcolor="#292a2c"];\n' +
    'node [fontname="Arial",fontsize=13,color="#aaa7a0",margin="0.15,0.11",penwidth=1];\n' +
    'edge [fontname="Arial",fontsize=12,arrowsize=0.65,penwidth=1];\n' +
    local.map(room => nodeDefinition(room)).join('\n') + '\n' +
    external.map(id => nodeDefinition(rooms.get(id), true)).join('\n') + '\n' +
    edges.map(edgeDefinition).join('\n') + '\n}';
}

function overviewSvg() {
  const boxes = [
    {id:'H',x:640,y:415,w:260,h:112,lines:['ЗАЛ ДВЕРЕЙ','Общий узел','Маленькая дверь · ножницы']},
    {id:'L',x:220,y:680,w:300,h:112,lines:['01 · НИЖНИЕ ПОКОИ','Додо → A01 · веер','Мышь · тайники берега']},
    {id:'W',x:230,y:180,w:310,h:112,lines:['02 · РАЗРОСШАЯСЯ ЧАЩА','Гриб A02 → Голубка','Сети · скрытые ходы']},
    {id:'K',x:1060,y:180,w:310,h:112,lines:['03 · ВЛАДЕНИЯ ГЕРЦОГИНИ','Кухарка → A03 · Шаг','Поросёнок · клетки']},
    {id:'T',x:700,y:800,w:320,h:112,lines:['04 · ЗАСТЫВШЕЕ ЧАЕПИТИЕ','Шляпник → A04 · часы','Соня · гостевые тайники']},
    {id:'G',x:1080,y:575,w:310,h:112,lines:['05 · ЧЕРВОННЫЙ САД И СУД','Королева · освобождение','Садовники · архивы']}
  ];
  const arrow = (d, text, x, y, type = 'main') => '<g class="overview-edge ' + type + '"><path d="' + d + '" fill="none" stroke="' + (type === 'return' ? '#9b3545' : '#767780') + '" stroke-width="2" marker-end="url(#overview-arrow)"/><text x="' + x + '" y="' + y + '" text-anchor="middle">' + escape(text) + '</text></g>';
  const edges = [
    arrow('M 510 445 C 350 465 250 520 230 615', 'Спуск в слёзы', 285, 490),
    arrow('M 330 625 C 400 570 480 540 540 480', 'A01 · возврат наверх', 420, 590, 'return'),
    arrow('M 545 360 C 440 300 350 275 305 240', 'A01 · к дому Кролика', 410, 302),
    arrow('M 315 235 C 400 265 470 290 570 355', 'R01 · гриб + спуск за гнездом', 590, 267, 'return'),
    arrow('M 755 360 C 860 290 935 265 990 240', 'A02 · служебный путь', 890, 310),
    arrow('M 1060 120 V 65 H 230 V 120', 'R02 · после Кухарки: через узел обратно в Чащу', 650, 48, 'return'),
    arrow('M 75 190 H 30 V 880 H 700 V 865', 'A03 · исчезнувший мост', 290, 865),
    arrow('M 700 740 C 780 640 790 590 725 480', 'R03 · A04 · галерея', 840, 680, 'return'),
    arrow('M 770 440 C 850 450 890 500 935 530', 'Рычаг калитки из галереи', 1050, 438)
  ].join('');
  const nodes = boxes.map(box => '<g class="overview-node"><rect x="' + (box.x-box.w/2) + '" y="' + (box.y-box.h/2) + '" width="' + box.w + '" height="' + box.h + '" rx="8" fill="#fffdf8" stroke="#aba69c"/><text x="' + box.x + '" y="' + (box.y-23) + '" text-anchor="middle" font-weight="bold">' + escape(box.lines[0]) + '</text><text x="' + box.x + '" y="' + (box.y+5) + '" text-anchor="middle">' + escape(box.lines[1]) + '</text><text x="' + box.x + '" y="' + (box.y+31) + '" text-anchor="middle" font-size="16">' + escape(box.lines[2]) + '</text></g>').join('');
  return '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1300 930" role="img" aria-labelledby="overview-title"><title id="overview-title">Связи пяти биомов и Зала дверей</title><defs><marker id="overview-arrow" markerWidth="8" markerHeight="8" refX="7" refY="4" orient="auto"><path d="M0,0 L8,4 L0,8 Z" fill="#9b3545"/></marker></defs><style>.overview-edge text{paint-order:stroke;stroke:#fffdf8;stroke-width:6px;stroke-linejoin:round}</style><rect width="1300" height="930" fill="#fffdf8"/><g font-family="Arial,sans-serif" font-size="18" fill="#292a2c">' + edges + nodes + '<text x="650" y="905" text-anchor="middle" font-size="16">Схема связей, без масштаба. Тайники, NPC и квестовые места — на схемах помещений.</text></g></svg>';
}

(async () => {
  const checks = validate();
  const viz = await instance();
  for (const region of data.regions) {
    const dot = regionalDot(region);
    let svg = viz.renderString(dot, { format: 'svg', engine: 'dot' });
    svg = svg.slice(svg.indexOf('<svg')).replaceAll(/\bid="([^"]+)"/g, (_, id) => 'id="' + region.id + '-' + id + '"');
    fs.writeFileSync(path.join(directory, region.file + '.svg'), svg + '\n');
  }
  fs.writeFileSync(path.join(directory, 'overview.svg'), overviewSvg() + '\n');
  const report = { version: data.version, rooms: rooms.size, optional: data.rooms.filter(room => room.optional).length, connections: data.edges.length, checks };
  fs.writeFileSync(path.join(directory, 'Validation.json'), JSON.stringify(report, null, 2) + '\n');
  console.log(JSON.stringify(report, null, 2));
})().catch(error => { console.error(error); process.exitCode = 1; });
