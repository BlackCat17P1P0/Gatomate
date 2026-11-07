#!/bin/bash
# Compila con Roslyn directamente (sin NuGet). Uso: Herramientas/compilar.sh <salida.dll> <archivos.cs...>
# Útil cuando `dotnet build` no puede restaurar paquetes. En un PC normal basta con `dotnet build`.
OUT=$1; shift
SDK=$(ls -d /usr/lib/dotnet/sdk/*/ | head -1)
REF=$(ls -d /usr/lib/dotnet/packs/Microsoft.NETCore.App.Ref/*/ref/net8.0 | head -1)
REFS=""
for f in $REF/*.dll; do REFS="$REFS -r:$f"; done
mkdir -p $(dirname $OUT)
dotnet ${SDK}Roslyn/bincore/csc.dll -nologo -debug:embedded -langversion:7.3 -target:${TARGET:-exe} -out:$OUT -nowarn:1591 $REFS "$@"
echo '{ "runtimeOptions": { "tfm": "net8.0", "framework": { "name": "Microsoft.NETCore.App", "version": "8.0.0" }, "configProperties": { "System.Globalization.Invariant": true } } }' > ${OUT%.dll}.runtimeconfig.json
