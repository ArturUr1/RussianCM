# Yautja CMSS13 Parity Fixes

## Цель

Довести три связанных участка реализации Yautja до проверяемого соответствия локальному CMSS13-референсу:

1. медицинское снаряжение и его payload;
2. интерактивную tactical map на корабле охотников;
3. переключаемый thermal visor Bio-Mask с просмотром мобов через стены.

Пиксельный аудит в этом цикле охватывает все медицинские спрайты, visor, hunter globe и три уже найденных несовпадающих состояния масок. Полный аудит спрайтов оружия и брони Yautja остаётся отдельной задачей.

## Исходные расхождения

Ревью текущего дерева и `cmss13-ref-full` подтвердило следующее:

- herbal case локально содержит 2+2 применения вместо source-поведения 20+20;
- локальные crystals дают три дозы по 45u вместо одноразовой 30u инъекции, а thrall-вариант не имеет отдельного source reagent/visual;
- full medicomp создаёт stackable медицинские предметы вместо трёх отдельных source-equivalent capsules;
- локальный healing gun бесконечный, тогда как CMSS13 использует loaded/empty/reload цикл;
- часть медицинских прототипов ссылается на неверные sprite states;
- `visor_nvg` и три состояния масок не совпадают с исходными DMI по пикселям;
- hunter globe имеет только tactical-map tracking/icon components и не открывает UI;
- текущий wall-vision overlay привязан к наличию `YautjaComponent`, а не к активному visor, и не защищает ownership/lifecycle.

Размер внешнего medicomp остаётся `Small` как намеренная локальная адаптация для выдачи в `pocket2`. Его storage capacity, whitelist содержимого и source payload не должны от этого изменяться.

## Решение и границы

### 1. Медицинское снаряжение

Медицинские прототипы сохраняются в существующем CMU/RMC namespace, но получают source-equivalent данные и поведение:

- herbal case создаётся с двумя mending-herb и двумя soothing-herb stacks с source-лимитами применений;
- базовый Yautja crystal становится одноразовым 30u item;
- thrall crystal получает отдельный source reagent и оранжевый source visual;
- filled medicomp варианты создают три отдельные healing-gel capsules, а не stack count, который искусственно увеличивает число капсул;
- healing gun получает loaded/empty состояния, расходует одну capsule и поддерживает корректную перезарядку;
- storage whitelist допускает source herbal case, но не sibling military herb container, если CMSS13 его не допускает;
- ограничение `storage_slots: 12` проверяется с учётом stack count/uses, а не только количества entity;
- `Item.size: Small` у внешнего medicomp сохраняется и покрывается отдельным тестом как documented local adaptation.

Существующие локальные названия компонентов и reagent systems переиспользуются, если они могут выразить source-поведение без изменения общего RMC-контракта. Новая общая медицинская система вводится только при необходимости для loaded/empty/reload цикла healing gun.

### 2. Точные спрайты

Для каждого предмета из охвата создаётся явное сопоставление source DMI state → локальный RSI state. Переиспользование похожего RMC-спрайта не считается parity.

Обязательные исправления:

- stabilizer gel → source `stabilizer_gel`;
- healing gel → source `healing_gel`;
- wound clamp → source `wound_clamp`;
- alien analyzer → source `scanner`;
- crystals → source `crystal`/thrall source visual;
- herbal case → source `surgical_case` representation;
- mask visor → source `visor_nvg`, с pixel verification;
- `pred_mask19_crimson`, `pred_mask19_silver`, `pred_mask_ancient_redglow` → source item/worn states;
- уже совпадающие `medicomp`, `medicomp_open`, herb states, healing-gun frames и hunter globe не заменяются без необходимости.

Проверка assets должна сравнивать размеры, state names, frame count, delay и pixel hash каждого охваченного кадра. Engine-specific переименования `icon`/`equipped-MASK` допустимы только при сохранении тех же пикселей.

### 3. Hunter globe и tactical map

Hunter globe становится физическим интерактивным viewer:

- на прототип добавляются `ActivatableUI`, `UserInterface` с `TacticalMapComputerBui` и `TacticalMapComputer`;
- computer получает all-faction read scope, соответствующий `MINIMAP_FLAG_ALL` из CMSS13; `faction: yautja` для этого объекта не используется;
- добавляется server-enforced read-only режим: globe не принимает drawing, line или label updates, а клиентский UI не показывает доступные инструменты рисования;
- текущие `TacticalMapTracked`, `TacticalMapIcon` и `TacticalMapAlwaysVisible` удаляются с globe, чтобы сам globe не отображался как посторонний blip;
- globe остаётся физически неразрушаемым, без ID/faction access lock;
- UI использует обычную interaction range и закрывается при отходе, удалении globe или потере питания;
- существующая bracer tactical-map action остаётся отдельным пользовательским механизмом и не используется как замена globe.

> Read-only enforcement является серверной гарантией. Скрытие кнопок в UI само по себе не считается защитой.

Для globe используется существующий powered-console pattern: `ApcPowerReceiver` с `needsPower: true` и рабочей нагрузкой, а также `RMCPowerReceiver` с idle/active load на equipment channel. Если в помещении globe отсутствует рабочий APC/power grid, исправляется размещение или power setup hunter ship; `needsPower: false` не добавляется только ради прохождения теста.

