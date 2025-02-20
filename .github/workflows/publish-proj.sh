$version = $1
$project= $2
$output = $3

dotnet restore ../../../$project

dotnet build ../../../$project -c Release --no-restore -p Version=$version

dotnet publish ../../../$project -c Release --output $output