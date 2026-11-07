#!/bin/bash
# Compila la herramienta de evidencia con optimizaciones (como una compilación Release).
# Uso:  bash Herramientas/Evidencia/compilar_evidencia.sh <salida.dll>
# En un PC con NuGet basta:  dotnet run -c Release --project Herramientas/Evidencia -- todo resultados .
OUT=${1:-bin/Evidencia.dll}
SDK=$(ls -d /usr/lib/dotnet/sdk/*/ 2>/dev/null | head -1)
[ -z "$SDK" ] && SDK=$(dirname $(readlink -f $(which dotnet)))/sdk/$(ls $(dirname $(readlink -f $(which dotnet)))/sdk | head -1)/
REF=$(ls -d /usr/lib/dotnet/packs/Microsoft.NETCore.App.Ref/*/ref/net8.0 | head -1)
REFS=""
for f in $REF/*.dll; do REFS="$REFS -r:$f"; done
mkdir -p $(dirname $OUT)
dotnet ${SDK}Roslyn/bincore/csc.dll -nologo -optimize+ -langversion:7.3 -target:exe -out:$OUT -nowarn:1591,0168,0219,0414 $REFS Herramientas/Evidencia/Program.cs Assets/Scripts/Core/*.cs
echo '{ "runtimeOptions": { "tfm": "net8.0", "framework": { "name": "Microsoft.NETCore.App", "version": "8.0.0" }, "configProperties": { "System.Globalization.Invariant": true } } }' > ${OUT%.dll}.runtimeconfig.json
