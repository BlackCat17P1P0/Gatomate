// Gatomate - herramienta de evidencia para el PoC / Spikes / ADR (Fases 4 y 7).
// Cada spike genera archivos CSV y un registro de corrida (Run Log) en la carpeta de salida, para que un tercero pueda repetirlo.
//
// Uso:   dotnet Evidencia.dll <spike> <carpetaSalida> [raizDelProyecto]
//   sp1  aislamiento del Core (análisis estático del código)
//   sp2  azar determinista (reproducibilidad, restauración, calidad, velocidad, interferencia simulada)
//   sp3  combate por eventos (completitud del registro de eventos, sobrecosto, invariancia, determinismo)
//   sp4  presupuesto de tiempo (latencia de las llamadas del Core que bloquean el hilo principal)
//   sp5  guardado y reanudación (idempotencia y equivalencia de continuar)
//   e2e  escenarios de punta a punta sin Unity (Fase 7)
//   todo ejecuta todos
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Gatomate.Core;

public static class Program
{
    private static string _sal = ".";
    private static string _raiz = ".";
    private static readonly CultureInfo CI = CultureInfo.InvariantCulture;

    public static int Main(string[] args)
    {
        string que = args.Length > 0 ? args[0] : "todo";
        _sal = args.Length > 1 ? args[1] : "resultados";
        _raiz = args.Length > 2 ? args[2] : ".";
        Directory.CreateDirectory(_sal);
        switch (que)
        {
            case "sp1": Sp1.Correr(_sal, _raiz); break;
            case "sp2": Sp2.Correr(_sal); break;
            case "sp3": Sp3.Correr(_sal); break;
            case "sp4": Sp4.Correr(_sal); break;
            case "sp5": Sp5.Correr(_sal); break;
            case "e2e": E2e.Correr(_sal); break;
            case "todo":
                Sp1.Correr(_sal, _raiz); Sp2.Correr(_sal); Sp3.Correr(_sal); Sp4.Correr(_sal); Sp5.Correr(_sal); E2e.Correr(_sal);
                break;
            default: Console.WriteLine("spike desconocido: " + que); return 1;
        }
        return 0;
    }
}

// ======================================================================== utilidades comunes
public static class U
{
    public static readonly CultureInfo CI = CultureInfo.InvariantCulture;

    public static string Hash(string s)
    {
        ulong h = 14695981039346656037UL;
        for (int i = 0; i < s.Length; i++) { h ^= s[i]; h *= 1099511628211UL; }
        return h.ToString("x16");
    }

    public static double Pct(List<double> v, double p)
    {
        if (v.Count == 0) return double.NaN;
        var a = v.ToArray(); Array.Sort(a);
        double k = (a.Length - 1) * p; int lo = (int)Math.Floor(k), hi = (int)Math.Ceiling(k);
        return a[lo] + (a[hi] - a[lo]) * (k - lo);
    }
    public static double Media(List<double> v) { double s = 0; foreach (double x in v) s += x; return v.Count == 0 ? double.NaN : s / v.Count; }
    public static double Desv(List<double> v)
    {
        if (v.Count < 2) return 0; double m = Media(v), s = 0; foreach (double x in v) s += (x - m) * (x - m); return Math.Sqrt(s / (v.Count - 1));
    }
    public static double Max(List<double> v) { double m = double.NaN; foreach (double x in v) if (double.IsNaN(m) || x > m) m = x; return m; }
    public static string F(double d, string f = "0.000") { return d.ToString(f, CI); }

    public static string Maquina()
    {
        string cpu = "";
        try
        {
            if (File.Exists("/proc/cpuinfo"))
                foreach (string l in File.ReadLines("/proc/cpuinfo")) if (l.StartsWith("model name")) { cpu = l.Substring(l.IndexOf(':') + 1).Trim(); break; }
        }
        catch { }
        return "SO: " + System.Runtime.InteropServices.RuntimeInformation.OSDescription + " | CPU: " + (cpu.Length > 0 ? cpu : "(sin dato)") + " | nucleos: " + Environment.ProcessorCount +
               " | runtime: " + System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription + " | arq: " + System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture;
    }

    public static StreamWriter Log(string dir, string nombre, string titulo)
    {
        var w = new StreamWriter(Path.Combine(dir, nombre), false, new UTF8Encoding(false));
        w.WriteLine("# " + titulo);
        w.WriteLine("# Fecha (UTC): " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CI));
        w.WriteLine("# " + Maquina());
        w.WriteLine("# Compilación optimizada: " + (IsOptimized() ? "sí" : "no") + " | Config.Medidor: " + (Config.Medidor != null ? "encendido" : "apagado"));
        w.WriteLine();
        return w;
    }

    private static bool IsOptimized()
    {
        var a = typeof(U).Assembly;
        var d = (System.Diagnostics.DebuggableAttribute)Attribute.GetCustomAttribute(a, typeof(System.Diagnostics.DebuggableAttribute));
        return d == null || !d.IsJITOptimizerDisabled;
    }

    public static void Dos(StreamWriter w, string s) { w.WriteLine(s); Console.WriteLine(s); w.Flush(); }

    // chi-cuadrado: p-valor = Q(df/2, x/2) con la gamma incompleta regularizada
    public static double ChiP(double x, int df)
    {
        if (x <= 0) return 1;
        return GammaQ(df / 2.0, x / 2.0);
    }
    private static double GammaQ(double a, double x)
    {
        if (x < a + 1) return 1 - GammaPSerie(a, x);
        return GammaQFrac(a, x);
    }
    private static double LnGamma(double z)
    {
        double[] c = { 76.18009172947146, -86.50532032941677, 24.01409824083091, -1.231739572450155, 0.1208650973866179e-2, -0.5395239384953e-5 };
        double y = z, t = z + 5.5; t -= (z + 0.5) * Math.Log(t); double s = 1.000000000190015;
        for (int j = 0; j < 6; j++) s += c[j] / ++y;
        return -t + Math.Log(2.5066282746310005 * s / z);
    }
    private static double GammaPSerie(double a, double x)
    {
        double ap = a, sum = 1.0 / a, del = sum;
        for (int n = 0; n < 500; n++) { ap += 1; del *= x / ap; sum += del; if (Math.Abs(del) < Math.Abs(sum) * 1e-14) break; }
        return sum * Math.Exp(-x + a * Math.Log(x) - LnGamma(a));
    }
    private static double GammaQFrac(double a, double x)
    {
        double b = x + 1 - a, c = 1 / 1e-300, d = 1 / b, h = d;
        for (int i = 1; i < 500; i++)
        {
            double an = -i * (i - a); b += 2;
            d = an * d + b; if (Math.Abs(d) < 1e-300) d = 1e-300;
            c = b + an / c; if (Math.Abs(c) < 1e-300) c = 1e-300;
            d = 1 / d; double del = d * c; h *= del;
            if (Math.Abs(del - 1) < 1e-14) break;
        }
        return Math.Exp(-x + a * Math.Log(x) - LnGamma(a)) * h;
    }

    // ---- huellas de estado (para comparar corridas)
    public static string HashPlan(GeneradorNivel.PlanNivel p)
    {
        var sb = new StringBuilder();
        sb.Append(p.Tab.Ancho).Append('x').Append(p.Tab.Alto).Append('d').Append(p.Disposicion).Append('|');
        for (int y = 0; y < p.Tab.Alto; y++) for (int x = 0; x < p.Tab.Ancho; x++) sb.Append((int)p.Tab.TerrenoEn(new Pos(x, y))).Append(',');
        sb.Append('|');
        foreach (Pos q in p.PosGatos) sb.Append(q).Append(';');
        for (int i = 0; i < p.Enemigos.Count; i++) sb.Append(p.Enemigos[i].Id).Append(p.PosEnemigos[i]).Append(';');
        return Hash(sb.ToString());
    }

    public static string ResumenRun(Run r)
    {
        var sb = new StringBuilder();
        sb.Append(r.Fase).Append('|').Append(r.Vidas).Append('|').Append(r.Dinero).Append('|').Append(r.Mundo).Append('-').Append(r.Nivel).Append('|');
        foreach (RegistroNivel g in r.Historial) sb.Append(g.Mundo).Append('-').Append(g.Nivel).Append(g.Resultado[0]).Append(g.Rondas).Append(',');
        sb.Append('|');
        foreach (Gato g in r.Expedicion) sb.Append(g.ClaseId).Append(g.Nivel).Append('/').Append(g.Vida).Append('/').Append(g.Xp).Append(';');
        foreach (Gato g in r.Reserva) sb.Append("R").Append(g.ClaseId).Append(g.Nivel).Append('/').Append(g.Vida).Append(';');
        return sb.ToString();
    }

    public static string HashEventos(List<Evento> evs)
    {
        ulong h = 14695981039346656037UL;
        foreach (Evento e in evs)
        {
            string s = e.Tipo + ":" + (e.U != null ? e.U.Id : -1) + ":" + (e.Obj != null ? e.Obj.Id : -1) + ":" + e.A + e.B + ":" + e.N + ":" + e.Txt + ":" + e.Elem + ":" + (e.Hab != null ? e.Hab.Id : "") + ";";
            for (int i = 0; i < s.Length; i++) { h ^= s[i]; h *= 1099511628211UL; }
        }
        return h.ToString("x16");
    }

    /// <summary>Juega la run completa con el bot (igual que BotJugador.JugarRun) y avisa tras cada combate.</summary>
    public static void JugarRun(Run r, Action<Run> trasCombate, Action<Run> antesDeEmpezar = null)
    {
        int guardia = 0;
        while (r.Fase != FaseRun.RunPerdida && r.Fase != FaseRun.CampanaCompletada && guardia++ < 1000)
        {
            switch (r.Fase)
            {
                case FaseRun.ElegirHabilidad: BotJugador.ElegirHabilidades(r); break;
                case FaseRun.Tienda: BotJugador.Comprar(r); r.SalirDeTienda(); break;
                case FaseRun.Preparacion: BotJugador.Equipar(r); if (antesDeEmpezar != null) antesDeEmpezar(r); r.Empezar(); break;
                case FaseRun.Combate:
                    BotJugador.JugarCombate(r.Combate);
                    if (trasCombate != null) trasCombate(r);
                    r.ResolverFinDeCombate();
                    break;
                case FaseRun.Recompensa: BotJugador.ElegirDesafio(r); break;
            }
        }
    }
}

