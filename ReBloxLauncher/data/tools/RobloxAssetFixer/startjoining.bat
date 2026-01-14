@echo off
echo IP Address:
set /p ip=
echo Username:
set /p user=
echo User Id:
set /p id=

.\..\node\node.exe index.js -username=%user% -userid=%id% -ip=%ip% -joining -assetFromServer
