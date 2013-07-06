@setlocal enabledelayedexpansion
@echo off

set _name=seth
set _src=Program.cs

call vcvars.bat

if exist "%_name%.exe" (
    del /Q /F "%_name%.exe"
)

csc.exe /nologo /t:exe /platform:anycpu /out:Bin\%_name%.exe %_src%

if %ERRORLEVEL% equ 0 (
    echo Built successfully..
    if exist "Bin\%_name%.exe" (
        if exist "C:\Kody\Root" (
            echo Copying to root folder..
            copy "Bin\%_name%.exe" "C:\Kody\Root\%_name%.exe" /Y
        )
    )
) else (
    echo.
    pause
)

@endlocal && exit /B 0