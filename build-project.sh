#!/bin/bash

dotnet restore src/SheetCuttingTools.Grasshopper
dotnet build src/SheetCuttingTools.Grasshopper -c Release --no-restore
dotnet publish src/SheetCuttingTools.Grasshopper -c Release --output ./publish