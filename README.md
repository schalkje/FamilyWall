# 🏡 FamilyWall

> _A calm pulse in the center of home life — where family, memory, and daily rhythm come together._

> A living frame for family life.

Tagline:
**The heart of your connected family home.**


Essence:
A digital home centerpiece blending photos, memories, and home control.
It connects your family, calendar, and home assistant into a single, calm, and beautiful interface — always alive, always yours.

Tone & Style:
Warm minimalism · Calm motion · Natural light · Centered design
Keywords: together · warmth · flow · memory · calm · presence

Let’s define a visual identity system for FamilyWall — warm, elegant, and calm; tech-forward but deeply human.

![Passive mode mockup](./documents/design/images/mockup-passive-mode.png)




---

##  Quick Start

### Prerequisites

- .NET 9 SDK: https://dotnet.microsoft.com/download/dotnet/9.0
- Windows 11 (for Surface deployment)
- MAUI workload: `dotnet workload install maui`

### Build and Run

```pwsh
dotnet restore
dotnet build
cd src/FamilyWall.App
dotnet build -t:Run -f net9.0-windows10.0.19041.0
```

Or open `FamilyWall.sln` in Visual Studio 2022 and press F5.

### Project Structure

- **FamilyWall.App**  MAUI Blazor Hybrid shell
- **FamilyWall.Core**  Domain models, settings, abstractions
- **FamilyWall.Infrastructure**  SQLite persistence, caching
- **FamilyWall.Services**  Background services (photo indexing, presence)
- **FamilyWall.Integrations.Graph**  Microsoft Graph wrapper
- **FamilyWall.Integrations.HA**  Home Assistant + MQTT

### Configuration

Edit `src/FamilyWall.App/appsettings.json` to configure photo sources, calendar providers, Night Mode, and Home Assistant.

See `documents/design/architecture.md` for full specs and `documents/features/01-structure/plan.md` for implementation roadmap.
