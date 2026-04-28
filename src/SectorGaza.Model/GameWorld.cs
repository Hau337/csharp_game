namespace SectorGaza.Model;

public sealed class GameWorld
{
    private const int RoomWidth = 1536;
    private const int RoomHeight = 960;
    private const int BorderSize = 40;
    private const int DoorOpeningSize = 200;
    private const int TransitionCooldownTicks = 18;
    private const int PlayerWidth = 28;
    private const int PlayerHeight = 28;
    private const int DoorOpeningTop = (RoomHeight - DoorOpeningSize) / 2;
    private const int DoorOpeningBottom = DoorOpeningTop + DoorOpeningSize;

    private readonly IReadOnlyList<LevelRoom> rooms;
    private int currentRoomIndex;
    private int transitionCooldown;
    private int storyMessageTicks;
    private string? activeStoryMessage;

    private const int StoryMessageDurationTicks = 320;

    private GameWorld(IReadOnlyList<LevelRoom> rooms, Player player)
    {
        this.rooms = rooms;
        Player = player;
        currentRoomIndex = 0;
    }

    private LevelRoom CurrentLevelRoom => rooms[currentRoomIndex];

    public Room Room => CurrentLevelRoom.Room;

    public Player Player { get; }

    public IReadOnlyList<Enemy> Enemies => CurrentLevelRoom.Enemies;

    public IReadOnlyList<Medkit> Medkits => CurrentLevelRoom.Medkits;

    public IReadOnlyList<RoomTransition> Transitions => CurrentLevelRoom.Transitions;

    public IReadOnlyList<StoryNote> Notes => CurrentLevelRoom.Notes;

    public KeyCard? KeyCard => CurrentLevelRoom.KeyCard;

    public FinalDoor? FinalDoor => CurrentLevelRoom.FinalDoor;

    public bool IsGameOver => !Player.IsAlive;

    public bool HasKeyCard { get; private set; }

    public bool IsVictory { get; private set; }

    public string? ActiveStoryMessage => storyMessageTicks > 0 ? activeStoryMessage : null;

    public string? InteractionHint => GetInteractionHint(CurrentLevelRoom);

    public int TotalEnemies
    {
        get
        {
            var total = 0;
            foreach (var room in rooms)
            {
                total += room.Enemies.Count;
            }

            return total;
        }
    }

    public int TotalNotes
    {
        get
        {
            var total = 0;
            foreach (var room in rooms)
            {
                total += room.Notes.Count;
            }

            return total;
        }
    }

    public int CollectedNotesCount
    {
        get
        {
            var total = 0;
            foreach (var room in rooms)
            {
                foreach (var note in room.Notes)
                {
                    if (note.IsCollected)
                    {
                        total++;
                    }
                }
            }

            return total;
        }
    }

    public int AliveEnemies
    {
        get
        {
            var alive = 0;
            foreach (var room in rooms)
            {
                foreach (var enemy in room.Enemies)
                {
                    if (enemy.IsAlive)
                    {
                        alive++;
                    }
                }
            }

            return alive;
        }
    }

    public int CurrentRoomNumber => currentRoomIndex + 1;

    public int TotalRooms => rooms.Count;

    public bool IsCurrentRoomCleared
    {
        get
        {
            foreach (var enemy in CurrentLevelRoom.Enemies)
            {
                if (enemy.IsAlive)
                {
                    return false;
                }
            }

            return true;
        }
    }

    public string CurrentRoomName => currentRoomIndex switch
    {
        0 => "\u0412\u0445\u043E\u0434\u043D\u043E\u0439 \u043A\u043E\u0440\u0438\u0434\u043E\u0440",
        1 => "\u0421\u0435\u043A\u0442\u043E\u0440 \u0430\u043D\u0430\u043B\u0438\u0437\u043E\u0432",
        2 => "\u0418\u0437\u043E\u043B\u044F\u0442\u043E\u0440",
        _ => CurrentLevelRoom.Name
    };

    public static GameWorld CreateDefault()
    {
        var player = new Player(CreateSpawnNearLeftDoor());
        return new GameWorld(CreateRooms(), player);
    }