### 4. Thermal visor и wall-vision

Wall-vision больше не является врождённой способностью сущности с `YautjaComponent`. Источником applied-state служит фактически надетый visor item.

У visor glasses появляется ownership/source связь с конкретной маской или powered helmet. Для текущего wearer эффект считается активным только при одновременном выполнении условий:

1. visor item находится в `eyes`;
2. ownership/source указывает на активную маску или helmet;
3. `NightVisionItemComponent.User` совпадает с wearer;
4. режим visor не `Off`;
5. viewer и target находятся на одной карте;
6. wearer не удалён и не потерял применимый power/authorization state.

После успешного equip glasses к wearer добавляется networked thermal-source component. Клиентская система создаёт один world-space overlay только для local wearer с этим component. Overlay:

- выполняет target lookup только при активном thermal-source;
- показывает только подходящие mob sprites;
- исключает самого wearer, невидимые sprites, entities внутри containers и targets другой карты;
- использует явный render order;
- не раскрывает обычные objects, structures, контейнеры или содержимое storage;
- не меняет server-side line of sight, interaction range, targeting или damage rules.

Overlay не изменяет общий RobustToolbox renderer. Thermal presentation ограничивается контролируемым отображением существующего mob sprite; health bars, heat values и отдельная информация о предметах не добавляются.

## Lifecycle и обработка ошибок

### Visor

Порядок включения:

1. проверить wearer, Yautja technology, bracer, slot и blocking glasses;
2. создать visor glasses с source ownership;
3. выполнить equip;
4. только после успешного equip отметить visor applied и отправить thermal-source state.

Если equip не удался, состояние маски, glasses и thermal-source остаётся выключенным.

Порядок выключения и cleanup:

- выключение удаляет glasses только своего source;
- снятие/удаление маски удаляет только принадлежащие ей glasses;
- powered helmet не может удалить glasses обычной маски и наоборот;
- low-power shutdown выполняет тот же cleanup, что и ручное выключение;
- сохранённый `VisorEnabled` powered helmet не считается активным без фактически надетых glasses;
- удаление wearer, detach local player и map change снимают overlay без оставшегося client state.

### Tactical map

Ошибки открытия UI, отсутствие питания, удаление globe и выход из interaction range закрывают viewer без изменения authoritative tactical-map buckets. Drawing/label events от read-only viewer отбрасываются на сервере.

### Medical items

Невозможность создать payload, использовать capsule или перезарядить gun не должна оставлять несогласованные stack count, reagent volume или loaded/empty visual. Состояние меняется атомарно относительно действия пользователя.

## Тестирование

### Unit tests

- target selection при visor off/on;
- self, hidden sprite, non-mob, container descendant;
- map mismatch и remote eye;
- stack-aware medicomp capacity;
- source reagent/uses для обычного и thrall crystal;
- loaded/empty/reload transitions healing gun.

### Integration tests

- regular mask: off → on → off → power loss → unequip;
- powered helmet: intent flag после unequip не активирует wall-vision без glasses;
- thrall и tech-authorized wearer получают тот же thermal contract;
- две одновременно существующие маски/helmet не удаляют ownership друг друга;
- failed visor equip не оставляет applied state;
- hunter globe открывает UI рядом и закрывает его при отходе/потере питания;
- map state содержит all-faction buckets и отклоняет drawing/labels;
- все размещённые hunter globe wrappers наследуют функциональный viewer;
- medicomp variants содержат точные source-equivalent items/quantities;
- herbal case и military case соблюдают source whitelist;
- sprite prototype states и pixel hashes соответствуют source manifest.

### Verification

Порядок проверки:

1. `git diff --check` и выборочный просмотр только затронутых путей;
2. focused `Content.Tests`;
3. build/test для `Content.IntegrationTests` с узкими фильтрами;
4. asset parity script/manifest;
5. повторная проверка статуса рабочего дерева, чтобы unrelated user changes не попали в diff.

Если integration testhost снова аварийно завершится до assertion, результат помечается как инфраструктурный blocker с полным stack trace. Успешной считается только проверка, дошедшая до assertions.

## Критерии приёмки

Работа считается завершённой, когда:

1. выключенный visor не даёт смотреть мобов через стены;
2. включённый thermal visor даёт wall-vision только на текущей карте;
3. снятие visor, потеря power и map mismatch выключают эффект;
4. hunter globe открывает общую read-only tactical map с отображением faction buckets;
5. medicomp payload и healing behavior соответствуют CMSS13 с единственным документированным `Small` outer-size adaptation;
6. все заявленные medical/visor/globe/mask assets проходят pixel parity;
7. focused tests и build проходят без попадания несвязанных изменений в рабочий diff.

## Не входит в scope

- полный аудит всех Yautja weapon/armor sprites;
- изменение общей RobustToolbox visibility/FOV архитектуры;
- innate `SEE_MOBS` для Yautja без активного visor;
- thermal health bars, heat signatures или object-through-wall vision;
- замена bracer tactical map action;
- несвязанный баланс лечения, урона или power drain.
