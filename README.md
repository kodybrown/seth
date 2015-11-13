seth
====

Displays the environment variables in a nicely formatted list, with new options!

Usage:
------

seth.exe v1.0.13.51031
Copyright (C) 2003-2015 Kody Brown (@kodybrown).

  Displays the environment variables with various options.

USAGE: seth [options]

    >seth /?
    seth.exe v1.0.13.51031
    Copyright (C) 2003-2015 Kody Brown (@kodybrown).

      Displays the environment variables with various options.

    USAGE: seth [options]

      /?, -h          show this help
      -p, --pause     pauses after each screenful (applies -pp)
      -pp             pauses at the end

      --no-wrap       outputs formatted output, but without wrapping envar values.
                      this format is used when the output is being redirected.
      --wrap=n        forces wrapping at n characters instead of the window width.
                      enforces minimum value of 20.

      --align=[l|r]   aligns the envar name left or right. the default is left.

      --indent=n      sets the envar name indentation. the default is 16
                      characters.
      --no-indent     sets the envar name indentation to 0.

      --lower         lower-cases the envar names.
      --upper         upper-cases the envar names.
                      if --lower and --upper are not specified, the envar name is
                      not modified.

      --machine       shows only the machine-level environment variables.
      --process       shows only the process-level environment variables.
      --user          shows only the user environment variables.
      --all           shows all environment variables regardless of where it came
                      from. this is the default behavior.


Example output:
---------------

This was run with the console width to 80. (I also removed a bunch of lines.)

    ALLUSERSPROFILE  = C:\ProgramData
    APPDATA          = C:\Users\kodyb\AppData\Roaming
    CommonProgramFiles = C:\Program Files\Common Files
    CommonProgramFiles(x86) = C:\Program Files (x86)\Common Files
    CommonProgramW6432 = C:\Program Files\Common Files
    ComSpec          = C:\Windows\system32\cmd.exe
    OS               = Windows_NT
    Path             ╤ C:\bin;
                     ├ C:\Windows\system32;
                     ├ C:\Windows;
                     ├ C:\Program Files\Git\bin;
                     ├ C:\Program Files\Microsoft SQL Server\120\Tools\Binn\;
                     ├ C:\Program Files (x86)\Windows Kits\10\Windows Performance
                       Toolkit\;
                     ├ C:\Program Files (x86)\OpenSSH\bin;
                     ├ C:\Program Files (x86)\Microsoft Emulator Manager\1.0\;
                     ├ C:\tools\python\3.4.3;
                     ├ C:\tools\python\3.4.3\Scripts;
                     ├ C:\tools\python\3.4.3\Lib;
                     └ C:\Program Files (x86)\Microsoft VS Code\bin ;
    PATHEXT          = .LNK;.CMD;.BAT;.COM;.EXE;.VBS;.VBE;.JS;.JSE;.WSF;.WSH;.MSC;.
                       PY;.RB;.RBW
    PROCESSOR_ARCHITECTURE = AMD64
    ProgramData      = C:\ProgramData
    ProgramFiles     = C:\Program Files
    ProgramFiles(x86) = C:\Program Files (x86)
    PROMPT           = $P$G
    SystemRoot       = C:\Windows


> The [GitVersion MSBuild](https://github.com/kodybrown/GitVersion) assembly is required to compile this util (or you can remove the custom attributes in `seth.csproj`).