    public void Update(InputState inputState)
    {
        Player.Tick();
        if (transitionCooldown > 0)
        {
            transitionCooldown--;
        }

        if (storyMessageTicks > 0)
        {
            storyMessageTicks--;
            if (storyMessageTicks == 0)
            {
                activeStoryMessage = null;
            }
        }

        if (IsGameOver || IsVictory)
        {
            return;
        }

        var room = CurrentLevelRoom;
        var axes = inputState.GetMovementAxes();
        Player.UpdateMovement(axes.Horizontal, axes.Vertical, room.Room);

        if (inputState.ConsumeAttackRequest())
        {
            Player.TryAttack(room.Enemies);
        }

        foreach (var enemy in room.Enemies)
        {
            enemy.Update(Player, room.Room);
            if (enemy.IsTouching(Player))
            {
                Player.TakeDamage(enemy.ContactDamage);
            }
        }

        if (inputState.ConsumeInteractRequest())
        {
            HandleInteraction(room);
            if (IsVictory)
            {
                return;
            }
        }

        TryUseTransitions(inputState);
    }

    private void HandleInteraction(LevelRoom room)
    {
        foreach (var note in room.Notes)
        {
            if (!note.TryCollect(Player))
            {
                continue;
            }

            ShowStoryMessage($"{note.Title}: {note.Text}");
            return;
        }

        if (room.KeyCard is not null && room.KeyCard.TryCollect(Player))
        {
            HasKeyCard = true;
            ShowStoryMessage("\u0412\u044B \u043D\u0430\u0448\u043B\u0438 \u043A\u043B\u044E\u0447-\u043A\u0430\u0440\u0442\u0443. \u0422\u0435\u043F\u0435\u0440\u044C \u043C\u043E\u0436\u043D\u043E \u043E\u0442\u043A\u0440\u044B\u0442\u044C \u0444\u0438\u043D\u0430\u043B\u044C\u043D\u044B\u0439 \u0448\u043B\u044E\u0437.");
            return;
        }

        if (room.FinalDoor is not null && Player.Bounds.IntersectsWith(room.FinalDoor.Bounds))
        {
            if (!HasKeyCard)
            {
                ShowStoryMessage("\u0428\u043B\u044E\u0437 \u0437\u0430\u0431\u043B\u043E\u043A\u0438\u0440\u043E\u0432\u0430\u043D. \u041D\u0443\u0436\u043D\u0430 \u043A\u043B\u044E\u0447-\u043A\u0430\u0440\u0442\u0430.");
                return;
            }

            if (!IsCurrentRoomCleared)
            {
                ShowStoryMessage("\u0421\u043D\u0430\u0447\u0430\u043B\u0430 \u0437\u0430\u0447\u0438\u0441\u0442\u0438\u0442\u0435 \u043A\u043E\u043C\u043D\u0430\u0442\u0443, \u0438 \u043F\u043E\u0442\u043E\u043C \u0430\u043A\u0442\u0438\u0432\u0438\u0440\u0443\u0439\u0442\u0435 \u0448\u043B\u044E\u0437.");
                return;
            }

            room.FinalDoor.Open();
            IsVictory = true;
            ShowStoryMessage("\u0428\u043B\u044E\u0437 \u0430\u043A\u0442\u0438\u0432\u0438\u0440\u043E\u0432\u0430\u043D. \u0412\u044B \u0432\u044B\u0431\u0440\u0430\u043B\u0438\u0441\u044C \u0438\u0437 \u0441\u0435\u043A\u0442\u043E\u0440\u0430.");
            return;
        }

        foreach (var medkit in room.Medkits)
        {
            var wasCollected = medkit.IsCollected;
            medkit.TryCollect(Player);
            if (!wasCollected && medkit.IsCollected)
            {
                ShowStoryMessage("\u0410\u043F\u0442\u0435\u0447\u043A\u0430 \u0438\u0441\u043F\u043E\u043B\u044C\u0437\u043E\u0432\u0430\u043D\u0430.");
                return;
            }
        }
    }

