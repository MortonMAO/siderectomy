@echo off
chcp 65001 >nul
setlocal
set "UNITY="
if exist "%ProgramFiles%\Unity\Hub\Editor\2022.3.62f3c1\Editor\Unity.exe" set "UNITY=%ProgramFiles%\Unity\Hub\Editor\2022.3.62f3c1\Editor\Unity.exe"
if not defined UNITY if exist "%ProgramFiles(x86)%\Unity\Hub\Editor\2022.3.62f3c1\Editor\Unity.exe" set "UNITY=%ProgramFiles(x86)%\Unity\Hub\Editor\2022.3.62f3c1\Editor\Unity.exe"
if defined UNITY (
    start "" "%UNITY%" -projectPath "%~dp0."
) else (
    echo 未自动找到 Unity 2022.3.62f3c1，请用 Unity Hub 的 Add 按钮添加本目录后打开：
    echo   %~dp0
    pause
)