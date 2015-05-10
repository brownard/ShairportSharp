@echo off

md tmp
ilmerge /lib:"C:\Windows\Microsoft.NET\Framework\v4.0.30319" /out:tmp\AirPlayer.dll AirPlayer.dll AirPlayer.Common.dll ShairportSharp.dll DirectShowWrapper.dll ZeroconfService.dll BouncyCastle.Crypto.dll Arm7.dll
IF EXIST AirPlayer_UNMERGED.dll del AirPlayer_UNMERGED.dll
ren AirPlayer.dll AirPlayer_UNMERGED.dll
IF EXIST AirPlayer_UNMERGED.pdb del AirPlayer_UNMERGED.pdb
ren AirPlayer.pdb AirPlayer_UNMERGED.pdb

move tmp\*.* .
rd tmp