// ======================================================================== SP1: aislamiento del Core
public static class Sp1
{
    private static readonly string[] Patrones = { "UnityEngine", "MonoBehaviour", "ScriptableObject", "GameObject", "Mathf.", "Debug.Log", "Time.deltaTime", "Input.", "PlayerPrefs", "Application.", "Resources.Load", "Coroutine", "IEnumerator" };

    public static void Correr(string sal, string raiz)
    {
        var w = U.Log(sal, "sp1_runlog.txt", "SP1 - Aislamiento del Core (análisis estático del código fuente)");
        string core = Path.Combine(raiz, "Assets", "Scripts", "Core"), view = Path.Combine(raiz, "Assets", "Scripts", "View");
        if (!Directory.Exists(core)) { U.Dos(w, "No encuentro " + core + ". Pasa la raíz del proyecto como tercer argumento."); w.Close(); return; }

        var csv = new StringBuilder("capa;archivo;lineas;" + string.Join(";", Patrones) + ";usa_Gatomate_Core;usa_Gatomate_View;usa_UnityEngine_Random\n");
        var tot = new Dictionary<string, int>();
        var resumen = new Dictionary<string, int[]>();           // capa -> {archivos, lineas, archivosConUnity, ocurrenciasUnity}
        var usings = new SortedDictionary<string, int>();
        int coreRefView = 0, viewRefCore = 0, viewRandom = 0, coreRandomUnity = 0;
        foreach (string capa in new[] { "Core", "View" })
        {
            string dir = capa == "Core" ? core : view;
            var r = new int[4]; resumen[capa] = r;
            foreach (string f in Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories))
            {
                string[] ls = File.ReadAllLines(f);
                string txt = string.Join("\n", ls);
                r[0]++; r[1] += ls.Length;
                var fila = new StringBuilder(capa + ";" + Path.GetFileName(f) + ";" + ls.Length);
                int unityFile = 0;
                foreach (string p in Patrones)
                {
                    int n = 0; int ix = 0;
                    while ((ix = txt.IndexOf(p, ix, StringComparison.Ordinal)) >= 0) { n++; ix += p.Length; }
                    fila.Append(';').Append(n);
                    if (p == "UnityEngine") unityFile += n;
                }
                bool usaCore = txt.Contains("Gatomate.Core"), usaView = txt.Contains("Gatomate.View");
                int rnd = Regex.Matches(txt, @"\bRandom\.(Range|value|InitState|insideUnitCircle)").Count;
                fila.Append(';').Append(usaCore ? 1 : 0).Append(';').Append(usaView ? 1 : 0).Append(';').Append(rnd);
                csv.AppendLine(fila.ToString());
                if (unityFile > 0) { r[2]++; r[3] += unityFile; }
                if (capa == "Core") { if (usaView) coreRefView++; coreRandomUnity += rnd; }
                else { if (usaCore) viewRefCore++; viewRandom += rnd; }
                foreach (Match m in Regex.Matches(txt, @"^\s*using\s+([\w\.]+)\s*;", RegexOptions.Multiline))
                {
                    if (capa != "Core") continue;
                    int c; usings.TryGetValue(m.Groups[1].Value, out c); usings[m.Groups[1].Value] = c + 1;
                }
            }
        }
        File.WriteAllText(Path.Combine(sal, "sp1_archivos.csv"), csv.ToString(), new UTF8Encoding(false));

        U.Dos(w, "== Tamaño y referencias a Unity por capa ==");
        foreach (var kv in resumen)
            U.Dos(w, kv.Key + ": " + kv.Value[0] + " archivos, " + kv.Value[1] + " líneas; archivos que mencionan UnityEngine: " + kv.Value[2] + "; ocurrencias de 'UnityEngine': " + kv.Value[3]);
        U.Dos(w, "Core -> View (archivos del Core que citan Gatomate.View): " + coreRefView);
        U.Dos(w, "View -> Core (archivos de la vista que usan Gatomate.Core): " + viewRefCore + " de " + resumen["View"][0]);
        U.Dos(w, "Core: usos de UnityEngine.Random (Random.Range / Random.value ...): " + coreRandomUnity);
        U.Dos(w, "View: usos de UnityEngine.Random: " + viewRandom);
        U.Dos(w, "== Espacios de nombres que importa el Core (using) ==");
        foreach (var kv in usings) U.Dos(w, "  " + kv.Key + "  (" + kv.Value + " archivos)");

