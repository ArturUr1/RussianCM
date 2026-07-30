# Yautja Original Spawn Loadout Design

## Goal

Сделать прямой спавн трёх player-ролей яутжа в CMU соответствующим оригинальному CMSS13: при появлении игрок получает только bracer и communicator, а броню, маску, плащ, backpack, медикаменты и оружие выбирает через яутжа-вендор.

## Scope

Изменяются обычный Hunter, Youngblood и Bad Blood player-spawn.

Полные gear-прототипы сохраняются для вендорных наборов, специальных event-мобов и Bad Blood Grunt/Leader. Их поведение не должно быть случайно заменено минимальным player-стартом.

## Architecture

1. Добавить отдельные минимальные StartingGear-прототипы:
   - обычный Hunter: `CMUYautjaBracer` и `CMUYautjaCommunicator`;
   - Bad Blood: `CMUYautjaBadBloodBracer` и `CMUYautjaBadBloodCommunicator`.

2. Перевести `CMUMobYautja` и player-прототип `CMUMobYautjaBadBlood` на соответствующие минимальные Loadout-прототипы. Полные `CMUYautjaHunterGear*` и `CMUYautjaBadBloodGear*` оставить доступными для уже существующих специальных сценариев.

3. Изменить `CMUYautjaYoungbloodGear`: убрать маску и оставить communicator + bracer. Youngblood уже экипируется этим StartingGear через `YautjaYoungbloodSystem`.

4. Разделить профильное применение на визуально-личностную часть и экипировку. При первичном player-spawn профиль продолжает задавать имя, возраст, внешность и настройки bracer, но не создаёт броню, маску, гревсы или плащ в пустых слотах. Применение профиля к предметам после выдачи через вендор сохраняется через существующие post-vendor hooks.

## Data flow

Обычный Hunter и Bad Blood получают минимальный Loadout при создании `JobEntity`; `StationSpawningSystem` применяет профиль без автоматической экипировки профильных предметов. Youngblood получает минимальный StartingGear при `MindAdded`. После этого все три роли используют уже существующие loadout vendors для обязательного снаряжения и выбора оружия.

## Testing

- Обновить интеграционные проверки player-spawn для Hunter и Bad Blood: в слотах остаются только communicator и bracer, остальные стартовые предметы отсутствуют.
- Добавить проверку Youngblood StartingGear без маски.
- Сохранить проверки bracer, communicator, фракции, доступа и post-vendor profile replacement.
- Запустить точечные Yautja integration tests, затем сборку/тесты затронутых проектов.

## Non-goals

- Не менять баланс, цены, состав существующих вендоров и доступность их предметов.
- Не удалять полные gear-прототипы, используемые NPC/event-пресетами.
- Не менять механику рангов, whitelist, vendor points или профильной кастомизации.
