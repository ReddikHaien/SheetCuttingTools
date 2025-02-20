#!/bin/bash
(dotnet restore $2 3>&2 2>&1 1>&3) 2>/dev/null
(dotnet build $2 -c Release --no-restore -p Version=$1 3>&2 2>&1 1>&3) 2>/dev/null