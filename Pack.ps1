param([Switch]$Rebuild)

#<#
# Merge nuspec templates.
#
# $baseFile: A file path to the base template file to be merged.
#
# $mergingFile: A file path to the base template file to be merging.
#
# $outputFile: A file path to the temporary merged *.nuspec file.
##>
function MergeXml([string]$baseFile, [string]$mergingFile, [string]$outputFile )
{
    $baseXml = ( Select-Xml $baseFile -XPath '/' ).Node
    $mergingXml = ( Select-Xml $mergingFile -XPath '/' ).Node
    
    $baseMetadata = $baseXml.SelectSingleNode( '/package/metadata' )
    foreach( $appendingMetadata in $mergingXml.SelectSingleNode( '/package/metadata' ).ChildNodes )
    {
        $baseMetadata.AppendChild( $baseXml.ImportNode( $appendingMetadata, $true ) ) | Out-Null
    }
    
    $basePackage = $baseXml.SelectSingleNode( '/package' )
    $basePackage.AppendChild( $baseXml.ImportNode( $mergingXml.SelectSingleNode( '/package/files' ), $true ) ) | Out-Null
    
    $baseXml.OuterXml | Out-File $outputFile
}

#<#
# Creates NuGet package for specified kind.
#
# $kind: The specifier of package kind.
##>
function CreatePackage( [string]$kind )
{
    $tempNuspec = ".\MsgPack-RPC-$kind.nuspec"
    MergeXml '.\MsgPack-RPC.nuspec.xml' ".\MsgPack-RPC-$kind.nuspec.xml" $tempNuspec

    .\.nuget\nuget.exe pack $tempNuspec
    Remove-Item $tempNuspec
}

[string]$temp = '.\nugettmp'
[string]$builder = "$env:windir\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe"

[string]$sln = 'MsgPack-RPC.sln'

$buildOptions = @()
if( $Rebuild )
{
    $buildOptions += '/t:Rebuild'
}

$buildOptions += '/p:Configuration=Release'

&$builder $sln $buildOptions

CreatePackage 'full' 
CreatePackage 'core'
CreatePackage 'client'
CreatePackage 'server'