        // estado global mutable del Core (riesgo para la reproducibilidad): campos static no readonly / no const
        U.Dos(w, "== Campos estáticos mutables del Core (estado global) ==");
        int glob = 0;
        foreach (string f in Directory.GetFiles(core, "*.cs"))
        {
            string[] ls = File.ReadAllLines(f);
            for (int i = 0; i < ls.Length; i++)
            {
                string l = ls[i];
                if (Regex.IsMatch(l, @"^\s*(public|internal|private)\s+static\s+(?!readonly|class|void|partial)(?!.*\()[^=;]*[=;]") && !l.Contains(" const ") && !l.Contains("readonly") && !l.Contains("=>") && !l.Contains("(") )
                { U.Dos(w, "  " + Path.GetFileName(f) + ":" + (i + 1) + "  " + l.Trim()); glob++; }
            }
        }
        U.Dos(w, "Total campos estáticos mutables: " + glob);

        // cobertura de eventos: todo EvTipo tiene un 'case EvTipo.X' en la vista
        string ev = File.ReadAllText(Path.Combine(core, "Eventos.cs"));
        var tipos = new List<string>();
        Match en = Regex.Match(ev, @"enum EvTipo\s*\{([^}]*)\}", RegexOptions.Singleline);
        foreach (Match m in Regex.Matches(en.Groups[1].Value, @"^\s*(\w+)\s*,?", RegexOptions.Multiline)) tipos.Add(m.Groups[1].Value);
        string vtxt = "";
        foreach (string f in Directory.GetFiles(view, "*.cs")) vtxt += File.ReadAllText(f) + "\n";
        int cub = 0; var faltan = new List<string>();
        foreach (string t in tipos) { if (vtxt.Contains("EvTipo." + t)) cub++; else faltan.Add(t); }
        U.Dos(w, "== Cobertura de eventos en la vista ==");
        U.Dos(w, "Tipos de evento del Core: " + tipos.Count + "; con manejador en la vista: " + cub + (faltan.Count > 0 ? "; sin manejador: " + string.Join(", ", faltan) : ""));
        File.WriteAllText(Path.Combine(sal, "sp1_resumen.csv"),
            "metrica;valor\n" +
            "core_archivos;" + resumen["Core"][0] + "\ncore_lineas;" + resumen["Core"][1] + "\ncore_archivos_con_UnityEngine;" + resumen["Core"][2] + "\ncore_ocurrencias_UnityEngine;" + resumen["Core"][3] + "\n" +
            "view_archivos;" + resumen["View"][0] + "\nview_lineas;" + resumen["View"][1] + "\nview_archivos_con_UnityEngine;" + resumen["View"][2] + "\n" +
            "core_cita_view;" + coreRefView + "\nview_usa_core;" + viewRefCore + "\ncore_UnityEngine_Random;" + coreRandomUnity + "\nview_UnityEngine_Random;" + viewRandom + "\n" +
            "core_estaticos_mutables;" + glob + "\neventos_total;" + tipos.Count + "\neventos_con_manejador_en_vista;" + cub + "\n", new UTF8Encoding(false));
        w.Close();
    }
}

