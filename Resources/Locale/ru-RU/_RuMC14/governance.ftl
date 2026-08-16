cmd-governance-player-only = Эта команда доступна только подключённому игроку.
cmd-governance-status-description = Показывает активную смену RUCM Community Governance.
cmd-governance-status-help = Использование: {$command}
cmd-governance-status-inactive = Активная DutySession для текущего раунда не найдена.
cmd-governance-status-active = DutySession #{$session} активна для раунда #{$round} до {$expires}.

cmd-governance-freeze-description = Временно замораживает игрока в рамках активного инцидента Governance.
cmd-governance-freeze-help = Использование: {$command} <игрок|UUID> <1-120 секунд> <incident-id> <причина>
cmd-governance-freeze-denied = Действие отклонено сервером: {$reason}
cmd-governance-freeze-success = {$target} заморожен на {$seconds} с. Инцидент: {$incident}.

governance-duty-observer-only = Активная смена Community Governance допускает участие в раунде только наблюдателем.
governance-denial-disabled = система Governance отключена
governance-denial-invalid-input = некорректный incident-id или текст причины
governance-denial-not-on-duty = нет активной DutySession или capability moderation.freeze
governance-denial-not-observer = исполнитель должен находиться в режиме наблюдателя
governance-denial-self-target = нельзя применить действие к себе
governance-denial-invalid-duration = длительность выходит за разрешённые пределы
governance-denial-target-unavailable = цель недоступна или не имеет игрового тела
governance-denial-already-frozen = цель уже заморожена другим механизмом
governance-denial-unknown = неизвестная ошибка проверки полномочий
