# Сектор газа

Небольшая 2D игра с видом сверху на Windows Forms в сеттинге заброшенной лаборатории.

## Структура репозитория

- `SectorGaza.sln` - решение Visual Studio.
- `src/SectorGaza.Model` - модель игры (состояние мира и логика).
- `src/SectorGaza.WinForms` - окно, ввод и отрисовка.
- `assets/sprites` - спрайты персонажей.
- `assets/tiles` - тайлы окружения.
- `assets/licenses` - источники и лицензии ассетов.

## Запуск

1. Установить `.NET SDK 8` или Visual Studio 2022 с workload `.NET desktop development`.
2. Открыть `SectorGaza.sln` и запустить `SectorGaza.WinForms`.

Или через терминал:

```powershell
dotnet build .\src\SectorGaza.WinForms\SectorGaza.WinForms.csproj
dotnet run --project .\src\SectorGaza.WinForms\SectorGaza.WinForms.csproj
```
