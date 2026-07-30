# Военный яутжа как скрытая ивентовая роль

## Контекст

В оригинальном CMSS13 военная каста яутжа существует как часть Predator Deathsquad, а не как обычная профессия станции. В ней есть два пресета: `Military Caste Soldier` и `Military Caste Enforcer`. Отряд вызывается событием; обычного latejoin/job preference для этих ролей нет.

В RussianCM уже есть подходящий механизм для такой роли: скрытый `JobPrototype` можно выдать администратору через существующий `Spawn Here As Job`. В репозитории также уже присутствует военное снаряжение яутжа, включая силовую броню, военные наручи, коммуникатор, ранец пушек и двойные плазменные пушки.

## Цель

Добавить военного яутжа как две скрытые ивентовые роли, которые администратор может заспавнить вручную:

- `CMUYautjaMilitaryCasteSoldier` — рядовой участник отряда;
- `CMUYautjaMilitaryCasteEnforcer` — командир/энфорсер отряда.

Роли должны использовать обычный pipeline спавна профессии и не попадать в нормальный выбор профессии, latejoin или whitelist-роли.

## Поведение ролей

Обе роли:

- `hidden: true`;
- `whitelisted: false`;
- `canBeAntag: false`;
- `joinNotifyCrew: false`;
- `usePlayerProfile: false`;
- используют базовое тело `CMUMobYautja` и компоненты яутжа, включая замедление от ксеноморфных сорняков и боевой skill preset;
- получают фиксированный starting gear, а не обычную персонализацию охотника;
- доступны только через уже существующий админский `Spawn Here As Job`.

Отдельный автоматический вызов отряда, подбор нескольких призраков, лимиты `3–8` игроков и shuttle/deathsquad-логика в этот объём не входят. Это оставляет роль пригодной для ручных ивентов и не создаёт нового конкурирующего event framework.

## Экипировка

Солдат получает фиксированный боевой комплект военной касты на основе существующих прототипов:

- военный коммуникатор;
- закрытый силовой шлем;
- `CMUYautjaSoldierBracers` с автоподрывом после смерти;
- `CMUYautjaPoweredArmor`;
- `CMUYautjaPoweredGreaves`;
- стандартный боевой комплект оружия, медицинских предметов и расходников из существующих Yautja/MCaste-прототипов.

Энфорсер получает тот же базовый комплект, но с командным отличием:

- `CMUYautjaPoweredArmorEnforcer`;
- `CMUYautjaCannonPack`;
- `CMUYautjaDualPlasmaCannons`;
- командный вариант оружия/расходников, если он требуется существующими локальными прототипами.

Конкретные слоты (`head`, `gloves`, `outerClothing`, `shoes`, `back` и хранилища) должны быть оформлены в отдельных `StartingGearPrototype`, чтобы тестировать их независимо от job prototype.

## Локализация и учёт времени

Добавить названия и описания обеих ролей в английскую и русскую локали. Для каждой роли добавить отдельный `playTimeTracker`, как уже сделано для hunter, hellhound, youngblood и bad blood.

## Проверки

Добавить интеграционные проверки, которые подтверждают:

1. оба job prototype существуют, скрыты, не требуют whitelist и не доступны как обычный round job;
2. Soldier и Enforcer используют яутжа-сущность, `usePlayerProfile: false` и свои starting gear;
3. Soldier получает обычную powered armor, а Enforcer — командную powered armor;
4. starting gear содержит военные наручи, шлем, коммуникатор, поножи и соответствующий боевой комплект;
5. спавн через `StationSpawningSystem.SpawnPlayerMob` создаёт корректного яутжа с ожидаемым снаряжением;
6. существующие hunter/youngblood/bad blood роли не меняют свои loadout и поведение.

## Не входит в объём

- отдельный `emergency_call`/death squad command;
- автоматический набор игроков в отряд;
- роль погонщика гончих;
- изменение обычной hunter whitelist или hunter ship flow;
- новая система faction/briefing, если для неё нет уже используемого локального механизма.

## Источники и соответствие оригиналу

- CMSS13 gear presets: <https://github.com/cmss13-devs/cmss13/blob/master/code/modules/gear_presets/yautja.dm#L246-L324>
- CMSS13 deathsquad call: <https://github.com/cmss13-devs/cmss13/blob/master/code/datums/emergency_calls/deathsquad.dm#L818-L879>
- RussianCM admin spawn-as-job pipeline: `Content.Server/_RMC14/Admin/RMCAdminSystem.cs`
- RussianCM MCaste equipment: `Resources/Prototypes/_CMU14/Threats/Yautja/Equipment/mcaste_items.yml`