// ======================================================================== SP2: azar determinista
public static class Sp2
{
    public static void Correr(string sal)
    {
        var w = U.Log(sal, "sp2_runlog.txt", "SP2 - Azar determinista: A1 = Azar propio (xorshift32 por instancia). A2 = generador compartido; se mide con System.Random como sustituto porque UnityEngine.Random no existe fuera de Unity (este último sí expone Random.state según la documentación; ver evidencia documental E12)");
        Config.GrabarEventos = false;

        // ---- M1: reproducibilidad (A1, juego real)
        U.Dos(w, "== M1. Reproducibilidad del juego real con A1 ==");
        int nSem = 1000, igualPlan = 0;
        var hashes = new StringBuilder("semilla;mundo;nivel;hash_plan\n");
        for (int s = 1; s <= nSem; s++)
        {
            int m = 1 + s % 7, n = 1 + s % 3;
            string a = U.HashPlan(GeneradorNivel.Planear(m, n, s)), b = U.HashPlan(GeneradorNivel.Planear(m, n, s));
            if (a == b) igualPlan++;
            hashes.Append(s).Append(';').Append(m).Append(';').Append(n).Append(';').Append(a).Append('\n');
        }
        U.Dos(w, "Planear(mundo,nivel,semilla) dos veces: iguales " + igualPlan + " de " + nSem);
        int nRun = 200, igualRun = 0;
        var hr = new StringBuilder("semilla_run;hash_resumen\n"); string global = "";
        for (int s = 1; s <= nRun; s++)
        {
            Run r1 = new Run(s * 7919); BotJugador.JugarRun(r1);
            Run r2 = new Run(s * 7919); BotJugador.JugarRun(r2);
            string a = U.Hash(U.ResumenRun(r1)), b = U.Hash(U.ResumenRun(r2));
            if (a == b) igualRun++;
            hr.Append(s * 7919).Append(';').Append(a).Append('\n'); global += a;
        }
        U.Dos(w, "Run completa con el bot dos veces: iguales " + igualRun + " de " + nRun);
        U.Dos(w, "Huella global de las " + nRun + " runs: " + U.Hash(global) + "   (compárala entre procesos y configuraciones del runtime)");
        File.WriteAllText(Path.Combine(sal, "sp2_hashes_planes.csv"), hashes.ToString(), new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(sal, "sp2_hashes_runs.csv"), hr.ToString(), new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(sal, "sp2_huella_global.txt"), U.Hash(global) + "\n" + U.Hash(hashes.ToString()) + "\n");

        // ---- M2: restaurar el estado
        U.Dos(w, "== M2. Restaurar el estado del generador tras k tiradas (A2 = System.Random, que no expone su estado) ==");
        var csv2 = new StringBuilder("alternativa;k;igual;tiempo_restaurar_ms\n");
        foreach (int k in new[] { 1000, 100000, 1000000, 10000000 })
        {
            var a1 = new Azar(42); for (int i = 0; i < k; i++) a1.Entre(0, 100);
            uint est = a1.Estado; var ref1 = new int[1000]; for (int i = 0; i < 1000; i++) ref1[i] = a1.Entre(0, 100);
            var sw = Stopwatch.StartNew();
            var b1 = new Azar(0); b1.Estado = est;
            sw.Stop(); double t1 = sw.Elapsed.TotalMilliseconds;
            bool ok1 = true; for (int i = 0; i < 1000; i++) if (b1.Entre(0, 100) != ref1[i]) ok1 = false;
            csv2.Append("A1;").Append(k).Append(';').Append(ok1 ? 1 : 0).Append(';').Append(U.F(t1, "0.0000")).Append('\n');

            var a2 = new Random(42); for (int i = 0; i < k; i++) a2.Next(0, 100);
            var ref2 = new int[1000];
            var copia = new Random(42); // System.Random no expone su estado: hay que repetir las k tiradas desde la semilla
            // referencia: continuar a2
            for (int i = 0; i < 1000; i++) ref2[i] = a2.Next(0, 100);
            sw.Restart();
            var b2 = new Random(42); for (int i = 0; i < k; i++) b2.Next(0, 100);
            sw.Stop(); double t2 = sw.Elapsed.TotalMilliseconds;
            bool ok2 = true; for (int i = 0; i < 1000; i++) if (b2.Next(0, 100) != ref2[i]) ok2 = false;
            csv2.Append("A2;").Append(k).Append(';').Append(ok2 ? 1 : 0).Append(';').Append(U.F(t2, "0.0000")).Append('\n');
            U.Dos(w, "k=" + k + ": A1 restaurar " + U.F(t1, "0.0000") + " ms (igual=" + ok1 + ")   A2 restaurar " + U.F(t2, "0.00") + " ms (igual=" + ok2 + ", exige contar y repetir las tiradas)");
        }
        File.WriteAllText(Path.Combine(sal, "sp2_restaurar.csv"), csv2.ToString(), new UTF8Encoding(false));

        // ---- M3: interferencia de tiradas cosméticas (SIMULADA)
        U.Dos(w, "== M3. Interferencia de la vista sobre la lógica (simulación del mecanismo, no del juego) ==");
        int semillas = 1000, L = 500, difA1 = 0, difA2 = 0;
        for (int s = 1; s <= semillas; s++)
        {
            var seqX = new int[L]; var seqY = new int[L];
            // A1: la lógica usa su Azar; la vista usa otro generador distinto -> las tiradas cosméticas no tocan a la lógica
            var lx = new Azar(s); var ly = new Azar(s); var vx = new Random(1); var vy = new Random(2);
            for (int i = 0; i < L; i++) { seqX[i] = lx.Entre(0, 100); for (int j = 0; j < 1; j++) vx.Next(); seqY[i] = ly.Entre(0, 100); for (int j = 0; j < 3; j++) vy.Next(); }
            bool dif = false; for (int i = 0; i < L; i++) if (seqX[i] != seqY[i]) dif = true;
            if (dif) difA1++;
            // A2: un único generador compartido; la "máquina X" hace 1 tirada cosmética por tirada lógica y la "máquina Y" hace 3
            var gx = new Random(s); var gy = new Random(s);
            for (int i = 0; i < L; i++) { seqX[i] = gx.Next(0, 100); for (int j = 0; j < 1; j++) gx.Next(); seqY[i] = gy.Next(0, 100); for (int j = 0; j < 3; j++) gy.Next(); }
            dif = false; for (int i = 0; i < L; i++) if (seqX[i] != seqY[i]) dif = true;
            if (dif) difA2++;
        }
        U.Dos(w, "Misma semilla, distinta cantidad de tiradas cosméticas: secuencia lógica distinta en A1 " + difA1 + " de " + semillas + "; en A2 " + difA2 + " de " + semillas);
        File.WriteAllText(Path.Combine(sal, "sp2_interferencia.csv"), "alternativa;semillas;secuencias_distintas\nA1;" + semillas + ";" + difA1 + "\nA2;" + semillas + ";" + difA2 + "\n", new UTF8Encoding(false));

        // ---- M4: calidad estadística
        U.Dos(w, "== M4. Uniformidad (chi-cuadrado, alfa 0,05) ==");
        var csv4 = new StringBuilder("prueba;alternativa;n;categorias;chi2;p_valor;pasa\n");
        Action<string, string, int, int, Func<int>> chi = delegate (string prueba, string alt, int n, int cat, Func<int> f)
        {
            var obs = new long[cat]; for (int i = 0; i < n; i++) obs[f()]++;
            double e = (double)n / cat, x2 = 0; for (int i = 0; i < cat; i++) x2 += (obs[i] - e) * (obs[i] - e) / e;
            double p = U.ChiP(x2, cat - 1);
            csv4.Append(prueba).Append(';').Append(alt).Append(';').Append(n).Append(';').Append(cat).Append(';').Append(U.F(x2, "0.00")).Append(';').Append(U.F(p, "0.0000")).Append(';').Append(p >= 0.05 ? 1 : 0).Append('\n');
            U.Dos(w, prueba + " " + alt + ": chi2=" + U.F(x2, "0.00") + " p=" + U.F(p, "0.0000") + (p >= 0.05 ? " (pasa)" : " (NO pasa)"));
        };
        { var a = new Azar(12345); chi("Entre(0,10) 1e6 tiradas", "A1", 1000000, 10, delegate { return a.Entre(0, 10); }); }
        { var a = new Random(12345); chi("Entre(0,10) 1e6 tiradas", "A2", 1000000, 10, delegate { return a.Next(0, 10); }); }
        { var a = new Azar(777); chi("dado 1d6 1e6 tiradas", "A1", 1000000, 6, delegate { return a.Entre(0, 6); }); }
        { var a = new Random(777); chi("dado 1d6 1e6 tiradas", "A2", 1000000, 6, delegate { return a.Next(0, 6); }); }
        { int sem = 0; chi("primera tirada de semillas consecutivas 1..1e5", "A1", 100000, 10, delegate { sem++; return new Azar(sem).Entre(0, 10); }); }
        { int sem = 0; chi("primera tirada de semillas consecutivas 1..1e5", "A2", 100000, 10, delegate { sem++; return new Random(sem).Next(0, 10); }); }
        // independencia entre semillas vecinas: pares (primera tirada de la semilla s, primera tirada de la semilla s+1) en 10x10 categorías
        foreach (string alt in new[] { "A1", "A2" })
        {
            var obs = new long[100]; int N = 200000;
            Func<int, int> primera = delegate (int sm) { return alt == "A1" ? new Azar(sm).Entre(0, 10) : new Random(sm).Next(0, 10); };
            for (int sm = 1; sm <= N; sm++) obs[primera(sm) * 10 + primera(sm + 1)]++;
            double e = (double)N / 100, x2 = 0; for (int i = 0; i < 100; i++) x2 += (obs[i] - e) * (obs[i] - e) / e;
            double p = U.ChiP(x2, 99);
            csv4.Append("pares de semillas vecinas (independencia)").Append(';').Append(alt).Append(';').Append(N).Append(';').Append(100).Append(';').Append(U.F(x2, "0.00")).Append(';').Append(U.F(p, "0.0000")).Append(';').Append(p >= 0.05 && p <= 0.95 ? 1 : 0).Append('\n');
            U.Dos(w, "pares de semillas vecinas " + alt + ": chi2=" + U.F(x2, "0.00") + " p=" + U.F(p, "0.0000") + (p >= 0.05 && p <= 0.95 ? " (pasa)" : " (NO pasa: demasiado desigual o demasiado regular)"));
        }
        File.WriteAllText(Path.Combine(sal, "sp2_calidad.csv"), csv4.ToString(), new UTF8Encoding(false));

        // ---- M5: velocidad
        U.Dos(w, "== M5. Velocidad (ns por tirada, 5e7 tiradas, mejor de 3) ==");
        double mejor1 = double.MaxValue, mejor2 = double.MaxValue; long sink = 0;
        for (int rep = 0; rep < 3; rep++)
        {
            var a = new Azar(1); var sw = Stopwatch.StartNew(); for (int i = 0; i < 50000000; i++) sink += a.Entre(0, 100); sw.Stop(); mejor1 = Math.Min(mejor1, sw.Elapsed.TotalMilliseconds * 1e6 / 5e7);
            var b = new Random(1); sw.Restart(); for (int i = 0; i < 50000000; i++) sink += b.Next(0, 100); sw.Stop(); mejor2 = Math.Min(mejor2, sw.Elapsed.TotalMilliseconds * 1e6 / 5e7);
        }
        U.Dos(w, "A1 " + U.F(mejor1, "0.00") + " ns/tirada   A2 " + U.F(mejor2, "0.00") + " ns/tirada   (suma de control " + sink + ")");
        File.WriteAllText(Path.Combine(sal, "sp2_velocidad.csv"), "alternativa;ns_por_tirada\nA1;" + U.F(mejor1, "0.00") + "\nA2;" + U.F(mejor2, "0.00") + "\n", new UTF8Encoding(false));

        // ---- M6: colisiones de Azar.Derivar
        U.Dos(w, "== M6. Colisiones de las semillas derivadas Azar.Derivar(semilla, mundo, nivel) ==");
        var vistos = new HashSet<int>(); int tripl = 0;
        for (int s = 1; s <= 200000; s++) for (int m = 1; m <= 7; m++) for (int n = 1; n <= 3; n++) { tripl++; vistos.Add(Azar.Derivar(s * 7919, m, n)); }
        U.Dos(w, "Triples probados: " + tripl + "; valores distintos: " + vistos.Count + "; colisiones: " + (tripl - vistos.Count));
        File.WriteAllText(Path.Combine(sal, "sp2_derivar.csv"), "triples;distintos;colisiones\n" + tripl + ";" + vistos.Count + ";" + (tripl - vistos.Count) + "\n", new UTF8Encoding(false));

        File.WriteAllText(Path.Combine(sal, "sp2_resumen.csv"),
            "metrica;valor\nplanes_iguales;" + igualPlan + "\nplanes_total;" + nSem + "\nruns_iguales;" + igualRun + "\nruns_total;" + nRun + "\n" +
            "interferencia_A1;" + difA1 + "\ninterferencia_A2;" + difA2 + "\ninterferencia_total;" + semillas + "\nns_A1;" + U.F(mejor1, "0.00") + "\nns_A2;" + U.F(mejor2, "0.00") + "\n", new UTF8Encoding(false));
        w.Close();
    }
}

