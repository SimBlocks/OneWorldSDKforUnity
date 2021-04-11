set currentDir=%cd%
set "pathFrom=%~dp0\Bin\"
set "pathTo=%~dp0\src\OneWorldSDK_UnityDemo\Assets\Plugins\"

REM COPY DEPENDENCIES
copy /y "%pathFrom%Newtonsoft.Json.dll" "%pathTo%"
copy /y "%pathFrom%Pngcs.dll" "%pathTo%"
copy /y "%pathFrom%gdal_csharp.dll" "%pathTo%"
copy /y "%pathFrom%BitMiracle.LibJpeg.NET.dll" "%pathTo%"
copy /y "%pathFrom%ogr_csharp.dll" "%pathTo%"
copy /y "%pathFrom%x86\SQLite.Interop.dll" "%pathTo%windows-x86\"
copy /y "%pathFrom%x64\SQLite.Interop.dll" "%pathTo%windows-x64\"
copy /y "%pathFrom%DotSpatial.Projections.dll" "%pathTo%"
copy /y "%pathFrom%osr_csharp.dll" "%pathTo%"
copy /y "%pathFrom%OsmSharp.dll" "%pathTo%"
copy /y "%pathFrom%OsmSharp.dll" "%pathTo%"
copy /y "%pathFrom%Npgsql.dll" "%pathTo%"
copy /y "%pathFrom%..\src\Libraries\packages\protobuf-net.2.3.7\lib\net40\protobuf-net.dll" "%pathTo%"
copy /y "%pathFrom%NetTopologySuite.dll" "%pathTo%"
copy /y "%pathFrom%BruTile.dll" "%pathTo%"
copy /y "%pathFrom%Microsoft.Bcl.AsyncInterfaces.dll" "%pathTo%"
copy /y "%pathFrom%Npgsql.dll" "%pathTo%"

copy /y "%pathFrom%System.*.dll" "%pathTo%"
del "%pathTo%System.Net.Http.dll"

copy /y "%pathFrom%sbioMath.dll" "%pathTo%"
copy /y "%pathFrom%sbioMath.pdb" "%pathTo%"

copy /y "%pathFrom%sbioOneWorldSDK.dll" "%pathTo%"

REM CONVERT PDBs TO MDBs
cd %pathFrom%
pdb2mdb.exe sbioOneWorldSDK.dll
pdb2mdb.exe sbioMath.dll
cd %currentDir%

copy /y "%pathFrom%sbioOneWorldSDK.dll.mdb" "%pathTo%"
copy /y "%pathFrom%sbioMath.dll.mdb" "%pathTo%"
