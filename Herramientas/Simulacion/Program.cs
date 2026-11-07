// Simulación de balance de Gatomate: juega muchas runs con el bot y resume los resultados.
// Uso: dotnet run -c Release -- [runs] [mundos] [4clases]
//      dotnet run -c Release -- pruebas            (comprobaciones de reglas)
//      dotnet run -c Release -- log mundo nivel semilla clase1,clase2,clase3,clase4
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Gatomate.Core;

public static class Program
{
    private sealed class Acum { public int Intentos, Victorias; public long Rondas; }

    public static int Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "log") { var r = new string[args.Length - 1]; Array.Copy(args, 1, r, 0, r.Length); return Prueba.Correr(r); }
        if (args.Length > 0 && args[0] == "pruebas") return Pruebas.Correr();
        int runs = args.Length > 0 ? int.Parse(args[0]) : 200;
        int mundos = args.Length > 1 ? int.Parse(args[1]) : 7;
        bool cuatro = args.Length > 2 && args[2] == "4clases";
        Config.MundosJugables = mundos;
        if (cuatro) Config.ClasesHabilitadas = new HashSet<string> { "luchador", "mago", "cazador", "clerigo" };

        var porNivel = new Dictionary<string, Acum>();
        int completadas = 0, mundoMax = 0; long vidasPerdidas = 0;
        var llegoAMundo = new int[8];
        double sumNivel = 0; int cuentaNivel = 0, conObjetos = 0, totalObjetos = 0;
        var sw = Stopwatch.StartNew();

        for (int s = 1; s <= runs; s++)
        {
            Run r = new Run(s * 7919);
            BotJugador.JugarRun(r);
            foreach (Gato gg in r.Expedicion) { sumNivel += gg.Nivel; cuentaNivel++; totalObjetos += gg.Equipo.Count; if (gg.Equipo.Count > 0) conObjetos++; }
            if (r.Fase == FaseRun.CampanaCompletada) completadas++;
            vidasPerdidas += Config.VidasPorRun - Math.Max(0, r.Vidas);
            int mmax = 0;
            foreach (RegistroNivel g in r.Historial)
            {
                string k = g.Mundo + "-" + g.Nivel;
                Acum a;
                if (!porNivel.TryGetValue(k, out a)) { a = new Acum(); porNivel[k] = a; }
                a.Intentos++;
                if (g.Resultado == "victoria") { a.Victorias++; a.Rondas += g.Rondas; }
                mmax = Math.Max(mmax, g.Mundo);
            }
            for (int m = 1; m <= mmax; m++) llegoAMundo[m]++;
            mundoMax += mmax;
        }

        var sb = new StringBuilder();
        sb.AppendLine("Runs: " + runs + "  mundos jugables: " + mundos + (cuatro ? "  (4 clases)" : "  (8 clases)") + "  tiempo: " + sw.ElapsedMilliseconds + " ms");
        sb.AppendLine("Campañas completadas: " + completadas + " (" + (100.0 * completadas / runs).ToString("0.0") + "%)");
        sb.AppendLine("Vidas perdidas por run (media): " + ((double)vidasPerdidas / runs).ToString("0.00"));
        sb.AppendLine("Mundo alcanzado (media): " + ((double)mundoMax / runs).ToString("0.00"));
        sb.AppendLine("Nivel medio de la expedición al terminar: " + (sumNivel / Math.Max(1, cuentaNivel)).ToString("0.0") + "; objetos por gato: " + ((double)totalObjetos / Math.Max(1, cuentaNivel)).ToString("0.00"));
        sb.AppendLine("Nivel   intentos  victorias  % victoria  rondas (victorias)");
        for (int m = 1; m <= mundos; m++)
            for (int n = 1; n <= 3; n++)
            {
                Acum a;
                if (!porNivel.TryGetValue(m + "-" + n, out a)) continue;
                double rond = a.Victorias > 0 ? (double)a.Rondas / a.Victorias : 0;
                sb.AppendLine(m + "-" + n + "     " + a.Intentos.ToString().PadLeft(8) + a.Victorias.ToString().PadLeft(11) + (100.0 * a.Victorias / a.Intentos).ToString("0.0").PadLeft(12) + rond.ToString("0.0").PadLeft(12));
            }
        Console.Write(sb.ToString());
        return 0;
    }
}
