#!/bin/bash

dotnet restore $2

dotnet build $2 -c Release --no-restore -p Version=$1

dotnet publish $2 -c Release --output $3