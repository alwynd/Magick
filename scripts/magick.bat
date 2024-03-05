rem default args: -perc 10 minsize 1048576

call env.bat
call runmagick.bat -folder Unity\Assets\Art -debug %ADDITIONAL_ARGS% > runmagick.log
