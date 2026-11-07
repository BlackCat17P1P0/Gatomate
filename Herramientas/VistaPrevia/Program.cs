// Gatomate - herramienta de vista previa: dibuja los sprites procedurales y los guarda como .rgba crudos
// (un script los convierte a PNG). No es parte del juego; sirve para revisar el arte sin abrir Unity.
using System;
using System.Collections.Generic;
using System.IO;
using Gatomate.Core;
using Gatomate.View;

public static class Previa
{
    static string IdEq(EquipoTipo e)
    {
        switch (e)
        {
            case EquipoTipo.CollarFuerza: return "it_collar_fue";
            case EquipoTipo.CollarInteligencia: return "it_collar_int";
            case EquipoTipo.CollarDestreza: return "it_collar_des";
            case EquipoTipo.BotasCascabel: return "bota";
            case EquipoTipo.ChalecoCuero: return "it_chaleco";
            case EquipoTipo.AmuletoTrebol: return "it_trebol";
            case EquipoTipo.CascabelOro: return "it_cascabel";
            case EquipoTipo.AnilloFuego: return "it_anillo_fuego";
            case EquipoTipo.AnilloHielo: return "it_anillo_hielo";
            case EquipoTipo.AnilloRayo: return "it_anillo_rayo";
            case EquipoTipo.AnilloVeneno: return "it_anillo_veneno";
            case EquipoTipo.GemaFuego: return "it_gema_fuego";
            case EquipoTipo.GemaHielo: return "it_gema_hielo";
            case EquipoTipo.GemaRayo: return "it_gema_rayo";
            case EquipoTipo.GemaVeneno: return "it_gema_veneno";
            default: return "ob_" + e.ToString().ToLowerInvariant();
        }
    }

