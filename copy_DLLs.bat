set currentDir=%cd%
if "%1%"=="release" (set mode=Release& set d=) else (set mode=Debug& set d=d)
set "pathFrom_Managed=%~dp0\Bin\"
set "pathToUnity=%~dp0\src\OneWorldSDK_UnityDemo\Assets\Plugins\"

REM CREATE FOLDERS FOR UNITY
if not exist "%pathToUnity%windows-x64\" mkdir "%pathToUnity%windows-x64\"

REM COPY DEPENDENCIES
copy /y "%pathFrom_Managed%Newtonsoft.Json.dll" "%pathToUnity%"
copy /y "%pathFrom_Managed%Pngcs.dll" "%pathToUnity%"
copy /y "%pathFrom_Managed%gdal_csharp.dll" "%pathToUnity%"
copy /y "%pathFrom_Managed%BitMiracle.LibJpeg.NET.dll" "%pathToUnity%"
copy /y "%pathFrom_Managed%ogr_csharp.dll" "%pathToUnity%"
copy /y "%pathFrom_Managed%x64\SQLite.Interop.dll" "%pathToUnity%windows-x64\"
copy /y "%pathFrom_Managed%DotSpatial.Projections.dll" "%pathToUnity%"
copy /y "%pathFrom_Managed%osr_csharp.dll" "%pathToUnity%"
copy /y "%pathFrom_Managed%OsmSharp.dll" "%pathToUnity%"
copy /y "%pathFrom_Managed%Npgsql.dll" "%pathToUnity%"
copy /y "%pathFrom_Managed%..\src\Libraries\packages\NETStandard.Library.2.0.3\build\netstandard2.0\ref\System.Runtime.InteropServices.RuntimeInformation.dll" "%pathToUnity%"
copy /y "%pathFrom_Managed%..\src\Libraries\packages\protobuf-net.2.3.7\lib\net40\protobuf-net.dll" "%pathToUnity%"
copy /y "%pathFrom_Managed%NetTopologySuite.dll" "%pathToUnity%"
copy /y "%pathFrom_Managed%BruTile.dll" "%pathToUnity%"
copy /y "%pathFrom_Managed%Microsoft.Bcl.AsyncInterfaces.dll" "%pathToUnity%"
copy /y "%pathFrom_Managed%Npgsql.dll" "%pathToUnity%"

copy /y "%pathFrom_Managed%System.Buffers.dll" "%pathToUnity%"
copy /y "%pathFrom_Managed%System.Data.Common.dll" "%pathToUnity%"
copy /y "%pathFrom_Managed%System.Data.SQLite.dll" "%pathToUnity%"
copy /y "%pathFrom_Managed%System.Diagnostics.StackTrace.dll" "%pathToUnity%"
copy /y "%pathFrom_Managed%System.Diagnostics.Tracing.dll" "%pathToUnity%"
copy /y "%pathFrom_Managed%System.Globalization.Extensions.dll" "%pathToUnity%"
copy /y "%pathFrom_Managed%System.IO.Compression.dll" "%pathToUnity%"
copy /y "%pathFrom_Managed%System.Memory.dll" "%pathToUnity%"
copy /y "%pathFrom_Managed%System.Net.Sockets.dll" "%pathToUnity%"
copy /y "%pathFrom_Managed%System.Numerics.Vectors.dll" "%pathToUnity%"
copy /y "%pathFrom_Managed%System.Runtime.CompilerServices.Unsafe.dll" "%pathToUnity%"
copy /y "%pathFrom_Managed%System.Runtime.Serialization.Primitives.dll" "%pathToUnity%"
copy /y "%pathFrom_Managed%System.Text.Encodings.Web.dll" "%pathToUnity%"
copy /y "%pathFrom_Managed%System.Text.Json.dll" "%pathToUnity%"
copy /y "%pathFrom_Managed%System.Threading.Overlapped.dll" "%pathToUnity%"
copy /y "%pathFrom_Managed%System.Threading.Tasks.Extensions.dll" "%pathToUnity%"
copy /y "%pathFrom_Managed%System.ValueTuple.dll" "%pathToUnity%"
copy /y "%pathFrom_Managed%System.Xml.XPath.XDocument.dll" "%pathToUnity%"

if exist "%pathToUnity%System.Net.Http.dll" del "%pathToUnity%System.Net.Http.dll"

copy /y "%pathFrom_Managed%sbioMath.dll" "%pathToUnity%"
copy /y "%pathFrom_Managed%sbioMath.pdb" "%pathToUnity%"

copy /y "%pathFrom_Managed%sbioOneWorldSDK.dll" "%pathToUnity%"