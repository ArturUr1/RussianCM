# Роль сапёра КОФ.
au14-job-name-clfsapper = Сапёр КОФ
au14-job-description-clfsapper = Партизан, обученный подрывным работам. Расставляй ловушки, маскируй их и обращай саму землю колонии против сил правительства.
au14-job-prefix-clfsapper = САП

# Установка/обезвреживание ловушек
insfor-sapper-trap-deployed = Вы закладываете заряд, и он скрывается из виду.
insfor-sapper-trap-disarmed = Вы перерезаете растяжку и убираете заряд.
insfor-sapper-trap-deploy-container = Здесь нельзя установить ловушку.
insfor-sapper-trap-deploy-occupied = На этой клетке уже есть ловушка.
insfor-sapper-trap-unskilled = Вы возитесь с устройством, но понятия не имеете, как его настроить.

# Растяжка
insfor-sapper-tripwire-attached = Вы прикрепляете взрывчатку к растяжке.
insfor-sapper-tripwire-full = Больше взрывчатки не прикрепить.
insfor-sapper-tripwire-need-explosive = Сначала прикрепите взрывчатку к растяжке, прежде чем устанавливать её.
insfor-sapper-tripwire-place-other-end = Вы устанавливаете заряд. Теперь протяните проволоку туда, где хотите её закончить — до {$range} клеток по прямой в пределах видимости — и используйте её там.
insfor-sapper-tripwire-strung = Растяжка натянута и взведена.
insfor-sapper-tripwire-charge-gone = Заряд, к которому ведёт эта проволока, исчез.
insfor-sapper-tripwire-bad-spot = Здесь нельзя протянуть проволоку.
insfor-sapper-tripwire-too-close = Вы стоите прямо над зарядом.
insfor-sapper-tripwire-not-straight = Проволока должна идти по прямой от заряда.
insfor-sapper-tripwire-too-far = Слишком далеко от заряда, чтобы дотянуться.
insfor-sapper-tripwire-no-los = Нет прямой видимости до заряда.
insfor-sapper-tripwire-eject-verb = Снять взрывчатку
insfor-sapper-tripwire-ejected = Вы снимаете взрывчатку с заряда.

# Звуковая ловушка
insfor-sapper-audio-name-title = Звуковая ловушка
insfor-sapper-audio-name-prompt = Назовите ловушку
insfor-sapper-audio-default-name = Без названия
insfor-sapper-audio-location-unknown = неизвестное место
insfor-sapper-audio-radio-alert = Звуковая ловушка {$name} сработала. Местоположение: {$location}.

# Верстак сапёра
insfor-sapper-workbench-deployed = Вы раскладываете верстак и фиксируете его ножки.
insfor-sapper-workbench-need-materials = На верстаке не хватает материалов или компонентов (положите предметы на верстак или рядом с ним).
insfor-sapper-workbench-crafted = Вы собираете {$item}.

# Переключатель
au14-switch-on = Вы щёлкаете переключателем. Ударно-спусковой механизм перестаёт разбираться.
au14-switch-off = Вы возвращаете переключатель в исходное положение.
au14-switch-jammed = Механизм заклинивает — оружие заклинило!
au14-switch-exploded = Оружие разрывается у вас в руках!
au14-switch-jammed-shoot = Оружие заклинило! Сначала передёрните затвор.
au14-switch-rack-verb = Передёрнуть затвор (устранить задержку)
au14-switch-rack-fail = Вы передёргиваете затвор, но гильза остаётся зажатой.
au14-switch-rack-success = Смятая гильза вылетает. Оружие снова готово к стрельбе.

# Оружейный верстак
insfor-sapper-workbench-weapon-placed = Вы кладёте оружие на верстак.
insfor-sapper-workbench-weapon-occupied = На верстаке уже лежит оружие.
insfor-sapper-workbench-no-weapon = Сначала положите оружие на верстак.
insfor-sapper-workbench-slots-full = Все слоты этого оружия уже заняты.
insfor-sapper-workbench-attached = Вы принудительно устанавливаете насадку ({$slot}).
insfor-sapper-workbench-wrong-slot = Эта насадка не подходит ни к одному слоту данного оружия.
insfor-sapper-workbench-hold-attachment = Сначала возьмите насадку в руки.
insfor-sapper-workbench-take-weapon = Взять оружие
insfor-sapper-workbench-detach = Снять: {$name}