    public static void Main(string[] args)
    {
        GameData.Clase("mago");
        string dir = args.Length > 0 ? args[0] : ".";
        Directory.CreateDirectory(dir);

        // 1) casillas de cada mundo
        var h1 = new Hoja(7 * 140 + 20, 4 * 110 + 20, new Col(0.12f, 0.1f, 0.18f));
        for (int m = 1; m <= 7; m++)
        {
            Terreno[] ts = { Terreno.Suelo, Terreno.HierbaAlta, Terreno.Flores, Terreno.Barro };
            for (int i = 0; i < ts.Length; i++) h1.Poner(Dibujos.Casilla(m, ts[i], 0), 10 + (m - 1) * 140, 10 + i * 110);
        }
        h1.Guardar(Path.Combine(dir, "casillas.rgba"));

        // 1b) los 50 objetos equipables y la clave de fuego amigo
        {
            var nombres = new System.Collections.Generic.List<string>();
            foreach (EquipoTipo e in Enum.GetValues(typeof(EquipoTipo))) { if (e.ToString() == "Ninguno") continue; nombres.Add(IdEq(e)); }
            nombres.Add("amigo");
            int cols = 10, filas = (nombres.Count + cols - 1) / cols;
            var ho = new Hoja(cols * 72 + 20, filas * 72 + 20, new Col(0.96f, 0.91f, 0.76f));
            for (int i = 0; i < nombres.Count; i++) ho.Poner(Dibujos.Icono(nombres[i]), 10 + (i % cols) * 72, 10 + (i / cols) * 72);
            ho.Guardar(Path.Combine(dir, "objetos.rgba"));
            Console.WriteLine("objetos: " + (nombres.Count - 1));
        }

        // 2) bloques y matas por mundo
        var h2 = new Hoja(7 * 140 + 20, 2 * 140 + 20, new Col(0.12f, 0.1f, 0.18f));
        for (int m = 1; m <= 7; m++)
        {
            h2.Poner(Dibujos.Casilla(m, Terreno.Suelo, 1), 10 + (m - 1) * 140, 10);
            h2.Poner(Dibujos.Bloque(m, false, 0), 10 + (m - 1) * 140, 10);
            h2.Poner(Dibujos.Casilla(m, Terreno.Suelo, 1), 10 + (m - 1) * 140, 150);
            h2.Poner(Dibujos.Bloque(m, true, 0), 10 + (m - 1) * 140 + 0, 150 - 4);
        }
        h2.Guardar(Path.Combine(dir, "bloques.rgba"));

        // 3) gatos y enemigos
        string[] clases = { "luchador", "mago", "tanque", "cazador", "clerigo", "picaro", "druida", "alquimista" };
        var h3 = new Hoja(8 * 110 + 20, 110 + 110 + 20, new Col(0.55f, 0.75f, 0.55f));
        for (int i = 0; i < clases.Length; i++) h3.Poner(Dibujos.Gato(clases[i]), 10 + i * 110, 10);
        string[] en = { "maton", "arquero", "bruto", "hechicero", "sanador", "acosador", "lodo" };
        for (int i = 0; i < en.Length; i++) h3.Poner(Dibujos.Enemigo(en[i]), 10 + i * 110, 120);
        h3.Guardar(Path.Combine(dir, "unidades.rgba"));

        // 4) jefes
        var h4 = new Hoja(7 * 140 + 20, 150, new Col(0.55f, 0.75f, 0.55f));
        for (int i = 1; i <= 7; i++) h4.Poner(Dibujos.Enemigo("jefe" + i), 10 + (i - 1) * 140, 10);
        h4.Guardar(Path.Combine(dir, "jefes.rgba"));

        // 5) iconos y formas
        var h5 = new Hoja(14 * 70 + 20, 3 * 80 + 20, new Col(0.2f, 0.18f, 0.28f));
        string[] ic = { "corazon", "corazon_vacio", "gota", "gota_vacia", "bota", "escudo", "espada", "estrella", "pocionVida", "pocionMana", "pocionVel", "antidoto", "bomba", "calavera" };
        for (int i = 0; i < ic.Length; i++) h5.Poner(Dibujos.Icono(ic[i]), 10 + i * 70, 10);
        for (int i = 0; i < clases.Length; i++) h5.Poner(Dibujos.IconoHabilidad(clases[i]), 10 + i * 70, 90);
        Imagen[] fx = { Dibujos.Disco(48, true), Dibujos.Disco(48, false), Dibujos.Estrella(48), Dibujos.MasCruz(48), Dibujos.AnilloImg(48), Dibujos.Tajo(48), Dibujos.Chispa(48), Dibujos.Llama(48), Dibujos.Hoja(48), Dibujos.Burbuja(48), Dibujos.Runa(48), Dibujos.RomboBorde(64), Dibujos.Sombra() };
        for (int i = 0; i < fx.Length; i++) h5.Poner(fx[i], 10 + i * 70, 170);
        h5.Guardar(Path.Combine(dir, "iconos.rgba"));

        // 6) UI
        var h6 = new Hoja(500, 200, new Col(0.3f, 0.5f, 0.6f));
        h6.Poner(Dibujos.Panel(Col.Hex("#3A2B4D"), Col.Hex("#F6E7C1"), true), 10, 10, 128, 128);
        h6.Poner(Dibujos.Panel(Col.Hex("#F6E7C1"), Col.Hex("#6B4A8A"), false), 150, 10, 128, 128);
        h6.Poner(Dibujos.Boton(Col.Hex("#F5B642"), false), 290, 10, 100, 60);
        h6.Poner(Dibujos.Boton(Col.Hex("#F5B642"), true), 290, 80, 100, 60);
        h6.Poner(Dibujos.Boton(Col.Hex("#E0556B"), false), 400, 10, 90, 60);
        h6.Guardar(Path.Combine(dir, "ui.rgba"));

        // 7) arte nuevo: terrenos, obstáculos, monedas, enemigos, iconos
        var h7 = new Hoja(10 * 130 + 20, 130 + 120 + 120 + 90 + 90 + 90 + 30, new Col(0.45f, 0.65f, 0.5f));
        Terreno[] nt = { Terreno.Agua, Terreno.Caja, Terreno.RocaFragil, Terreno.Barril };
        for (int i = 0; i < nt.Length; i++)
        {
            Terreno bt = nt[i] == Terreno.Agua ? Terreno.Agua : Terreno.Suelo;
            h7.Poner(Dibujos.Casilla(2, bt, 0), 10 + i * 130, 10);
            if (nt[i] != Terreno.Agua) h7.Poner(Dibujos.Obstaculo(2, nt[i]), 10 + i * 130, 10);
        }
        for (int i = 1; i <= 3; i++) { h7.Poner(Dibujos.Casilla(2, Terreno.Suelo, 0), 10 + (3 + i) * 130, 10); h7.Poner(Dibujos.Moneda(i), 10 + (3 + i) * 130 + 32, 10 + 24); }
        string[] ne = { "nido", "rata", "bombardero", "slime_fuego", "slime_hielo", "sapo", "anguila", "chaman", "ladron" };
        for (int i = 0; i < ne.Length; i++) h7.Poner(Dibujos.Enemigo(ne[i]), 10 + i * 130, 150);
        string[] ni = { "moneda", "bombaFuego", "bombaHielo", "frascoVeneno", "fuego", "hielo", "rayo", "veneno", "fuerza", "flechaEmpuje" };
        for (int i = 0; i < ni.Length; i++) h7.Poner(Dibujos.Icono(ni[i]), 10 + i * 130, 270);
        int fila = 0, col = 0;
        foreach (ClaseDef cd in GameData.Clases)
        {
            foreach (HabilidadDef hd in cd.Pool) { h7.Poner(Dibujos.IconoHabilidadDef(hd, cd.Id), 10 + col * 66, 350 + fila * 72, 64, 64); col++; if (col >= 24) { col = 0; fila++; } }
        }
        h7.Guardar(Path.Combine(dir, "nuevo.rgba"));

        // 8) iconos de palabras clave y objetos equipables
        string[] ck = { "dmg", "rango", "gratis", "libre", "espalda", "aislado", "fisico", "cruz", "alrededor", "carga", "cura", "limpiar", "provocar", "defensa", "potenciar", "salto", "intercambio", "embestida", "enredadera", "zona_fuego", "zona_veneno", "zona_hielo", "hierba", "st_def", "st_vel", "st_fue", "st_int", "st_des", "st_sue", "it_collar_fue", "it_collar_int", "it_collar_des", "it_chaleco", "it_trebol", "it_cascabel", "it_anillo_fuego", "it_anillo_hielo", "it_anillo_rayo", "it_anillo_veneno", "it_gema_fuego", "it_gema_hielo", "it_gema_rayo", "it_gema_veneno" };
        var h8 = new Hoja(15 * 70 + 20, 3 * 70 + 20, new Col(0.2f, 0.18f, 0.28f));
        for (int i = 0; i < ck.Length; i++) h8.Poner(Dibujos.Icono(ck[i]), 10 + (i % 15) * 70, 10 + (i / 15) * 70, 64, 64);
        h8.Guardar(Path.Combine(dir, "claves.rgba"));

        Compositor.Escena(Path.Combine(dir, "escena2.rgba"), 2, 2, true);
        Compositor.Escena(Path.Combine(dir, "escena4.rgba"), 4, 1, false);
        Compositor.Escena(Path.Combine(dir, "escena5.rgba"), 5, 3, false);
        Compositor.Escena(Path.Combine(dir, "escena3.rgba"), 3, 2, false);
        Compositor.Escena(Path.Combine(dir, "escena6.rgba"), 6, 2, false);
    }
}

