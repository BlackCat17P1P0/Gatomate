// Gatomate - vista. Convierte los dibujos procedurales (Dibujos.cs) en sprites y texturas de Unity, con caché.
// No hay archivos de imagen en el proyecto: todo se genera al arrancar (o al entrar en cada mundo).
using System;
using System.Collections.Generic;
using UnityEngine;
using Gatomate.Core;

namespace Gatomate.View
{
    public static class Arte
    {
        public const float PPU = 100f;      // 100 píxeles = 1 unidad de mundo
        private static readonly Dictionary<string, Sprite> _sp = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, Texture2D> _tx = new Dictionary<string, Texture2D>();

        private static Texture2D Crear(Imagen im)
        {
            var tex = new Texture2D(im.W, im.H, TextureFormat.RGBA32, false);
            byte[] b = im.Bytes();
            var px = new Color32[im.W * im.H];
            for (int y = 0; y < im.H; y++)
                for (int x = 0; x < im.W; x++)
                {
                    // Unity: la fila 0 es la de ABAJO; nuestras imágenes tienen la fila 0 arriba
                    int s = ((im.H - 1 - y) * im.W + x) * 4;
                    px[y * im.W + x] = new Color32(b[s], b[s + 1], b[s + 2], b[s + 3]);
                }
            tex.SetPixels32(px);
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            return tex;
        }

        /// <summary>Sprite con caché por clave.</summary>
        public static Sprite Sprite(string clave, Func<Imagen> f)
        {
            Sprite s;
            if (_sp.TryGetValue(clave, out s)) return s;
            Imagen im = f();
            Texture2D t = Crear(im);
            s = UnityEngine.Sprite.Create(t, new Rect(0, 0, im.W, im.H), new Vector2(im.PivX, im.PivYAbajo), PPU);
            _sp[clave] = s;
            _tx[clave] = t;
            return s;
        }

        /// <summary>Textura con caché (para dibujar en la interfaz con GUI.DrawTexture).</summary>
        public static Texture2D Textura(string clave, Func<Imagen> f)
        {
            Sprite s = Sprite(clave, f);
            return _tx[clave];
        }

        // ---------------------------------------------------------------- atajos
        public static Sprite Casilla(int mundo, Terreno t, int variante)
        {
            Terreno k = (t == Terreno.Roca || t == Terreno.Muro || t == Terreno.Caja || t == Terreno.RocaFragil || t == Terreno.Barril) ? Terreno.Suelo : t;
            return Sprite("cas" + mundo + "_" + (int)k + "_" + variante, delegate { return Dibujos.Casilla(mundo, k, variante); });
        }
        public static Sprite Mata(int mundo, int variante) { return Sprite("mata" + mundo + "_" + variante, delegate { return Dibujos.MataHierba(mundo, variante); }); }
        public static Sprite Bloque(int mundo, bool muro, int variante) { return Sprite("blq" + mundo + (muro ? "m" : "r") + variante, delegate { return Dibujos.Bloque(mundo, muro, variante); }); }
        public static Sprite Obstaculo(int mundo, Terreno t) { return Sprite("obs" + mundo + "_" + (int)t, delegate { return Dibujos.Obstaculo(mundo, t); }); }
        public static Sprite Moneda(int n) { int k = n < 1 ? 1 : (n > 3 ? 3 : n); return Sprite("moneda" + k, delegate { return Dibujos.Moneda(k); }); }
        public static Sprite Gato(string clase) { return Sprite("gato_" + clase, delegate { return Dibujos.Gato(clase); }); }
        public static Sprite Enemigo(string id) { return Sprite("en_" + id, delegate { return Dibujos.Enemigo(id); }); }
        public static Sprite DeUnidad(Unidad u) { return u.EsGato ? Gato(u.Gato.ClaseId) : Enemigo(u.Def.Id); }
        public static Texture2D TexUnidad(Unidad u) { DeUnidad(u); return u.EsGato ? _tx["gato_" + u.Gato.ClaseId] : _tx["en_" + u.Def.Id]; }
        public static Texture2D TexGato(string clase) { Gato(clase); return _tx["gato_" + clase]; }
        public static Texture2D TexEnemigo(string id) { Enemigo(id); return _tx["en_" + id]; }