    private string? GetInteractionHint(LevelRoom room)
    {
        if (IsGameOver || IsVictory)
        {
            return null;
        }

        foreach (var note in room.Notes)
        {
            if (!note.IsCollected && note.Bounds.IntersectsWith(Player.Bounds))
            {
                return "E - прочитать записку";
            }
        }

        if (room.KeyCard is not null
            && !room.KeyCard.IsCollected
            && room.KeyCard.Bounds.IntersectsWith(Player.Bounds))
        {
            return "E - подобрать ключ-карту";
        }

        if (room.FinalDoor is not null && room.FinalDoor.Bounds.IntersectsWith(Player.Bounds))
        {
            if (!HasKeyCard)
            {
                return "Нужна ключ-карта";
            }

            if (!IsCurrentRoomCleared)
            {
                return "Сначала зачистите комнату";
            }

            return "E - активировать шлюз";
        }

        foreach (var medkit in room.Medkits)
        {
            if (medkit.IsCollected || !medkit.Bounds.IntersectsWith(Player.Bounds))
            {
                continue;
            }

            return Player.CurrentHealth >= Player.MaxHealth
                ? "Аптечка не нужна (HP полное)"
                : "E - использовать аптечку";
        }

        return null;
    }

    private void ShowStoryMessage(string text)
    {
        activeStoryMessage = text;
        storyMessageTicks = StoryMessageDurationTicks;
    }

    private void TryUseTransitions(InputState inputState)
    {
        if (transitionCooldown > 0)
        {
            return;
        }

        foreach (var transition in CurrentLevelRoom.Transitions)
        {
            if (!Player.Bounds.IntersectsWith(transition.TriggerBounds))
            {
                continue;
            }

            var movesForward = transition.TargetRoomIndex > currentRoomIndex;
            if (movesForward && !IsCurrentRoomCleared)
            {
                ShowStoryMessage("\u0414\u0432\u0435\u0440\u044C \u043E\u0442\u043A\u0440\u043E\u0435\u0442\u0441\u044F \u043F\u043E\u0441\u043B\u0435 \u0437\u0430\u0447\u0438\u0441\u0442\u043A\u0438 \u0432\u0440\u0430\u0433\u043E\u0432.");
                continue;
            }

            currentRoomIndex = transition.TargetRoomIndex;
            Player.TeleportTo(transition.TargetPlayerBounds);
            transitionCooldown = TransitionCooldownTicks;
            inputState.Reset();
            return;
        }
    }