public sealed class Hoja
{
    public readonly int W, H; private readonly float[] _p;
    public Hoja(int w, int h, Col fondo)
    {
        W = w; H = h; _p = new float[w * h * 3];
        for (int i = 0; i < w * h; i++) { _p[i * 3] = fondo.R; _p[i * 3 + 1] = fondo.G; _p[i * 3 + 2] = fondo.B; }
    }
    public void Poner(Imagen im, int x, int y) { Poner(im, x, y, im.W, im.H); }
    public void Poner(Imagen im, int x, int y, int w, int h)
    {
        for (int j = 0; j < h; j++)
            for (int i = 0; i < w; i++)
            {
                int sx = i * im.W / w, sy = j * im.H / h;
                int s = (sy * im.W + sx) * 4;
                int dx = x + i, dy = y + j;
                if (dx < 0 || dy < 0 || dx >= W || dy >= H) continue;
                int d = (dy * W + dx) * 3;
                float ia = 1f - im.P[s + 3];
                _p[d] = im.P[s] + _p[d] * ia; _p[d + 1] = im.P[s + 1] + _p[d + 1] * ia; _p[d + 2] = im.P[s + 2] + _p[d + 2] * ia;
            }
    }
    public void PonerAlfa(Imagen im, int x, int y, int w, int h, float alfa, Col tinte, bool espejo)
    {
        for (int j = 0; j < h; j++)
            for (int i = 0; i < w; i++)
            {
                int sx = (espejo ? (w - 1 - i) : i) * im.W / Math.Max(1, w), sy = j * im.H / Math.Max(1, h);
                if (sx < 0 || sy < 0 || sx >= im.W || sy >= im.H) continue;
                int s = (sy * im.W + sx) * 4;
                int dx = x + i, dy = y + j;
                if (dx < 0 || dy < 0 || dx >= W || dy >= H) continue;
                int d = (dy * W + dx) * 3;
                float a = im.P[s + 3] * alfa, ia = 1f - a;
                float r = im.P[s] * tinte.R, g = im.P[s + 1] * tinte.G, b = im.P[s + 2] * tinte.B;
                _p[d] = r * alfa + _p[d] * ia; _p[d + 1] = g * alfa + _p[d + 1] * ia; _p[d + 2] = b * alfa + _p[d + 2] * ia;
            }
    }
    public void Guardar(string ruta)
    {
        var b = new byte[8 + W * H * 3];
        BitConverter.GetBytes(W).CopyTo(b, 0); BitConverter.GetBytes(H).CopyTo(b, 4);
        for (int i = 0; i < W * H * 3; i++) b[8 + i] = (byte)Math.Max(0, Math.Min(255, _p[i] * 255f + 0.5f));
        File.WriteAllBytes(ruta, b);
    }
}

