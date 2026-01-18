# Changelog

All notable changes to this project will be documented in this file.

## [1.0.0] - 2026-01-19

### Added
- File locking system with Base64 encoded paths
- Git hooks (pre-commit, pre-push) for lock enforcement
- Project view overlay with lock status icons
- Main dashboard window with tabs (My Locks, Team, Settings)
- Right-click context menu integration
- Team management with auto-detection from Git history
- Notification system for lock events
- Keyboard shortcut (Shift+Ctrl+R) for refresh

### Features
- Works without external server (Git-only)
- Supports .unity, .prefab, .asset, .mat, .fbx, and more
- Compatible with Unity 2021.3+
