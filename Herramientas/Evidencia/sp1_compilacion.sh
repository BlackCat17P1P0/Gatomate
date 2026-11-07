#!/bin/bash
# SP1, partes compilación y pruebas. Uso: bash Herramientas/Evidencia/sp1_compilacion.sh <carpetaSalida>
# 1) Compila SOLO el Core contra .NET Standard 2.1 (perfil de API de Unity 2021.2+) con C# 7.3, sin ninguna referencia a UnityEngine.
# 2) Compila el Core contra .NET 8 y corre las comprobaciones de reglas (Herramientas/Simulacion) midiendo el tiempo.
SAL=${1:-resultados}; mkdir -p $SAL
SDK=$(ls -d /usr/lib/dotnet/sdk/*/ | head -1)
NS=$(ls /usr/lib/dotnet/packs/NETStandard.Library.Ref/*/ref/netstandard2.1/netstandard.dll | head -1)
REF=$(ls -d /usr/lib/dotnet/packs/Microsoft.NETCore.App.Ref/*/ref/net8.0 | head -1)
OUT=$SAL/sp1_compilacion.txt
{
echo "# SP1 - compilación y pruebas del Core fuera de Unity"
echo "# Fecha (UTC): $(date -u '+%Y-%m-%d %H:%M:%S')"
echo "# $(uname -a)"
echo "# dotnet: $(dotnet --version)"
echo
echo "== 1. Core contra .NET Standard 2.1, C# 7.3 (sin UnityEngine) =="
mkdir -p /tmp/ev_ns
dotnet ${SDK}Roslyn/bincore/csc.dll -nologo -langversion:7.3 -target:library -noconfig -nostdlib+ -r:$NS -out:/tmp/ev_ns/Gatomate.Core.dll -nowarn:1591 Assets/Scripts/Core/*.cs > /tmp/ev_ns/salida.txt 2>&1
CODIGO=$?
cat /tmp/ev_ns/salida.txt
echo "Código de salida del compilador: $CODIGO (0 = compila)"
echo "Errores: $(grep -c 'error CS' /tmp/ev_ns/salida.txt)   Advertencias: $(grep -c 'warning CS' /tmp/ev_ns/salida.txt)"
echo
echo "== 2. Core contra .NET 8 + comprobaciones de reglas =="
REFS=""; for f in $REF/*.dll; do REFS="$REFS -r:$f"; done
mkdir -p /tmp/ev_sim
dotnet ${SDK}Roslyn/bincore/csc.dll -nologo -optimize+ -langversion:7.3 -target:exe -out:/tmp/ev_sim/Sim.dll -nowarn:1591,0168,0219 $REFS Herramientas/Simulacion/*.cs Assets/Scripts/Core/*.cs > /tmp/ev_sim/salida.txt 2>&1
echo '{ "runtimeOptions": { "tfm": "net8.0", "framework": { "name": "Microsoft.NETCore.App", "version": "8.0.0" }, "configProperties": { "System.Globalization.Invariant": true } } }' > /tmp/ev_sim/Sim.runtimeconfig.json
echo "Errores de compilación: $(grep -c 'error CS' /tmp/ev_sim/salida.txt)"
INI=$(date +%s.%N)
dotnet /tmp/ev_sim/Sim.dll pruebas > /tmp/ev_sim/pruebas.txt 2>&1
FIN=$(date +%s.%N)
tail -3 /tmp/ev_sim/pruebas.txt
echo "Tiempo de las comprobaciones (incluye arrancar el runtime): $(echo "$FIN - $INI" | bc) s"
} | tee $OUT