    private static IReadOnlyList<LevelRoom> CreateRooms()
    {
        var spawnLeft = CreateSpawnNearLeftDoor();
        var spawnRight = CreateSpawnNearRightDoor();

        var entranceRoom = new LevelRoom(
            "Входной коридор",
            new Room(RoomWidth, RoomHeight, CreateWalls(
                hasLeftDoor: false,
                hasRightDoor: true,
                new[]
                {
                    new Wall(new IntRectangle(300, 180, 36, 440)),
                    new Wall(new IntRectangle(336, 180, 280, 36)),
                    new Wall(new IntRectangle(760, 420, 280, 36)),
                    new Wall(new IntRectangle(1080, 220, 40, 520)),
                    new Wall(new IntRectangle(1120, 220, 300, 40)),
                    new Wall(new IntRectangle(1320, 500, 40, 260))
                })),
            new List<Enemy>
            {
                new(new IntRectangle(930, 280, 30, 30), EnemyKind.Normal),
                new(new IntRectangle(1250, 300, 30, 30), EnemyKind.Normal),
                new(new IntRectangle(1370, 760, 30, 30), EnemyKind.Normal),
                new(new IntRectangle(560, 760, 30, 30), EnemyKind.Normal)
            },
            new List<Medkit>
            {
                new(new IntRectangle(150, 660, 24, 24)),
                new(new IntRectangle(680, 280, 24, 24)),
                new(new IntRectangle(1200, 840, 24, 24))
            },
            new List<RoomTransition>
            {
                CreateRightTransition(1, spawnLeft)
            },
            notes: new List<StoryNote>
            {
                new(
                    new IntRectangle(520, 820, 26, 22),
                    "\u0417\u0430\u043F\u0438\u0441\u043A\u0430 01",
                    "\u0418\u0445 \u0434\u0435\u0440\u0436\u0430\u043B\u0438 \u0432 \u0441\u0435\u043A\u0442\u043E\u0440\u0435 B. \u0415\u0441\u043B\u0438 \u0441\u0438\u0440\u0435\u043D\u0430 \u0441\u043D\u043E\u0432\u0430 \u0432\u043A\u043B\u044E\u0447\u0438\u0442\u0441\u044F, \u0432\u044B\u0445\u043E\u0434 \u043E\u0441\u0442\u0430\u043D\u0435\u0442\u0441\u044F \u0442\u043E\u043B\u044C\u043A\u043E \u0447\u0435\u0440\u0435\u0437 \u0448\u043B\u044E\u0437.")
            });

        var labRoom = new LevelRoom(
            "Сектор анализов",
            new Room(RoomWidth, RoomHeight, CreateWalls(
                hasLeftDoor: true,
                hasRightDoor: true,
                new[]
                {
                    new Wall(new IntRectangle(220, 100, 36, 560)),
                    new Wall(new IntRectangle(256, 100, 360, 36)),
                    new Wall(new IntRectangle(580, 220, 36, 430)),
                    new Wall(new IntRectangle(616, 220, 290, 36)),
                    new Wall(new IntRectangle(850, 360, 36, 380)),
                    new Wall(new IntRectangle(930, 260, 220, 36)),
                    new Wall(new IntRectangle(1180, 120, 40, 620)),
                    new Wall(new IntRectangle(1220, 520, 250, 40))
                })),
            new List<Enemy>
            {
                new(new IntRectangle(760, 180, 30, 30), EnemyKind.Fast),
                new(new IntRectangle(760, 600, 30, 30), EnemyKind.Normal),
                new(new IntRectangle(1030, 500, 30, 30), EnemyKind.Normal),
                new(new IntRectangle(1280, 240, 30, 30), EnemyKind.Normal),
                new(new IntRectangle(1360, 820, 30, 30), EnemyKind.Normal),
                new(new IntRectangle(500, 840, 30, 30), EnemyKind.Fast),
                new(new IntRectangle(1460, 700, 30, 30), EnemyKind.Normal)
            },
            new List<Medkit>
            {
                new(new IntRectangle(380, 700, 24, 24)),
                new(new IntRectangle(980, 130, 24, 24)),
                new(new IntRectangle(1320, 620, 24, 24))
            },
            new List<RoomTransition>
            {
                CreateLeftTransition(0, spawnRight),
                CreateRightTransition(2, spawnLeft)
            },
            notes: new List<StoryNote>
            {
                new(
                    new IntRectangle(1290, 860, 26, 22),
                    "\u0417\u0430\u043F\u0438\u0441\u043A\u0430 02",
                    "\u041A\u043B\u044E\u0447-\u043A\u0430\u0440\u0442\u0430 \u043E\u0441\u0442\u0430\u0432\u043B\u0435\u043D\u0430 \u0443 \u0441\u0435\u0432\u0435\u0440\u043D\u043E\u0439 \u043A\u043E\u043D\u0441\u043E\u043B\u0438. \u0411\u0435\u0437 \u043D\u0435\u0435 \u0444\u0438\u043D\u0430\u043B\u044C\u043D\u044B\u0439 \u0448\u043B\u044E\u0437 \u043D\u0435 \u043E\u0442\u043A\u0440\u044B\u0442\u044C.")
            },
            keyCard: new KeyCard(new IntRectangle(1370, 170, 26, 18)));

        var isolationRoom = new LevelRoom(
            "Изолятор",
            new Room(RoomWidth, RoomHeight, CreateWalls(
                hasLeftDoor: true,
                hasRightDoor: false,
                new[]
                {
                    new Wall(new IntRectangle(200, 90, 36, 640)),
                    new Wall(new IntRectangle(236, 90, 410, 36)),
                    new Wall(new IntRectangle(420, 240, 36, 560)),
                    new Wall(new IntRectangle(456, 240, 260, 36)),
                    new Wall(new IntRectangle(680, 90, 36, 520)),
                    new Wall(new IntRectangle(716, 420, 290, 36)),
                    new Wall(new IntRectangle(840, 240, 36, 560)),
                    new Wall(new IntRectangle(876, 240, 300, 36)),
                    new Wall(new IntRectangle(1080, 520, 36, 280)),
                    new Wall(new IntRectangle(1220, 140, 40, 660)),
                    new Wall(new IntRectangle(1260, 140, 220, 40)),
                    new Wall(new IntRectangle(1320, 600, 180, 40))
                })),
            new List<Enemy>
            {
                new(new IntRectangle(740, 680, 30, 30), EnemyKind.Normal),
                new(new IntRectangle(920, 640, 30, 30), EnemyKind.Normal),
                new(new IntRectangle(1020, 620, 30, 30), EnemyKind.Normal),
                new(new IntRectangle(960, 160, 30, 30), EnemyKind.Fast),
                new(new IntRectangle(1140, 680, 30, 30), EnemyKind.Fast),
                new(new IntRectangle(1310, 740, 30, 30), EnemyKind.Normal),
                new(new IntRectangle(1420, 840, 30, 30), EnemyKind.Fast),
                new(new IntRectangle(610, 840, 30, 30), EnemyKind.Normal),
                new(new IntRectangle(1490, 320, 30, 30), EnemyKind.Fast)
            },
            new List<Medkit>
            {
                new(new IntRectangle(330, 300, 24, 24)),
                new(new IntRectangle(1160, 120, 24, 24)),
                new(new IntRectangle(1420, 240, 24, 24))
            },
            new List<RoomTransition>
            {
                CreateLeftTransition(1, spawnRight)
            },
            finalDoor: new FinalDoor(new IntRectangle(RoomWidth - 84, (RoomHeight / 2) - 72, 28, 144)));

        return new List<LevelRoom>
        {
            entranceRoom,
            labRoom,
            isolationRoom
        };
    }