// ======================================================================== SP3: combate por eventos
public static class Sp3
{
    private sealed class Comp { public int Combates, HpOk, MuerteOk, PosOk, Unidades, UnidadesPos; public long Eventos; }

    public static void Correr(string sal)
    {
        var w = U.Log(sal, "sp3_runlog.txt", "SP3 - Combate por eventos: el Core resuelve al instante y la vista anima el registro (A1) frente a lógica sincronizada con animaciones (A2, no medible fuera de Unity)");
        Config.GrabarEventos = true;
        int nRun = 300;
        var c = new Comp();
        var porTipo = new Dictionary<EvTipo, long>();
        var evPorCombate = new List<double>();
        var fallos = new StringBuilder("semilla;mundo;nivel;campo;unidad;esperado;obtenido\n");
        int nFallos = 0;

        for (int s = 1; s <= nRun; s++)
        {
            Run r = new Run(s * 104729);
            var vidaIni = new Dictionary<Gato, int>();
            U.JugarRun(r, delegate (Run rr)
            {
                Combate cb = rr.Combate;
                c.Combates++;
                evPorCombate.Add(cb.Eventos.Count);
                c.Eventos += cb.Eventos.Count;
                foreach (Evento e in cb.Eventos) { long n; porTipo.TryGetValue(e.Tipo, out n); porTipo[e.Tipo] = n + 1; }
                // reconstrucción: estado inicial de cada unidad = el del gemelo creado con las mismas entradas (el combate real ya avanzó)
                // Para no depender del gemelo usamos: vida inicial de un gato = vida al empezar el nivel (se guardó en la run) y de un enemigo = su vida máxima.
                var hp = new Dictionary<int, int>(); var pos = new Dictionary<int, Pos>(); var muerto = new HashSet<int>(); var invocadas = new HashSet<int>();
                foreach (Unidad u in cb.Unidades) { hp[u.Id] = u.EsGato ? Math.Max(1, Math.Min(vidaIni[u.Gato], u.Gato.Stats.Vida)) : u.VidaMax; }
                // posiciones iniciales: se obtienen de los eventos (primer origen) o, si la unidad nunca se mueve, la final
                var primerOrigen = new Dictionary<int, Pos>();
                foreach (Evento e in cb.Eventos)
                {
                    Unidad q = e.U ?? e.Obj;
                    if (q == null) continue;
                    if ((e.Tipo == EvTipo.Mover || e.Tipo == EvTipo.Empuje || e.Tipo == EvTipo.Teletransporte || e.Tipo == EvTipo.Embestida || e.Tipo == EvTipo.Salto) && !primerOrigen.ContainsKey(q.Id)) primerOrigen[q.Id] = e.A;
                }
                foreach (Unidad u in cb.Unidades) { Pos p; pos[u.Id] = primerOrigen.TryGetValue(u.Id, out p) ? p : u.Pos; }
                foreach (Evento e in cb.Eventos)
                {
                    switch (e.Tipo)
                    {
                        case EvTipo.Dano: hp[e.Obj.Id] -= e.N; break;
                        case EvTipo.Cura: hp[e.Obj.Id] += e.N; break;
                        case EvTipo.Muerte: muerto.Add(e.U.Id); hp[e.U.Id] = 0; break;
                        case EvTipo.Mover: case EvTipo.Embestida: case EvTipo.Teletransporte: case EvTipo.Salto: pos[e.U.Id] = e.B; break;
                        case EvTipo.Empuje: pos[e.Obj.Id] = e.B; break;
                        case EvTipo.Invocar: invocadas.Add(e.U.Id); break;
                    }
                }
                bool hpOk = true, muOk = true, poOk = true;
                foreach (Unidad u in cb.Unidades)
                {
                    c.Unidades++;
                    int esp = Math.Max(0, hp[u.Id]);
                    int real = Math.Max(0, u.Vida);
                    if (u.Vida > u.VidaMax) real = u.VidaMax;
                    if (esp != real) { hpOk = false; if (nFallos++ < 200) fallos.Append(rr.Semilla).Append(';').Append(rr.Mundo).Append(';').Append(rr.Nivel).Append(";vida;").Append(u.Nombre).Append(';').Append(real).Append(';').Append(esp).Append('\n'); }
                    bool vivoRec = !muerto.Contains(u.Id) && esp > 0;
                    if (vivoRec != u.Vivo) { muOk = false; if (nFallos++ < 200) fallos.Append(rr.Semilla).Append(';').Append(rr.Mundo).Append(';').Append(rr.Nivel).Append(";vivo;").Append(u.Nombre).Append(';').Append(u.Vivo).Append(';').Append(vivoRec).Append('\n'); }
                    if (!invocadas.Contains(u.Id))
                    {
                        c.UnidadesPos++;
                        if (u.Vivo && pos[u.Id] != u.Pos) { poOk = false; if (nFallos++ < 200) fallos.Append(rr.Semilla).Append(';').Append(rr.Mundo).Append(';').Append(rr.Nivel).Append(";pos;").Append(u.Nombre).Append(';').Append(u.Pos).Append(';').Append(pos[u.Id]).Append('\n'); }
                    }
                }
                if (hpOk) c.HpOk++; if (muOk) c.MuerteOk++; if (poOk) c.PosOk++;
            }, delegate (Run rr) { vidaIni.Clear(); foreach (Gato g in rr.Expedicion) vidaIni[g] = g.Vida; });
        }
        U.Dos(w, "== M1. Completitud del registro de eventos (reconstruir el final de cada combate solo con los eventos) ==");
        U.Dos(w, "Combates analizados: " + c.Combates + " (" + nRun + " runs completas con el bot, GrabarEventos = true), unidades: " + c.Unidades + ", eventos: " + c.Eventos);
        U.Dos(w, "Vida final reconstruida = real, combates: " + c.HpOk + " de " + c.Combates);
        U.Dos(w, "Muertes reconstruidas = reales, combates: " + c.MuerteOk + " de " + c.Combates);
        U.Dos(w, "Posición final reconstruida = real (unidades no invocadas, vivas), combates: " + c.PosOk + " de " + c.Combates);
        U.Dos(w, "Eventos por combate: mediana " + U.F(U.Pct(evPorCombate, 0.5), "0") + ", p95 " + U.F(U.Pct(evPorCombate, 0.95), "0") + ", máximo " + U.F(U.Max(evPorCombate), "0"));
        var csvT = new StringBuilder("tipo_evento;cantidad\n"); foreach (var kv in porTipo) csvT.Append(kv.Key).Append(';').Append(kv.Value).Append('\n');
        File.WriteAllText(Path.Combine(sal, "sp3_eventos_por_tipo.csv"), csvT.ToString(), new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(sal, "sp3_fallos_reconstruccion.csv"), fallos.ToString(), new UTF8Encoding(false));

        // ---- M2: invariancia (grabar o no los eventos no cambia el resultado) y determinismo del flujo
        U.Dos(w, "== M2. Invariancia y determinismo ==");
        int nInv = 300, inv = 0, det = 0;
        for (int s = 1; s <= nInv; s++)
        {
            Config.GrabarEventos = false; Run a = new Run(s * 104729); BotJugador.JugarRun(a);
            Config.GrabarEventos = true; Run b = new Run(s * 104729); BotJugador.JugarRun(b);
            if (U.ResumenRun(a) == U.ResumenRun(b)) inv++;
            // determinismo del flujo de eventos: la huella del primer combate se repite
            string h1 = HuellaPrimerCombate(s * 104729), h2 = HuellaPrimerCombate(s * 104729);
            if (h1 == h2) det++;
        }
        U.Dos(w, "Resultado de la run con y sin registro de eventos: iguales " + inv + " de " + nInv);
        U.Dos(w, "Flujo de eventos del primer combate repetido con la misma semilla: iguales " + det + " de " + nInv);

        // ---- M3: sobrecosto
        U.Dos(w, "== M3. Sobrecosto de registrar eventos (100 runs completas, 3 repeticiones alternadas) ==");
        var tOff = new List<double>(); var tOn = new List<double>(); long bOff = 0, bOn = 0;
        for (int rep = 0; rep < 3; rep++)
        {
            foreach (bool on in new[] { rep % 2 == 0, rep % 2 != 0 })
            {
                Config.GrabarEventos = on;
                long b0 = GC.GetAllocatedBytesForCurrentThread(); var sw = Stopwatch.StartNew();
                for (int s = 1; s <= 100; s++) { Run r = new Run(s * 104729); BotJugador.JugarRun(r); }
                sw.Stop(); long b1 = GC.GetAllocatedBytesForCurrentThread() - b0;
                if (on) { tOn.Add(sw.Elapsed.TotalMilliseconds); bOn += b1; } else { tOff.Add(sw.Elapsed.TotalMilliseconds); bOff += b1; }
            }
        }
        double mOff = U.Media(tOff), mOn = U.Media(tOn);
        U.Dos(w, "Tiempo medio por 100 runs: sin eventos " + U.F(mOff, "0") + " ms, con eventos " + U.F(mOn, "0") + " ms (sobrecosto " + U.F((mOn / mOff - 1) * 100, "0.0") + " %)");
        U.Dos(w, "Memoria asignada por 100 runs: sin eventos " + (bOff / tOff.Count / 1048576) + " MiB, con eventos " + (bOn / tOn.Count / 1048576) + " MiB");
        File.WriteAllText(Path.Combine(sal, "sp3_sobrecosto.csv"), "modo;tiempo_ms_100_runs;MiB_100_runs\nsin_eventos;" + U.F(mOff, "0.0") + ";" + (bOff / tOff.Count / 1048576) + "\ncon_eventos;" + U.F(mOn, "0.0") + ";" + (bOn / tOn.Count / 1048576) + "\n", new UTF8Encoding(false));

        File.WriteAllText(Path.Combine(sal, "sp3_resumen.csv"),
            "metrica;valor\ncombates;" + c.Combates + "\nvida_ok;" + c.HpOk + "\nmuerte_ok;" + c.MuerteOk + "\nposicion_ok;" + c.PosOk + "\neventos_total;" + c.Eventos + "\n" +
            "eventos_mediana;" + U.F(U.Pct(evPorCombate, 0.5), "0") + "\neventos_p95;" + U.F(U.Pct(evPorCombate, 0.95), "0") + "\neventos_max;" + U.F(U.Max(evPorCombate), "0") + "\n" +
            "invariancia_ok;" + inv + "\ninvariancia_total;" + nInv + "\ndeterminismo_ok;" + det + "\nsobrecosto_pct;" + U.F((mOn / mOff - 1) * 100, "0.0") + "\n", new UTF8Encoding(false));
        Config.GrabarEventos = false;
        w.Close();
    }

