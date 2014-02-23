@echo off

md tmp
ilmerge /out:tmp\AirPlayer.dll AirPlayer.dll ShairportSharp.dll DirectShowWrapper.dll ZeroconfService.dll
IF EXIST AirPlayer_UNMERGED.dll del AirPlayer_UNMERGED.dll
ren AirPlayer.dll AirPlayer_UNMERGED.dll
IF EXIST AirPlayer_UNMERGED.pdb del AirPlayer_UNMERGED.pdb
ren AirPlayer.pdb AirPlayer_UNMERGED.pdb

move tmp\*.* .
rd tmp

