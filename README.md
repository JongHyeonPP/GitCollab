# Git Collab for Unity

A Git-based file locking and team collaboration tool for Unity Editor.

## Installation

### Via Git URL (Recommended)
1. Open `Window > Package Manager`
2. Click `+` button → `Add package from git URL...`
3. Enter: `https://github.com/JongHyeonPP/GitCollab.git`

### Manual Installation
Copy the `GitCollab` folder to your Unity project's `Packages/` folder.

## Features

- **File Locking**: Lock binary files (.unity, .prefab, .asset, etc.) to prevent conflicts
- **Git Hooks**: Automatically prevents committing files locked by others
- **Project View Integration**: Visual lock status icons in Project window
- **Team Dashboard**: View all locks and team members at a glance

## Usage

### Lock/Unlock Files
1. Right-click a file in Project view
2. Select `Git Collab > Lock File`
3. When done, select `Git Collab > Unlock File`

### Dashboard
Open `Window > Git Collab > Dashboard` to:
- View your locks and team locks
- Manage settings
- Reinstall/remove Git hooks

### Keyboard Shortcut
- `Shift+Ctrl+R`: Refresh lock status

## How It Works

The package creates a `.gitcollab/` folder in your project root:

```
.gitcollab/
├── locks/          # Lock files (Base64 encoded paths)
├── team.json       # Team member info
└── config.json     # Settings
```

This folder **must be committed to Git** to share lock status with your team.

## Requirements

- Unity 2021.3 or later
- Git installed and configured
- Project must be a Git repository

## License

MIT License - see [LICENSE](LICENSE) file