    private static string HuellaPrimerCombate(int semilla)
    {
        Config.GrabarEventos = true;
        Run r = new Run(semilla); string h = "";
        U.JugarRun(r, delegate (Run rr) { if (h.Length == 0) h = U.HashEventos(rr.Combate.Eventos); });
        return h;
    }
}

// ======================================================================== SP4: presupuesto de tiempo
public static class Sp4
{
    public static void Correr(string sal)
    {
        var w = U.Log(sal, "sp4_runlog.txt", "SP4 - Presupuesto de tiempo: latencia de las llamadas del Core que corren en el hilo principal de la vista");
        Config.GrabarEventos = true;       // como en el juego real
        double tick = 1000.0 / Stopwatch.Frequency;
        string[] nombres = { "", "TerminarTurnoJugador (turnos enemigos hasta el siguiente gato)", "Mover", "UsarHabilidad", "UsarConsumible", "Run.Empezar (generar nivel + Iniciar)", "Run.Serializar", "Run.Cargar", "Planear" };
        var v = new List<double>[9]; for (int i = 0; i < v.Length; i++) v[i] = new List<double>();
        var porMundo = new Dictionary<string, List<double>>();
        int mundoAct = 1;
        Config.Medidor = delegate (int id, long ticks)
        {
            double ms = ticks * tick; v[id].Add(ms);
            if (id == Config.MedTerminarTurno) { string k = "M" + mundoAct; List<double> l; if (!porMundo.TryGetValue(k, out l)) { l = new List<double>(); porMundo[k] = l; } l.Add(ms); }
        };
        int nRun = 400;
        var primeras = new List<double>();
        var sw = Stopwatch.StartNew();
        for (int s = 1; s <= nRun; s++)
        {
            Run r = new Run(s * 7919);
            int guardia = 0;
            while (r.Fase != FaseRun.RunPerdida && r.Fase != FaseRun.CampanaCompletada && guardia++ < 1000)
            {
                mundoAct = r.Mundo;
                switch (r.Fase)
                {
                    case FaseRun.ElegirHabilidad: BotJugador.ElegirHabilidades(r); break;
                    case FaseRun.Tienda: BotJugador.Comprar(r); r.SalirDeTienda(); break;
                    case FaseRun.Preparacion:
                        BotJugador.Equipar(r);
                        if (r.PuedeGuardar) { string t = r.Serializar(); Run c = Run.Cargar(t); }
                        r.Empezar(); break;
                    case FaseRun.Combate: BotJugador.JugarCombate(r.Combate); r.ResolverFinDeCombate(); break;
                    case FaseRun.Recompensa: BotJugador.ElegirDesafio(r); break;
                }
            }
            if (s <= 400)
            {
                // Planear aparte (lo usa el mapa del mundo en cada cambio de nivel)
                long t0 = Stopwatch.GetTimestamp(); GeneradorNivel.Planear(1 + s % 7, 1 + s % 3, s); v[Config.MedPlanear].Add((Stopwatch.GetTimestamp() - t0) * tick);
            }
        }
        sw.Stop();
        Config.Medidor = null;
        U.Dos(w, "Se jugaron " + nRun + " runs completas con el bot en " + U.F(sw.Elapsed.TotalSeconds, "0.0") + " s. Umbral de la guía de diseño: ≤ 16 ms por llamada (un cuadro a 60 fps); secundario: ≤ 33 ms (30 fps).");
        var csv = new StringBuilder("llamada;n;mediana_ms;p95_ms;p99_ms;maximo_ms;sobre_16ms;sobre_33ms;primera_llamada_ms\n");
        for (int id = 1; id <= 8; id++)
        {
            var l = v[id]; if (l.Count == 0) continue;
            int s16 = 0, s33 = 0; foreach (double x in l) { if (x > 16) s16++; if (x > 33) s33++; }
            csv.Append(nombres[id].Replace(';', ',')).Append(';').Append(l.Count).Append(';').Append(U.F(U.Pct(l, .5), "0.0000")).Append(';').Append(U.F(U.Pct(l, .95), "0.0000")).Append(';').Append(U.F(U.Pct(l, .99), "0.0000")).Append(';').Append(U.F(U.Max(l), "0.0000")).Append(';').Append(s16).Append(';').Append(s33).Append(';').Append(U.F(l[0], "0.0000")).Append('\n');
            U.Dos(w, nombres[id] + ": n=" + l.Count + " mediana " + U.F(U.Pct(l, .5), "0.000") + " ms, p95 " + U.F(U.Pct(l, .95), "0.000") + ", p99 " + U.F(U.Pct(l, .99), "0.000") + ", máx " + U.F(U.Max(l), "0.000") + "; sobre 16 ms: " + s16 + "; sobre 33 ms: " + s33 + "; primera llamada " + U.F(l[0], "0.000") + " ms");
        }
        File.WriteAllText(Path.Combine(sal, "sp4_latencias.csv"), csv.ToString(), new UTF8Encoding(false));
        var cm = new StringBuilder("mundo;n;mediana_ms;p95_ms;maximo_ms\n");
        U.Dos(w, "-- TerminarTurnoJugador por mundo --");
        for (int m = 1; m <= 7; m++)
        {
            List<double> l; if (!porMundo.TryGetValue("M" + m, out l)) continue;
            cm.Append(m).Append(';').Append(l.Count).Append(';').Append(U.F(U.Pct(l, .5), "0.0000")).Append(';').Append(U.F(U.Pct(l, .95), "0.0000")).Append(';').Append(U.F(U.Max(l), "0.0000")).Append('\n');
            U.Dos(w, "Mundo " + m + ": n=" + l.Count + " mediana " + U.F(U.Pct(l, .5), "0.000") + " ms, p95 " + U.F(U.Pct(l, .95), "0.000") + ", máx " + U.F(U.Max(l), "0.000"));
        }
        File.WriteAllText(Path.Combine(sal, "sp4_por_mundo.csv"), cm.ToString(), new UTF8Encoding(false));
        // guardado: tamaño
        Run rt = new Run(2026); rt.ElegirHabilidadesAutomatico(); string txt = rt.Serializar();
        U.Dos(w, "Tamaño de un guardado al inicio de la run: " + (txt == null ? "(no se puede guardar ahora)" : Encoding.UTF8.GetByteCount(txt) + " bytes"));
        w.Close();
    }
}

