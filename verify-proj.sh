#!/bin/bash
(dotnet restore $1 3>&2 2>&1 1>&3) 2>/dev/null
(dotnet build $1 -c Release --no-restore 3>&2 2>&1 1>&3) 2>/dev/null