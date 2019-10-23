
@echo off

set H=R:\KSP_1.7.3_dev
set GAMEDIR=EngineIgnitor

echo %H%

copy /Y "%1%2" "GameData\%GAMEDIR%\Plugins"
xcopy /e /y MM_Configs GameData\%GAMEDIR%\MM_Configs
xcopy /e /y Resources GameData\%GAMEDIR%\Resources


mkdir "%H%\GameData\%GAMEDIR%"
xcopy  /E /y GameData\%GAMEDIR% "%H%\GameData\%GAMEDIR%"
