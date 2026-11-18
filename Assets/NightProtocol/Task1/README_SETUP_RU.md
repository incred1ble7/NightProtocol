# Night Protocol — технический прототип Day 1

Откройте `Assets/Scenes/Station_Day1_Prototype.unity` в Unity 6.6 (6000.6.7f1). Сцена сделана из `inc greybox`.

## Inspector

- `NP_InteractiveMechanics/Greybox_Player` использует существующие `GreyboxFirstPerson`, `CharacterController` и дочернюю `PlayerCamera`. В прототипе выключен `Allow Jump And Sprint`. Поля `Interaction Prompt` и `NPFlashlight` уже заполнены. `PlayerCamera/Flashlight` — выключенный на старте Spotlight.
- `Operator Terminal` ссылается на игрока, общий `NPShiftProgress` и панель `Day 1 UI/Terminal Window`. В панели назначены TMP-статус и три кнопки. В сцене один `EventSystem` с `InputSystemUIInputModule`.
- P01 и P02 — экземпляры префабов из `Assets/NightProtocol/Task1/Prefabs`. При установке нового экземпляра укажите сценовый `NPShiftProgress` в поле `Shift Progress` и проверьте ID. Для этой смены нужны ровно две разные точки.
- Терминал и точки размещены на открытой площадке. `Room1` и `Generator_room` в исходнике — блоки с коллайдерами, модель башни без коллайдеров.

## Play Mode

1. Запустите сцену, при необходимости кликните по Game View. Проверьте WASD, мышь и включение и выключение фонаря по F. Space и Shift не меняют передвижение.
2. На старте терминал прямо перед игроком. Нажмите E. Должен появиться курсор, движение и фонарь блокируются. Нажмите **Start Shift**, затем **Close**. **Submit Report** пока недоступна.
3. Найдите P01 слева и P02 справа от ряда механизмов, если смотреть в сторону башни со старта. Подойдите к каждой примерно на 2 м, наведитесь прицелом и нажмите E. Повторный осмотр той же точки не увеличивает счётчик. E по-прежнему работает на штатных дверях и переключателях.
4. Вернитесь к терминалу. При статусе 2/2 нажмите **Submit Report** и убедитесь, что появился статус `SHIFT 01 COMPLETE`. При новом запуске Play Mode смена начинается с нуля.

## FMOD

Интеграция и банки FMOD в рабочей копии отсутствуют. Для подключения ambience:

1. Импортируйте FMOD for Unity. В FMOD Studio создайте зацикленные 3D-события `event:/Ambience/Tower` и `event:/Ambience/Forest` с подходящими аудиофайлами и настройте расстояния затухания. Соберите Master Bank, банк строк и банк с событиями. Укажите источник банков в настройках FMOD for Unity и убедитесь, что банк ambience загружается перед запуском событий.
2. На `PlayerCamera` добавьте один `Studio Listener`. Разместите по одному `Studio Event Emitter` возле башни и на стороне леса у периметра. Назначьте каждому соответствующее событие через FMOD Event Browser.
3. Для обоих эмиттеров назначьте `Object Start` как Play Event и `Object Destroy` как Stop Event. В Play Mode пройдите от станции к периметру, прислушайтесь к обоим циклам и отрегулируйте уровни и расстояния в FMOD Studio. [Документация FMOD](https://www.fmod.com/docs/2.04/unity/game-components.html)