// ======================================================================== SP5: guardado y reanudación
public static class Sp5
{
    public static void Correr(string sal)
    {
        var w = U.Log(sal, "sp5_runlog.txt", "SP5 - Guardado y reanudación: Serializar -> Cargar -> Serializar, y continuar tras cargar frente a continuar sin guardar");
        Config.GrabarEventos = false;
        int puntos = 0, idem = 0, equiv = 0, nulos = 0, runs = 150;
        var fallos = new StringBuilder("semilla;punto;prueba;detalle\n");
        for (int s = 1; s <= runs; s++)
        {
            int semilla = s * 15485863 % 1000003 + s;
            Run r = new Run(semilla);
            var reanudadas = new List<string>(); var ks = new List<int>();
            int guardia = 0, k = 0;
            while (r.Fase != FaseRun.RunPerdida && r.Fase != FaseRun.CampanaCompletada && guardia++ < 1000)
            {
                if (r.Fase == FaseRun.Preparacion && r.PuedeGuardar)
                {
                    k++; puntos++;
                    string t1 = r.Serializar();
                    Run c = Run.Cargar(t1);
                    if (c == null) { nulos++; fallos.Append(semilla).Append(';').Append(k).Append(";carga;Cargar devolvió null\n"); }
                    else
                    {
                        string t2 = c.Serializar();
                        if (t1 == t2) idem++; else fallos.Append(semilla).Append(';').Append(k).Append(";idempotencia;distinto texto guardado\n");
                        Run copia = Run.Cargar(t1);              // la copia sigue jugando desde el guardado
                        BotJugador.JugarRun(copia);
                        reanudadas.Add(U.ResumenRun(copia)); ks.Add(k);
                    }
                }
                switch (r.Fase)
                {
                    case FaseRun.ElegirHabilidad: BotJugador.ElegirHabilidades(r); break;
                    case FaseRun.Tienda: BotJugador.Comprar(r); r.SalirDeTienda(); break;
                    case FaseRun.Preparacion: BotJugador.Equipar(r); r.Empezar(); break;
                    case FaseRun.Combate: BotJugador.JugarCombate(r.Combate); r.ResolverFinDeCombate(); break;
                    case FaseRun.Recompensa: BotJugador.ElegirDesafio(r); break;
                }
            }
            string fin = U.ResumenRun(r);                        // la run original, sin haber guardado nunca, ya terminó
            for (int i = 0; i < reanudadas.Count; i++)
                if (reanudadas[i] == fin) equiv++; else fallos.Append(semilla).Append(';').Append(ks[i]).Append(";equivalencia;la reanudación termina distinto\n");
        }
        U.Dos(w, "Puntos de guardado probados (todas las preparaciones de nivel de " + runs + " runs): " + puntos);
        U.Dos(w, "Cargar devolvió null: " + nulos);
        U.Dos(w, "Idempotencia Serializar -> Cargar -> Serializar: " + idem + " de " + puntos);
        U.Dos(w, "Equivalencia (continuar tras cargar = continuar la run sin guardar): " + equiv + " de " + puntos);
        File.WriteAllText(Path.Combine(sal, "sp5_fallos.csv"), fallos.ToString(), new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(sal, "sp5_resumen.csv"), "metrica;valor\npuntos;" + puntos + "\nnulos;" + nulos + "\nidempotencia_ok;" + idem + "\nequivalencia_ok;" + equiv + "\n", new UTF8Encoding(false));

        // robustez ante guardados dañados
        U.Dos(w, "== Robustez: guardados dañados ==");
        Run base0 = new Run(99); base0.ElegirHabilidadesAutomatico();
        string ok = base0.Serializar();
        int rechazados = 0, casos = 0;
        var variantes = new List<string> { "", "basura", ok == null ? "" : ok.Replace("GATOMATE-GUARDADO-2", "GATOMATE-GUARDADO-1"), ok == null ? "" : ok.Substring(0, Math.Max(1, ok.Length / 2)), ok == null ? "" : ok.Replace("semilla=", "semilla=abc") };
        foreach (string t in variantes)
        {
            casos++; bool excepcion = false; Run c = null;
            try { c = Run.Cargar(t); } catch (Exception) { excepcion = true; }
            if (!excepcion && c == null) rechazados++;
            U.Dos(w, "  caso " + casos + ": " + (excepcion ? "EXCEPCIÓN no controlada" : (c == null ? "rechazado (null)" : "aceptado")));
        }
        U.Dos(w, "Guardados dañados rechazados sin excepción: " + rechazados + " de " + casos);
        File.AppendAllText(Path.Combine(sal, "sp5_resumen.csv"), "danados_rechazados;" + rechazados + "\ndanados_total;" + casos + "\n");
        w.Close();
    }

}