    private static IReadOnlyList<Wall> CreateWalls(bool hasLeftDoor, bool hasRightDoor, IEnumerable<Wall> innerWalls)
    {
        var walls = new List<Wall>
        {
            new(new IntRectangle(0, 0, RoomWidth, BorderSize)),
            new(new IntRectangle(0, RoomHeight - BorderSize, RoomWidth, BorderSize))
        };

        if (hasLeftDoor)
        {
            walls.Add(new Wall(new IntRectangle(0, 0, BorderSize, DoorOpeningTop)));
            walls.Add(new Wall(new IntRectangle(0, DoorOpeningBottom, BorderSize, RoomHeight - DoorOpeningBottom)));
        }
        else
        {
            walls.Add(new Wall(new IntRectangle(0, 0, BorderSize, RoomHeight)));
        }

        if (hasRightDoor)
        {
            walls.Add(new Wall(new IntRectangle(RoomWidth - BorderSize, 0, BorderSize, DoorOpeningTop)));
            walls.Add(new Wall(new IntRectangle(RoomWidth - BorderSize, DoorOpeningBottom, BorderSize, RoomHeight - DoorOpeningBottom)));
        }
        else
        {
            walls.Add(new Wall(new IntRectangle(RoomWidth - BorderSize, 0, BorderSize, RoomHeight)));
        }

        foreach (var innerWall in innerWalls)
        {
            walls.Add(innerWall);
        }

        return walls;
    }

    private static RoomTransition CreateRightTransition(int targetRoomIndex, IntRectangle targetSpawn)
    {
        var trigger = new IntRectangle(RoomWidth - 56, DoorOpeningTop + 8, 56, DoorOpeningSize - 16);
        return new RoomTransition(trigger, targetRoomIndex, targetSpawn, Direction.Right);
    }

    private static RoomTransition CreateLeftTransition(int targetRoomIndex, IntRectangle targetSpawn)
    {
        var trigger = new IntRectangle(0, DoorOpeningTop + 8, 56, DoorOpeningSize - 16);
        return new RoomTransition(trigger, targetRoomIndex, targetSpawn, Direction.Left);
    }

    private static IntRectangle CreateSpawnNearLeftDoor()
    {
        return new IntRectangle(70, (RoomHeight / 2) - (PlayerHeight / 2), PlayerWidth, PlayerHeight);
    }

    private static IntRectangle CreateSpawnNearRightDoor()
    {
        return new IntRectangle(RoomWidth - 70 - PlayerWidth, (RoomHeight / 2) - (PlayerHeight / 2), PlayerWidth, PlayerHeight);
    }
}
