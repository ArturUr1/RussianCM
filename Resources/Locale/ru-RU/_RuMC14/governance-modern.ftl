governance-ahelp-workspace-header = [bold]Центр обращений[/bold]
governance-ahelp-workspace-subtitle = Очередь, переписка и действия дежурного в одном окне.
governance-ahelp-list-heading = [bold]Очередь[/bold]
governance-ahelp-list-hint = Сначала возьмите свободное обращение. После этого здесь откроется полная переписка.
governance-ahelp-filter-placeholder = Поиск по игроку, ID или тексту обращения…
governance-ahelp-filter-empty = [color=#8c96a8]По этому запросу ничего не найдено.[/color]
governance-ahelp-reply-placeholder = Напишите ответ игроку…
governance-ahelp-counter-modern = Открыто: {$open} • Моих: {$mine}
governance-ahelp-template-greeting = Приветствие
governance-ahelp-template-greeting-text = Здравствуйте. Я взял ваше обращение и сейчас разбираюсь в ситуации.
governance-ahelp-template-details = Уточнить
governance-ahelp-template-details-text = Пожалуйста, уточните, что именно произошло, где это случилось и кто участвовал.
governance-ahelp-template-wait = Подождать
governance-ahelp-template-wait-text = Спасибо. Мне нужно немного времени, чтобы проверить информацию и логи.
governance-ahelp-send = Отправить
governance-ahelp-empty-modern = [color=#8c96a8]Открытых обращений сейчас нет.[/color]
governance-ahelp-selected-marker = ▶
governance-ahelp-ticket-card-modern = {$selected} #{$id} • {$reporter} • {$status} • {$time}
    {$summary}
governance-ahelp-no-selection-hint = [color=#8c96a8]Выберите обращение слева, чтобы посмотреть подробности.[/color]
governance-ahelp-conversation-header = [bold]Обращение #{$id}[/bold] • {$reporter}
governance-ahelp-conversation-meta = Статус: {$status}  •  Создано: {$time}
    SS14: {$uuid}
governance-ahelp-unclaimed-preview = [color=#8c96a8]Предпросмотр обращения[/color]
    {$summary}
    [italic]Возьмите обращение, чтобы открыть переписку и ответить игроку.[/italic]
governance-ahelp-transcript-empty = [color=#8c96a8]В этом обращении пока нет сообщений.[/color]
governance-ahelp-message-role-responder = [color=#ff5a5a]● Дежурный[/color]
governance-ahelp-message-role-player = Игрок
governance-ahelp-message-line = [color=#8c96a8]{$time}[/color] [bold]{$role} • {$sender}[/bold]
    {$body}
governance-ahelp-status-waiting-player = Ожидает игрока
governance-ahelp-send-failed = Не удалось отправить сообщение. Проверьте, что обращение всё ещё закреплено за вами.
governance-ahelp-player-unavailable = Центр поддержки сейчас недоступен.
governance-ahelp-player-send-failed = Не удалось отправить сообщение. Попробуйте ещё раз.
governance-ahelp-player-resolve-failed = Не удалось закрыть обращение.
governance-ahelp-player-title = Центр поддержки
governance-ahelp-player-header = [bold]Нужна помощь?[/bold]
governance-ahelp-player-description = Опишите проблему своими словами. Обращение попадёт свободному дежурному, а вся переписка останется здесь.
governance-ahelp-player-conversation-title = [bold]Переписка[/bold]
governance-ahelp-player-tips = [color=#8c96a8]Укажите, что произошло, где вы находитесь и кого касается ситуация. Не создавайте несколько обращений по одной проблеме.[/color]
governance-ahelp-player-message-placeholder = Опишите проблему или ответьте дежурному…
governance-ahelp-player-send = Отправить
governance-ahelp-player-resolve = Проблема решена
governance-ahelp-player-status = [bold]Статус:[/bold] {$status}
governance-ahelp-player-assignee-waiting = [bold]Дежурный:[/bold] ожидается
governance-ahelp-player-assignee = [color=#ff5a5a][bold]● Дежурный:[/bold] {$name}[/color]
governance-ahelp-player-empty = [color=#8c96a8]У вас пока нет активного обращения. Напишите сообщение ниже, чтобы создать его.[/color]
governance-ahelp-player-status-new = Новое обращение
governance-ahelp-player-status-open = В очереди
governance-ahelp-player-status-claimed = В работе
governance-ahelp-player-status-waiting = Ожидает вашего ответа
governance-ahelp-player-status-escalated = Передано в инцидент

governance-ahelp-incident-heading = [bold]Инцидент[/bold]
governance-ahelp-incident-none = [color=#8c96a8]Активный инцидент для этого обращения не создан.[/color]
governance-ahelp-incident-active = [bold]LiveIncident #{$id}[/bold] • цель: {$target} • тип: {$type}
governance-ahelp-incident-target-placeholder = Ник игрока или SS14 UUID
governance-ahelp-incident-type-placeholder = Тип инцидента
governance-ahelp-incident-type-default = нарушение правил
governance-ahelp-incident-create = Создать инцидент
governance-ahelp-incident-target-required = Укажите игрока, которого касается инцидент.
governance-ahelp-incident-target-not-found = Игрок с таким ником или SS14 UUID сейчас не найден на сервере.
governance-ahelp-incident-self-target = Дежурный не может создать инцидент против самого себя.
governance-ahelp-incident-type-invalid = Тип инцидента должен содержать от 2 до 64 символов.
governance-ahelp-incident-access-denied = У вас нет временного полномочия на создание live-инцидента.
governance-ahelp-incident-create-failed = Не удалось создать инцидент. Убедитесь, что обращение всё ещё закреплено за вами.

governance-ahelp-actions-heading = [bold]Действия по инциденту[/bold]
governance-ahelp-action-reason-placeholder = Основание действия (10–512 символов)…
governance-ahelp-action-freeze-seconds-placeholder = Секунды
governance-ahelp-action-request-explanation = Запросить объяснение
governance-ahelp-action-view-logs = Просмотреть логи
governance-ahelp-action-freeze = Заморозить
governance-ahelp-action-round-remove = Удалить до конца раунда
governance-ahelp-action-history-heading = [bold]История действий[/bold]
governance-ahelp-action-history-empty = [color=#8c96a8]По этому инциденту действий пока нет.[/color]
governance-ahelp-action-card = #{$id} • [bold]{$type}[/bold] • {$status} • {$approvals}/{$required}{$duration}
    {$reason}
governance-ahelp-action-duration =  • {$seconds} сек.
governance-ahelp-action-type-explanation = Запрос объяснения
governance-ahelp-action-type-logs = Просмотр логов
governance-ahelp-action-type-freeze = Заморозка
governance-ahelp-action-type-round-remove = Удаление до конца раунда
governance-ahelp-action-status-proposed = [color=#ffd166]ожидает одобрения[/color]
governance-ahelp-action-status-approved = [color=#72d572]одобрено[/color]
governance-ahelp-action-status-executed = [color=#72d572]выполнено[/color]
governance-ahelp-action-status-rejected = [color=#ff5a5a]отклонено[/color]
governance-ahelp-action-status-expired = истекло

governance-ahelp-approval-heading = [bold]Ожидают вашего решения[/bold]
governance-ahelp-approval-empty = [color=#8c96a8]Нет действий, требующих второго голоса.[/color]
governance-ahelp-approval-card = Действие #{$id} • инцидент #{$incident}
    {$actor} → {$target} • [bold]{$type}[/bold] • {$approvals}/{$required}
    {$reason}
governance-ahelp-approval-approve = Одобрить
governance-ahelp-approval-reject = Отклонить

governance-ahelp-logs-heading = [bold]Логи цели[/bold]
governance-ahelp-logs-empty = [color=#8c96a8]Логи не загружены. Нажмите «Просмотреть логи».[/color]
governance-ahelp-log-line = [color=#8c96a8]{$time}[/color] [bold]{$type}[/bold] {$message}

governance-ahelp-action-access-denied = У вас нет временного полномочия для этого действия.
governance-ahelp-action-no-incident = Сначала создайте инцидент для этого обращения.
governance-ahelp-action-invalid = Неизвестное действие по инциденту.
governance-ahelp-action-reason-invalid = Укажите основание длиной от 10 до 512 символов.
governance-ahelp-action-freeze-duration-invalid = Длительность заморозки должна быть от 1 до 120 секунд.
governance-ahelp-action-create-failed = Не удалось создать действие по инциденту.
governance-ahelp-action-target-unavailable = Цель или автор действия сейчас недоступны на сервере.
governance-ahelp-action-execution-failed = Действие одобрено, но сервер не смог его выполнить. Проверьте состояние цели и полномочия.
governance-ahelp-action-review-failed = Не удалось записать решение. Возможно, вы не можете голосовать за это действие или оно уже рассмотрено.
