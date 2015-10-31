@setlocal EnableDelayedExpansion
@echo off

if not defined bin set "bin=c:\bin"

:init
    set "OutputName=seth"
    set "SourceFiles=*.cs"

    where /Q csc.exe
    if %errorlevel% NEQ 0 call "%bin%\vcvars_env.bat"

:main
    rem if exist "Bin\%OutputName%.exe" del /Q /F "Bin\%OutputName%.exe"

    echo Building..

    csc.exe /nologo /t:exe /platform:anycpu /out:Bin\%OutputName%.exe %SourceFiles%
    if %errorlevel% NEQ 0 goto :builderror

    echo Built successfully..
    echo.

    if exist "Bin\%OutputName%.exe" if exist "%bin%\%OutputName%.exe" call :copyfile

:end
    endlocal
    exit /B

:builderror
    echo.
    pause
    goto :end

:copyfile
    echo Copying to bin folder..
    copy /B /V /Y "Bin\%OutputName%.exe" "%bin%\%OutputName%.exe"
    goto :eof
