# Vault Dash — Unity Edition

A Clash Royale/Subway Surfers-inspired endless runner game built in Unity 2022 LTS.

## 🚀 Setup

### Requirements
- Unity 2022.3 LTS (2022.3.20f1 recommended)
- Android Build Support module
- Git

### Clone & Open
```bash
git clone https://github.com/SneakyTurtleNL/vault-dash-unity.git
```

Then open the folder in **Unity Hub** → Add → select the cloned folder.

### First Scene
Open  in the Unity Editor.

## 🏗️ Project Structure

```
Assets/
├── Scenes/         # Unity scenes (MainMenu, Game, etc.)
├── Scripts/        # C# game scripts
├── Sprites/        # 2D art assets
├── Audio/          # Music & sound effects
├── Prefabs/        # Reusable game objects
├── Materials/      # Materials & shaders
└── Animations/     # Animation clips & controllers
```

## 📦 Build

Android APK is built automatically via GitHub Actions on every push to .

Manual build:
```
File → Build Settings → Android → Build
```

## 🔀 Development Workflow

- Branch naming: 
- Commit often, commit small
- Always pull before pushing
- Main branch triggers CI/CD build

## 🎮 Game Design

Vault Dash is an endless runner where:
- Player navigates through tunnel/vault environments
- Avoid obstacles, collect coins
- Clash Royale-style card power-ups
- Subway Surfers lane-switching mechanics

## 📱 Target Platform
- Android (primary)
- iOS (future)
