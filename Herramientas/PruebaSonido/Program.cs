// Gatomate - herramienta: genera los sonidos y músicas y los guarda como .wav, y revisa que no haya NaN ni recortes.
using System;
using System.IO;
using Gatomate.View;

public static class PruebaSonido
{
    public static void Main(string[] args)
    {
        string dir = args.Length > 0 ? args[0] : ".";
        Directory.CreateDirectory(dir);
        string[] ids = { "clic", "hover", "error", "paso", "barro", "hierba", "salto", "golpe", "dano", "esquiva", "cura", "magia", "fuego", "flecha", "provocar", "muerte", "invocar", "bomba", "pocion", "turno", "victoria", "derrota", "carta", "tormenta", "muro", "texto" };
        var sw = System.Diagnostics.Stopwatch.StartNew();
        foreach (string id in ids) Guardar(dir, "sfx_" + id, Sintetizador.Efecto(id));
        Console.WriteLine("efectos: " + sw.ElapsedMilliseconds + " ms");
        foreach (string m in new[] { "calma", "combate", "jefe" })
        {
            sw.Restart();
            float[] d = Sintetizador.Musica(m);
            Console.WriteLine("musica " + m + ": " + sw.ElapsedMilliseconds + " ms");
            Guardar(dir, "mus_" + m, d);
        }
    }

    private static void Guardar(string dir, string nombre, float[] d)
    {
        float pico = 0; int nan = 0; int recortes = 0;
        foreach (float f in d) { if (float.IsNaN(f) || float.IsInfinity(f)) nan++; else { pico = Math.Max(pico, Math.Abs(f)); if (Math.Abs(f) > 1f) recortes++; } }
        Console.WriteLine(nombre.PadRight(14) + (d.Length / (float)Sintetizador.Hz).ToString("0.00") + " s  pico " + pico.ToString("0.00") + (nan > 0 ? "  NaN=" + nan : "") + (recortes > 0 ? "  recortes=" + recortes : ""));
        using (var fs = new FileStream(Path.Combine(dir, nombre + ".wav"), FileMode.Create))
        using (var bw = new BinaryWriter(fs))
        {
            int n = d.Length;
            bw.Write(new[] { 'R', 'I', 'F', 'F' }); bw.Write(36 + n * 2); bw.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
            bw.Write(16); bw.Write((short)1); bw.Write((short)1); bw.Write(Sintetizador.Hz); bw.Write(Sintetizador.Hz * 2); bw.Write((short)2); bw.Write((short)16);
            bw.Write(new[] { 'd', 'a', 't', 'a' }); bw.Write(n * 2);
            foreach (float f in d) bw.Write((short)(Math.Max(-1f, Math.Min(1f, float.IsNaN(f) ? 0f : f)) * 32000));
        }
    }
}