        public static Sprite Disco { get { return Sprite("disco", delegate { return Dibujos.Disco(32, false); }); } }
        public static Sprite DiscoSuave { get { return Sprite("discoS", delegate { return Dibujos.Disco(64, true); }); } }
        public static Sprite Estrella { get { return Sprite("estrella", delegate { return Dibujos.Estrella(32); }); } }
        public static Sprite Cruz { get { return Sprite("cruz", delegate { return Dibujos.MasCruz(32); }); } }
        public static Sprite Anillo { get { return Sprite("anillo", delegate { return Dibujos.AnilloImg(96); }); } }
        public static Sprite Tajo { get { return Sprite("tajo", delegate { return Dibujos.Tajo(96); }); } }
        public static Sprite Chispa { get { return Sprite("chispa", delegate { return Dibujos.Chispa(32); }); } }
        public static Sprite Llama { get { return Sprite("llama", delegate { return Dibujos.Llama(40); }); } }
        public static Sprite Hoja { get { return Sprite("hoja", delegate { return Dibujos.Hoja(24); }); } }
        public static Sprite Burbuja { get { return Sprite("burbuja", delegate { return Dibujos.Burbuja(32); }); } }
        public static Sprite Runa { get { return Sprite("runa", delegate { return Dibujos.Runa(128); }); } }
        public static Sprite RomboLleno { get { return Sprite("romboL", delegate { return Dibujos.Rombo(128, true); }); } }
        public static Sprite RomboBorde { get { return Sprite("romboB", delegate { return Dibujos.RomboBorde(128); }); } }
        public static Sprite Sombra { get { return Sprite("sombra", delegate { return Dibujos.Sombra(); }); } }
        public static Sprite Flecha { get { return Sprite("flecha", delegate { return Dibujos.Flecha(); }); } }
        public static Sprite Rayo { get { return Sprite("rayo", delegate { return Dibujos.Rayo(); }); } }
        public static Sprite Barra { get { return Sprite("barra", delegate { return Dibujos.BarraBlanca(); }); } }
        public static Sprite Cuadrado { get { return Sprite("cuad", delegate { return Dibujos.Cuadrado(); }); } }
        public static Sprite Nube(int v) { return Sprite("nube" + v, delegate { return Dibujos.Nube(256, v); }); }
        public static Sprite Degradado(int mundo)
        {
            return Sprite("degr" + mundo, delegate { Paleta p = Dibujos.PaletaMundo(mundo); return Dibujos.Degradado(p.Cielo1, p.Cielo2); });
        }
        public static Texture2D Vineta { get { return Textura("vineta", delegate { return Dibujos.Vineta(); }); } }

        // ---------------------------------------------------------------- interfaz
        public static Texture2D Icono(string id) { return Textura("ic_" + id, delegate { return Dibujos.Icono(id); }); }
        /// <summary>Icono de una palabra clave (daño, rango, empuje...). Algunas reutilizan iconos que ya existían.</summary>
        public static Texture2D IconoClave(string id)
        {
            switch (id)
            {
                case "mana": return Icono("gota");
                case "mov": case "st_mov": return Icono("bota");
                case "consumible": return Icono("pocionVida");
                case "st_hp": return Icono("corazon");
                case "st_mana": return Icono("gota");
                default: return Icono(id);
            }
        }

        public static string IdIconoEquipo(EquipoTipo e)
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

        public static Texture2D IconoEquipo(EquipoTipo e) { return Icono(IdIconoEquipo(e)); }

