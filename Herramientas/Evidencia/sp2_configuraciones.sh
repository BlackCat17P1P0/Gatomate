#!/bin/bash
# SP2, M1 entre configuraciones: repite la corrida de reproducibilidad en procesos nuevos con otras opciones del compilador JIT
# y compara la huella global. Uso: bash Herramientas/Evidencia/sp2_configuraciones.sh <dll> <carpetaSalida> <raiz>
DLL=$1; SAL=$2; RAIZ=$3
OUT=$SAL/sp2_configuraciones.txt
echo "# SP2 - huella global de 200 runs en distintas configuraciones del runtime (misma maquina)" > $OUT
echo "# Fecha (UTC): $(date -u '+%Y-%m-%d %H:%M:%S')" >> $OUT
i=0
for CFG in "predeterminada" "DOTNET_TieredCompilation=0" "DOTNET_TieredPGO=0" "DOTNET_ReadyToRun=0" "DOTNET_TC_QuickJitForLoops=0"; do
  i=$((i+1)); D=$(mktemp -d)
  if [ "$CFG" = "predeterminada" ]; then dotnet $DLL sp2 $D $RAIZ > /dev/null 2>&1; else env $CFG dotnet $DLL sp2 $D $RAIZ > /dev/null 2>&1; fi
  H1=$(sed -n 1p $D/sp2_huella_global.txt); H2=$(sed -n 2p $D/sp2_huella_global.txt)
  echo "$CFG | huella de las runs: $H1 | huella de los planes: $H2" | tee -a $OUT
  rm -rf $D
done
UNICAS=$(grep -o 'runs: [0-9a-f]*' $OUT | sort -u | wc -l)
echo "Huellas de runs distintas entre configuraciones: $UNICAS (1 = todas iguales)" | tee -a $OUT