// ======================================================================== E2E: escenarios de punta a punta sin Unity
public static class E2e
{
    public static void Correr(string sal)
    {
        var w = U.Log(sal, "e2e_runlog.txt", "E2E - Escenarios integrados con el Core completo (sin vista de Unity)");
        Config.GrabarEventos = true;
        var csv = new StringBuilder("escenario;semilla;resultado;vidas;mundo_alcanzado;combates;eventos;tiempo_total_ms;guardados;max_llamada_ms\n");

        // E2E-1: run completa (campaña de 7 mundos) con guardado automático en cada preparación y todas las llamadas cronometradas
        int n1 = 50, ok1 = 0, compl = 0; double peor = 0;
        double tick = 1000.0 / Stopwatch.Frequency; double maxLlamada = 0;
        Config.Medidor = delegate (int id, long t) { double ms = t * tick; if (ms > maxLlamada) maxLlamada = ms; };
        var tiempos = new List<double>();
        for (int s = 1; s <= n1; s++)
        {
            maxLlamada = 0; int combates = 0, guardados = 0; long eventos = 0; var sw = Stopwatch.StartNew();
            Run r = new Run(s * 31337); int guardia = 0;
            while (r.Fase != FaseRun.RunPerdida && r.Fase != FaseRun.CampanaCompletada && guardia++ < 1000)
            {
                switch (r.Fase)
                {
                    case FaseRun.ElegirHabilidad: BotJugador.ElegirHabilidades(r); break;
                    case FaseRun.Tienda: BotJugador.Comprar(r); r.SalirDeTienda(); break;
                    case FaseRun.Preparacion:
                        if (r.PuedeGuardar) { string t = r.Serializar(); Run c = Run.Cargar(t); if (c != null) { r = c; guardados++; } }   // como la vista: guarda y puede reanudar
                        BotJugador.Equipar(r); r.Empezar(); break;
                    case FaseRun.Combate: BotJugador.JugarCombate(r.Combate); combates++; eventos += r.Combate.Eventos.Count; r.ResolverFinDeCombate(); break;
                    case FaseRun.Recompensa: BotJugador.ElegirDesafio(r); break;
                }
            }
            sw.Stop(); tiempos.Add(sw.Elapsed.TotalMilliseconds);
            bool fin = r.Fase == FaseRun.CampanaCompletada || r.Fase == FaseRun.RunPerdida; if (fin) ok1++;
            if (r.Fase == FaseRun.CampanaCompletada) compl++;
            int mmax = 0; foreach (RegistroNivel g in r.Historial) mmax = Math.Max(mmax, g.Mundo);
            peor = Math.Max(peor, maxLlamada);
            csv.Append("E2E-1;").Append(s * 31337).Append(';').Append(r.Fase).Append(';').Append(r.Vidas).Append(';').Append(mmax).Append(';').Append(combates).Append(';').Append(eventos).Append(';').Append(U.F(sw.Elapsed.TotalMilliseconds, "0.0")).Append(';').Append(guardados).Append(';').Append(U.F(maxLlamada, "0.000")).Append('\n');
        }
        Config.Medidor = null;
        U.Dos(w, "E2E-1 run completa con guardado/carga en cada preparación: " + n1 + " runs, terminan (campaña o derrota final) " + ok1 + "; campañas completadas " + compl + "; llamada más lenta del Core en todas las runs " + U.F(peor, "0.000") + " ms; tiempo por run mediana " + U.F(U.Pct(tiempos, .5), "0") + " ms.");

        // E2E-2: tutorial completo con el bot
        int okT = 0;
        for (int s = 1; s <= 20; s++)
        {
            Run t = Run.NuevoTutorial();
            BotJugador.JugarRun(t);
            if (t.Fase == FaseRun.CampanaCompletada) okT++;
        }
        U.Dos(w, "E2E-2 tutorial de 3 niveles superado por el bot: " + okT + " de 20.");
        csv.Append("E2E-2;tutorial;").Append(okT).Append("_de_20;;;;;;;\n");

        // E2E-3: condiciones de fallo y degradación
        int fallo = 0, total = 0;
        // (a) derrota: con 1 gato de vida casi nula, la run no se cuelga
        total++; { Run r = new Run(5); r.ElegirHabilidadesAutomatico(); foreach (Gato g in r.Expedicion) g.Vida = 1; BotJugador.JugarRun(r); if (r.Fase == FaseRun.RunPerdida || r.Fase == FaseRun.CampanaCompletada || r.Fase == FaseRun.Combate || r.Fase == FaseRun.Preparacion) { } else fallo++; }
        // (b) límite de rondas: ningún combate excede Config.RondasMaximas
        total++; { int mx = 0; Run r = new Run(11); U.JugarRun(r, delegate (Run rr) { mx = Math.Max(mx, rr.Combate.Ronda); }); if (mx > Config.RondasMaximas) fallo++; U.Dos(w, "  (b) rondas máximas observadas " + mx + " (límite " + Config.RondasMaximas + ")"); }
        // (c) guardado inválido
        total++; { Run c = null; bool ex = false; try { c = Run.Cargar("GATOMATE-GUARDADO-2\nsemilla=xx\n"); } catch (Exception) { ex = true; } if (ex) fallo++; U.Dos(w, "  (c) guardado inválido: " + (ex ? "excepción" : (c == null ? "rechazado" : "aceptado"))); }
        U.Dos(w, "E2E-3 condiciones de fallo comprobadas: " + total + ", con comportamiento inesperado: " + fallo);
        File.WriteAllText(Path.Combine(sal, "e2e_corridas.csv"), csv.ToString(), new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(sal, "e2e_resumen.csv"), "metrica;valor\nrun_completa_total;" + n1 + "\nrun_completa_terminan;" + ok1 + "\ncampanas_completadas;" + compl + "\nllamada_mas_lenta_ms;" + U.F(peor, "0.000") + "\ntutorial_ok;" + okT + "\nfallo_total;" + total + "\nfallo_inesperado;" + fallo + "\n", new UTF8Encoding(false));
        Config.GrabarEventos = false;
        w.Close();
    }
}
