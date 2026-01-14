@echo off
echo Username:
set /p user=
echo User Id:
set /p id=

.\..\node\node.exe index.js -username=%user% -userid=%id%