        public static Texture2D IconoHab(string clase) { return Textura("ih_" + clase, delegate { return Dibujos.IconoHabilidad(clase); }); }
        public static Texture2D IconoHab(HabilidadDef h, string clase) { return Textura("ihd_" + h.Id, delegate { return Dibujos.IconoHabilidadDef(h, clase); }); }
        public static Texture2D PanelCrema { get { return Textura("pCrema", delegate { return Dibujos.Panel(Col.Hex("#F6E7C1"), Col.Hex("#5A3B52"), false); }); } }
        public static Texture2D PanelOscuro { get { return Textura("pOsc", delegate { return Dibujos.Panel(Col.Hex("#3B2B4F"), Col.Hex("#F6E7C1"), true); }); } }
        public static Texture2D BarraFondo { get { return Textura("bFondo", delegate { return Dibujos.Panel(Col.Hex("#2A1B33"), Col.Contorno, false); }); } }
        public static Texture2D BarraRelleno { get { return Textura("bRelleno", delegate { return Dibujos.Panel(Col.Blanco, Col.Blanco.Mul(0.9f), true); }); } }
        public static Texture2D PanelRosa { get { return Textura("pRosa", delegate { return Dibujos.Panel(Col.Hex("#FFC7D8"), Col.Hex("#5A3B52"), true); }); } }
        public static Texture2D Boton(string tono, bool presionado)
        {
            string hex = tono == "rojo" ? "#E0556B" : (tono == "verde" ? "#6CCB7A" : (tono == "azul" ? "#58A6E8" : (tono == "gris" ? "#B9B3C4" : "#F5B642")));
            return Textura("btn" + tono + presionado, delegate { return Dibujos.Boton(Col.Hex(hex), presionado); });
        }

        /// <summary>Genera de una vez todo lo de un mundo (evita tirones durante el combate).</summary>
        public static void Precargar(int mundo)
        {
            for (int v = 0; v < 2; v++)
                foreach (Terreno t in new[] { Terreno.Suelo, Terreno.HierbaAlta, Terreno.Flores, Terreno.Barro, Terreno.Agua }) Casilla(mundo, t, v);
            foreach (Terreno t in new[] { Terreno.Caja, Terreno.RocaFragil, Terreno.Barril }) Obstaculo(mundo, t);
            Moneda(1); Moneda(2); Moneda(3);
            Mata(mundo, 0); Mata(mundo, 1); Bloque(mundo, false, 0); Bloque(mundo, true, 0); Degradado(mundo);
            MundoDef m = GameData.Mundos[Math.Min(mundo, 7) - 1];
            foreach (string id in m.TiposEnemigo) Enemigo(id);
            Enemigo(m.JefeId); Enemigo("lodo"); Enemigo("rata"); Enemigo("acosador");
        }

        public static void PrecargarBase()
        {
            foreach (ClaseDef c in GameData.Clases) { Gato(c.Id); IconoHab(c.Id); foreach (HabilidadDef h in c.Pool) IconoHab(h, c.Id); }
            Sprite s; s = Disco; s = DiscoSuave; s = Estrella; s = Cruz; s = Anillo; s = Tajo; s = Chispa; s = Llama; s = Hoja; s = Burbuja; s = Runa;
            s = RomboLleno; s = RomboBorde; s = Sombra; s = Flecha; s = Rayo; s = Barra; s = Cuadrado; s = Nube(0); s = Nube(1);
            Texture2D t; t = Vineta; t = PanelCrema; t = PanelOscuro; t = PanelRosa; t = BarraFondo; t = BarraRelleno;
            foreach (string i in new[] { "corazon", "corazon_vacio", "gota", "gota_vacia", "bota", "escudo", "espada", "estrella", "pocionVida", "pocionMana", "pocionVel", "antidoto", "bomba", "calavera", "moneda", "bombaFuego", "bombaHielo", "frascoVeneno", "fuego", "hielo", "rayo", "veneno", "fuerza", "flechaEmpuje" }) t = Icono(i);
            foreach (string tn in new[] { "amarillo", "rojo", "verde", "azul", "gris" }) { t = Boton(tn, false); t = Boton(tn, true); }
        }
    }
}
