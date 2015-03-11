Param(
  [string]$what = "pack"
)

$nugetSource = "internal-nuget-source"
$apikey = $env:nugetapikey

if (($what -eq 'pack') -or ($what -eq 'both'))
{
	gci *.nupkg | remove-item
	gci *.exe -recurse | foreach-object { $null = new-item -itemtype file "$($_.fullname).ignore" }
	nuget pack
	gci *.ignore -recurse | foreach ($_) {remove-item $_.fullname}
}
if (($what -eq 'push') -or ($what -eq 'both'))
{
	gci *.nupkg | foreach-object { nuget push $_.fullname -Source $nugetSource -apikey $apikey }
}