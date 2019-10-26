
@echo off

set H=%KSPDIR%
set GAMEDIR=EngineIgnitor

echo %H%

copy /Y "%1%2" "GameData\%GAMEDIR%\Plugins"
xcopy /e /y MM_Configs GameData\%GAMEDIR%\MM_Configs
xcopy /e /y Resources GameData\%GAMEDIR%\Resources


mkdir "%H%\GameData\%GAMEDIR%"
xcopy  /E /y GameData\%GAMEDIR% "%H%\GameData\%GAMEDIR%"