# Взлом банкомата
insfor-sapper-atm-already-hacked = Этот банкомат уже опустошён.
insfor-sapper-atm-hacked = Банкомат содрогается и выплёвывает {$amount} наличными.
insfor-sapper-atm-malfunction = ОШИБКА: УСТРОЙСТВО НЕИСПРАВНО. ОБРАТИТЕСЬ К АДМИНИСТРАТОРУ КОЛОНИИ.
insfor-sapper-console-drained = Средства с консоли утекают — {$amount} наличными высыпается наружу.
insfor-sapper-asrs-drained = Счёт ASRS опустошается у вас в руках — {$amount} наличными.
insfor-sapper-asrs-empty = На этом терминале нет средств для изъятия.

# Сеть шпионских камер
device-frequency-prototype-name-surveillance-camera-clf = Шпионские камеры КОФ

# Силок
insfor-sapper-snare-caught = Силок резко затягивается вокруг вас, сковывая руки и переворачивая вас!
insfor-sapper-snare-struggled-free = Вы вырываетесь из силка.
insfor-sapper-snare-cutting = Вы начинаете разрезать силок.
insfor-sapper-snare-cut-free = Силок разрезан, и вы падаете.

# Предметы
ent-AU14SapperTrapToolbox = набор ловушек сапёра
    .desc = Потрёпанный ящик с самодельными зарядами и растяжками. Всё, что нужно сапёру КОФ, чтобы превратить колонию в зону поражения.
    .suffix = КОФ, Сапёр

ent-AU14SapperIED = закопанное СВУ
    .desc = Самодельное взрывное устройство, зарытое под поверхностью и подключённое к скрытой педали давления. Заряда хватит, чтобы разнести всё, что окажется сверху.

ent-AU14SapperShotgunTrap = ружейная ловушка
    .desc = Обрезанный ствол, привязанный к колышку и подключённый к растяжке. Наступи перед ним — и он всадит заряд дроби в ноги.

ent-AU14SapperTripwireTrap = растяжка
    .desc = Пусковой блок, подключённый к почти невидимой растяжке, уходящей на несколько клеток вперёд к колышку. Прикрепи гранаты или взрывчатку к блоку, затем установи — всё, что пересечёт проволоку, не будучи союзным, сработает разом. Перережь проволоку кусачками, чтобы обезвредить.

ent-AU14SapperTripwireEndPlacer = катушка растяжки
    .desc = Свободный конец растяжки, тянущий проволоку обратно к установленному заряду. Используй там, где хочешь закончить линию.
    .suffix = КОФ, Сапёр

ent-AU14SapperTripwireSegment = растяжка
    .desc = Почти невидимая проволока, натянутая между зарядом и колышком. Пересечь её — очень плохая идея.

ent-AU14SapperTripwireEnd = колышек растяжки
    .desc = Дальний колышек, к которому привязана растяжка.

ent-AU14SapperSnareTrap = силок
    .desc = Скрытый клубок проволоки и крючков. Наступи — и он затянется, связав руки и повалив с ног.

ent-AU14SapperCraftingKit = материалы для самодельных ловушек
    .desc = Связка металлолома, проволоки и трофейной взрывчатки. Используй в руке, чтобы изготовить ловушки КОФ прямо на месте.
    .suffix = КОФ, Сапёр

ent-AU14SapperTrapAreaPreview = зона покрытия ловушки
    .desc = Область, в которой сработает устанавливаемая ловушка.

ent-AU14SapperAudioTrap = звуковая ловушка
    .desc = Свистковая сигнализация, подключённая к почти невидимой растяжке. Всё, что пересечёт проволоку, вызовет пронзительный свист и сообщение по радио ячейки. Перережь кусачками, чтобы заглушить.
