@echo off
cd /d "%~dp0"
msbuild Source\ChTraits\ChTraits.csproj /t:Build /p:Configuration=Release /p:Platform="Any CPU"
pause