/// <summary>Compone una escena isométrica fuera de Unity con las mismas fórmulas que EscenaIso (para revisar la alineación).</summary>
public static class Compositor
{
    private struct Item { public Imagen Im; public float Wx, Wy, Esc; public int Orden; public Col Tinte; public float Alfa; }

    public static void Escena(string ruta, int mundo, int nivel, bool marcar)
    {
        var run = new Run(4242);
        Combate c = GeneradorNivel.Crear(run.Expedicion, mundo, nivel, 777, new List<EfectoActivo>(), run.Inventario);
        int W = 1600, H = 900;
        Paleta pal = Dibujos.PaletaMundo(mundo);
        var hoja = new Hoja(W, H, pal.Cielo1.Mezcla(pal.Cielo2, 0.3f));
        // degradado de cielo
        Imagen deg = Dibujos.Degradado(pal.Cielo1, pal.Cielo2);
        hoja.Poner(deg, 0, 0, W, H);
        var items = new List<Item>();
        float cx = (c.Tab.Ancho - 1) * 0.5f, cy = (c.Tab.Alto - 1) * 0.5f;
        Func<float, float, float[]> mundoXY = delegate (float gx, float gy) { return new[] { ((gx - cx) - (gy - cy)) * 0.64f, -((gx - cx) + (gy - cy)) * 0.32f }; };
        // isla
        float k = (c.Tab.Ancho + c.Tab.Alto) * 0.5f;
        items.Add(new Item { Im = Dibujos.Rombo(128, true), Wx = 0, Wy = -0.42f, Esc = k * 1.04f, Orden = -60, Tinte = new Col(0.1f, 0.06f, 0.14f), Alfa = 0.55f });
        for (int x = 0; x < c.Tab.Ancho; x++)
            for (int y = 0; y < c.Tab.Alto; y++)
            {
                Pos p = new Pos(x, y);
                Terreno t = c.Tab.TerrenoEn(p);
                float[] w = mundoXY(x, y);
                int o = (x + y) * 10;
                Terreno basey = (t == Terreno.Roca || t == Terreno.Muro || t == Terreno.Caja || t == Terreno.RocaFragil || t == Terreno.Barril) ? Terreno.Suelo : t;
                items.Add(new Item { Im = Dibujos.Casilla(mundo, basey, (x + y) % 2), Wx = w[0], Wy = w[1], Esc = 1, Orden = o, Alfa = 1, Tinte = new Col(1, 1, 1) });
                if (t == Terreno.Roca || t == Terreno.Muro) items.Add(new Item { Im = Dibujos.Bloque(mundo, t == Terreno.Muro, (x * 3 + y) % 2), Wx = w[0], Wy = w[1], Esc = 1, Orden = o + 3, Alfa = 1, Tinte = new Col(1, 1, 1) });
                if (t == Terreno.Caja || t == Terreno.RocaFragil || t == Terreno.Barril) items.Add(new Item { Im = Dibujos.Obstaculo(mundo, t), Wx = w[0], Wy = w[1], Esc = 1, Orden = o + 3, Alfa = 1, Tinte = new Col(1, 1, 1) });
                if (c.Tab.MonedasEn(p) > 0) items.Add(new Item { Im = Dibujos.Moneda(Math.Min(3, c.Tab.MonedasEn(p))), Wx = w[0], Wy = w[1] + 0.05f, Esc = 1, Orden = o + 4, Alfa = 1, Tinte = new Col(1, 1, 1) });
                if (t == Terreno.HierbaAlta) items.Add(new Item { Im = Dibujos.MataHierba(mundo, (x * 3 + y) % 2), Wx = w[0], Wy = w[1], Esc = 1, Orden = o + 7, Alfa = 1, Tinte = new Col(1, 1, 1) });
                if (marcar && x >= 2 && x <= 4 && y >= 3 && y <= 5) items.Add(new Item { Im = Dibujos.Rombo(128, true), Wx = w[0], Wy = w[1], Esc = 1, Orden = o + 2, Alfa = 0.3f, Tinte = new Col(0.35f, 0.65f, 1f) });
            }
        foreach (Unidad u in c.Unidades)
        {
            float[] w = mundoXY(u.Pos.X, u.Pos.Y);
            int o = (u.Pos.X + u.Pos.Y) * 10;
            items.Add(new Item { Im = Dibujos.Sombra(), Wx = w[0], Wy = w[1], Esc = (u.Def != null && u.Def.EsJefe) ? 1.7f : 1.05f, Orden = o + 2, Alfa = 0.9f, Tinte = new Col(1, 1, 1) });
            Imagen im = u.EsGato ? Dibujos.Gato(u.Gato.ClaseId) : Dibujos.Enemigo(u.Def.Id);
            items.Add(new Item { Im = im, Wx = w[0], Wy = w[1], Esc = u.EsGato ? 1f : -1f, Orden = o + 5, Alfa = 1, Tinte = new Col(1, 1, 1) });
        }
        items.Sort(delegate (Item a, Item b) { return a.Orden.CompareTo(b.Orden); });
        float ppu = 100f * 0.95f;
        foreach (Item it in items)
        {
            float escX = it.Esc, escY = Math.Abs(it.Esc);
            int w = (int)(it.Im.W * Math.Abs(escX) * 0.95f), h = (int)(it.Im.H * escY * 0.95f);
            float px = W / 2f + it.Wx * ppu - it.Im.PivX * w, py = H / 2f - it.Wy * ppu - (1f - it.Im.PivYAbajo) * h + 20;
            hoja.PonerAlfa(it.Im, (int)px, (int)py, w, h, it.Alfa, it.Tinte, escX < 0);
        }
        hoja.Guardar(ruta);
    }
}
