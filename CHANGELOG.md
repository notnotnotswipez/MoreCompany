# [MoreCompany](https://github.com/notnotnotswipez/MoreCompany) Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Known Issues
- Some UI elements may not fully support more than 8 players

# 1.11.0 (2024-10-30)
- Fixed bug where too much cosmetic data caused none to send at all
- Added API for developers to configure hide-ability of cosmetics

# 1.10.2 (2024-10-23)
- Added the ability to change cosmetics mid-game via the pause menu
- Made the cosmetic button not show if no cosmetics are loaded
- Added the ability to disable cosmetics via the config
- Fixed masked & dead bodies cosmetics not instantly toggling when changed mid-game with a mod like LethalConfig
- Fixed party hat showing up in extra players' view in v65

# 1.10.1 (2024-08-19)
### Changed
- Modified how cosmetics are displayed on masked entities

# 1.10.0 (2024-08-17)
### Changed
- Updated to support v60/v61.

### Fixed
- Fixed some volume related issues.

# 1.9.4 (2024-07-06)
### Added
- Added slight workaround to fix player pitches in 4 player lobbies

## Fixed
- Fixed default player volume being quieter than usual

# 1.9.3 (2024-07-06)
This update has no information

# 1.9.1 (2024-04-22)
### Fixed
- Cosmetic related bugfixes

# 1.9.0 (2024-04-13)
### Added
- Added support for v50.
- Added various configuration options regarding cosmetics.

### Changed
- Made internal version of MC be calculated from the game number rather than a universal constant.

### Fixed
- Fixed player count dialogues not allowing you to type small digits.
- Fixed LAN player count issue at below 4 or above 50.
- Fixed meaningless chat error.

# 1.8.1 (2024-02-08)
### Added
- Readded optimization which was accidentally removed.

# 1.8.0 (2024-02-08)
### Fixed
- Fixed LAN support.
- Loading screen icon now shows the MoreCompany logo.
- Improved compatibility between other mods.

# 1.7.6 (2024-01-20)
### Changed
- Revert audio related changes. Will revisit at a later date.

# 1.7.5 (2024-01-19)
### Fixed
- Fixed bug regarding mimics ignoring the local cosmetic setting and equipping player cosmetics anyway.
- Minor "patch" relating to voice pitch. Only affects primary 4 players.

# 1.7.4 (2024-01-08)
### Fixed
- Fixed crew count changer not displaying on the host screen.

# 1.7.3 (2024-01-08)
### Added
- Added safety check for duplicate cosmetics.

### Fixed
- Fixed compatibility with v47.
- Fixed kicking not working on some players.

# 1.7.2 (2023-12-13)
### Added
- Added 1 new cosmetic.
- Added the ability to spin the display guy in the cosmetic screen.

### Changed
- Players which have been taken over by the mask will now keep their cosmetics when they become masked.

### Fixed
- Fixed R2Modman/Thunderstore Mod Manager compatibility when installing custom .cosmetics files.

# 1.7.1 (2023-12-09)
### Changed
- Updated the mod to function with v45.
- Moved cosmetic folder to plugins folder.

# 1.7.0 (2023-12-08)
### Changed
- Made Cosmetic system dynamic

# 1.6.0 (2023-12-04)
### Added
- Added 2 new cosmetics.
- Added new chest cosmetic anchor point.

### Fixed
- Fixed rare Coilhead targetting issue on extra players.
- Fixed jiggly cosmetics on players.

# 1.5.1 (2023-11-30)
### Fixed
- Fixed ship stall issue if the playercount was set to 8 or below.

# 1.5.0 (2023-11-30)
### Added
- Added 7 new cosmetics.
- Added option to disable cosmetics displaying on other players.
- Added player count selector to hosting box.

### Changed
- Upped max player cap to 50. (Default value is 32)
- Reverted LC_API serverlist display. Base MoreCompany players will only see other MoreCompany lobbies if they do not have LC_API installed.

### Fixed
- Fixed minor performance hit when in a lobby with a large player count while the player slots are not taken up.
- Fixed widescreen players not being able to see the cosmetic button on the main menu.

### Removed
- Removed 1 cosmetic.

# 1.4.2 (2023-11-25)
### Fixed
- Fixed death screen rendering above the results screen.

# 1.4.1 (2023-11-23)
### Fixed
- Fixed voice slider range going too quiet too quickly.

# 1.4.0 (2023-11-23)
### Added
- Added cosmetic system to MoreCompany. Currently features 12 cosmetics at the time of writing.

### Changed
- Expanded UI on quickmenu to be scrollable. All players have individual volume sliders. Including kick buttons.

# 1.3.0 (2023-11-20)
### Fixed
- Patched furniture getting desynced between clients if objects were moved prior to joining.
- Fixed suit on rack sometimes appearing far from the ship for clients other than the host.

# 1.2.1 (2023-11-19)
### Fixed
- Caught minor mistake in LC_API compatibility. Patched in this version.

# 1.2.0 (2023-11-19)
### Fixed
- Fixed cross compatibility with LC_API users and non LC_API users not being able to join one anothers lobbies.
- Patched out warn that occured every frame.

# 1.1.1 (2023-11-19)
### Changed
- The mod now targets BepInEx instead of MelonLoader.

# 1.1.0 (2023-11-17)
### Changed
- Increased player cap to 32 users.
- Modified version text in bottom left corner to display (MC) next to it on the menu.

### Fixed
- Prevented MoreCompany servers from showing up on public lobby displays. Only other MoreCompany users can see MoreCompany public lobbies.

# 1.0.1 (2023-11-16)
This update has no information

# 1.0.0 (2023-11-16)
This update has no information