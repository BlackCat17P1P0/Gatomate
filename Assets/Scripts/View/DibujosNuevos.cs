// Gatomate - vista. Dibujos procedurales de lo añadido en esta versión: agua, obstáculos rompibles, monedas,
// enemigos nuevos, iconos de elementos/estados y el icono de cada habilidad. Mismo estilo que Dibujos.cs.
using System;
using Gatomate.Core;

namespace Gatomate.View
{
    public static partial class Dibujos
    {
        // ------------------------------------------------------------------ agua
        private static void DecorAgua(Lienzo l, int sd)
        {
            for (int i = 0; i < 5; i++)
            {
                float x, y;
                PuntoRombo(Ruido.Hash(sd, i, 1) * 0.7f + 0.15f, Ruido.Hash(sd, i, 2) * 0.7f + 0.15f, out x, out y);
                float w = 8f + Ruido.Hash(sd, i, 3) * 8f;
                l.Curva(1.8f, Col.Blanco.Alfa(0.75f), x - w, y, x - w * 0.5f, y - 2f, x, y, x + w * 0.5f, y + 2f, x + w, y);
            }
            for (int i = 0; i < 3; i++)
            {
                float x, y;
                PuntoRombo(Ruido.Hash(sd, i, 7) * 0.7f + 0.15f, Ruido.Hash(sd, i, 8) * 0.7f + 0.15f, out x, out y);
                l.Anillo(x, y, 2.6f, 1.1f, Col.Blanco.Alfa(0.55f));
            }
        }

        // ------------------------------------------------------------------ obstáculos que se rompen (caja, roca agrietada, barril)
        public static Imagen Obstaculo(int mundo, Terreno t)
        {
            Paleta p = PaletaMundo(mundo);
            var l = new Lienzo(128, 120);
            float cx = 64, cy = 84;
            switch (t)
            {
                case Terreno.Caja:
                    {
                        float alto = 34, hw = 36, hh = hw / 2f;
                        Col m = Col.Hex("#B8813F");
                        l.Poligono(m.Mul(0.72f), cx - hw, cy, cx, cy + hh, cx, cy + hh - alto, cx - hw, cy - alto);
                        l.Poligono(m.Mul(0.55f), cx, cy + hh, cx + hw, cy, cx + hw, cy - alto, cx, cy + hh - alto);
                        l.Poligono(m.Mul(1.1f), cx, cy - hh - alto, cx + hw, cy - alto, cx, cy + hh - alto, cx - hw, cy - alto);
                        Col marco = Col.Hex("#6B4423");
                        // tablones de la tapa
                        l.Linea(cx - hw / 2, cy - alto - hh / 2, cx + hw / 2, cy - alto + hh / 2, 1.6f, marco.Alfa(0.7f));
                        l.Linea(cx - hw / 2, cy - alto + hh / 2, cx + hw / 2, cy - alto - hh / 2, 1.6f, marco.Alfa(0.7f));
                        // cruces en los costados
                        l.Linea(cx - hw + 3, cy - alto + 3, cx - 3, cy + hh - 3, 2.6f, marco.Alfa(0.9f));
                        l.Linea(cx - hw + 3, cy - 3, cx - 3, cy + hh - alto + 3, 2.6f, marco.Alfa(0.9f));
                        l.Linea(cx + 3, cy + hh - alto + 3, cx + hw - 3, cy - alto + 3, 2.6f, marco.Alfa(0.9f));
                        l.Linea(cx + 3, cy + hh - 3, cx + hw - 3, cy - 3, 2.6f, marco.Alfa(0.9f));
                        l.Circulo(cx - hw + 5, cy - alto + 5, 1.8f, Col.Hex("#E8D0A0"));
                        l.Circulo(cx + hw - 5, cy - alto + 5, 1.8f, Col.Hex("#E8D0A0"));
                        break;
                    }
                case Terreno.RocaFragil:
                    {
                        float alto = 30, hw = 38, hh = hw / 2f;
                        Col c = p.Roca.Mezcla(Col.Hex("#E0D2B0"), 0.4f);
                        l.Poligono(c.Mul(0.72f), cx - hw, cy, cx, cy + hh, cx, cy + hh - alto, cx - hw, cy - alto);
                        l.Poligono(c.Mul(0.55f), cx, cy + hh, cx + hw, cy, cx + hw, cy - alto, cx, cy + hh - alto);
                        l.Poligono(c.Mul(1.1f), cx, cy - hh - alto, cx + hw, cy - alto, cx, cy + hh - alto, cx - hw, cy - alto);
                        Col g = Col.Negro.Alfa(0.7f);
                        l.Curva(2.2f, g, cx - 16, cy - alto - 4, cx - 8, cy - alto + 2, cx - 12, cy - alto + 8, cx - 2, cy - alto + 14);
                        l.Curva(2.0f, g, cx + 10, cy - alto - 6, cx + 6, cy - alto + 1, cx + 14, cy - alto + 6);
                        l.Curva(2.2f, g, cx - 26, cy - 22, cx - 18, cy - 14, cx - 24, cy - 6, cx - 12, cy + 4);
                        l.Curva(2.0f, g, cx + 22, cy - 20, cx + 16, cy - 12, cx + 26, cy - 4);
                        // trocitos sueltos
                        l.Poligono(c.Mul(0.9f), cx - 44, cy + 6, cx - 36, cy + 2, cx - 38, cy + 10);
                        l.Poligono(c.Mul(0.8f), cx + 40, cy + 8, cx + 48, cy + 4, cx + 46, cy + 12);
                        break;
                    }
                default:      // Barril
                    {
                        Col m = Col.Hex("#C0392B");
                        l.Elipse(cx, cy + 2, 25, 12, m.Mul(0.55f));
                        l.RectRedondeado(cx - 25, cy - 46, 50, 48, 4, m.Mul(0.85f));
                        l.Rect(cx - 25, cy - 36, 50, 5, Col.Hex("#3C3F4A"));
                        l.Rect(cx - 25, cy - 12, 50, 5, Col.Hex("#3C3F4A"));
                        l.Elipse(cx, cy - 46, 25, 12, m.Mul(1.15f));
                        l.Elipse(cx, cy - 46, 18, 8, m.Mul(0.78f));
                        l.Linea(cx + 4, cy - 50, cx + 12, cy - 62, 2.6f, Col.Hex("#C98A3A"));
                        l.Circulo(cx + 13, cy - 63, 3.6f, Col.Hex("#FFB020")); l.Circulo(cx + 13, cy - 63, 1.8f, Col.Blanco);
                        l.Poligono(Col.Hex("#FFB020"), cx, cy - 33, cx + 8, cy - 20, cx, cy - 11, cx - 8, cy - 20);
                        l.Poligono(Col.Hex("#FFE066"), cx, cy - 26, cx + 4, cy - 19, cx, cy - 15, cx - 4, cy - 19);
                        l.Elipse(cx - 17, cy - 24, 3.5f, 9, Col.Blanco.Alfa(0.25f));
                        break;
                    }
            }
            return Cierra(l, cy, 120, 2f);
        }

        // ------------------------------------------------------------------ monedas en el suelo (1, 2 o 3+ monedas)
        private static void MonedaSuelta(Lienzo l, float x, float y, float r)
        {
            l.Elipse(x, y + 2, r * 0.95f, r * 0.55f, Col.Negro.Alfa(0.25f));
            l.Elipse(x, y, r * 0.9f, r, Col.Hex("#B5790D"));
            l.Elipse(x, y - 0.6f, r * 0.74f, r * 0.86f, Col.Hex("#FFD84A"));
            l.Elipse(x - r * 0.2f, y - r * 0.35f, r * 0.22f, r * 0.4f, Col.Blanco.Alfa(0.75f));
            l.Linea(x + r * 0.15f, y - r * 0.4f, x + r * 0.15f, y + r * 0.45f, 1.5f, Col.Hex("#B5790D"));
        }

        public static Imagen Moneda(int n)
        {
            var l = new Lienzo(64, 64);
            if (n <= 1) MonedaSuelta(l, 32, 34, 10);
            else if (n == 2) { MonedaSuelta(l, 23, 38, 9); MonedaSuelta(l, 41, 33, 9); }
            else { MonedaSuelta(l, 21, 40, 9); MonedaSuelta(l, 43, 39, 9); MonedaSuelta(l, 32, 28, 9); }
            Imagen im = l.Resolver(1.6f, Cont);
            im.PivX = 0.5f; im.PivYAbajo = (64f - 42f) / 64f;
            return im;
        }

        // ------------------------------------------------------------------ enemigos nuevos (96x96, pies en y=88)
        private static void RataBase(Lienzo l, Col c)
        {
            l.Bezier(68, 78, 94, 74, 88, 50, 3.2f, Col.Hex("#E8A0A8"));
            l.Elipse(48, 70, 24, 16, c);
            l.Elipse(46, 76, 14, 8, c.Mezcla(Col.Blanco, 0.4f));
            l.Elipse(30, 66, 14, 11, c.Mul(1.1f));
            l.Poligono(c.Mul(1.1f), 20, 60, 8, 69, 21, 74);
            l.Circulo(9, 69, 2.6f, Col.Hex("#FF8FA0"));
            l.Circulo(35, 53, 8.5f, c.Mul(1.1f)); l.Circulo(35, 53, 4.6f, Col.Hex("#FF9FB5"));
            l.Circulo(47, 56, 7f, c.Mul(1.1f)); l.Circulo(47, 56, 3.8f, Col.Hex("#FF9FB5"));
            l.Circulo(27, 63, 3f, Col.Blanco); l.Circulo(27.5f, 63.5f, 1.8f, Col.Negro);
            l.Linea(14, 67, 3, 62, 1.1f, Col.Blanco); l.Linea(14, 70, 3, 72, 1.1f, Col.Blanco);
            l.Elipse(32, 84, 6, 3.4f, c.Mul(0.8f)); l.Elipse(58, 84, 6, 3.4f, c.Mul(0.8f));
        }

        private static void SlimeBase(Lienzo l, Col c)
        {
            l.Poligono(c, 18, 88, 22, 56, 36, 40, 60, 40, 74, 56, 78, 88);
            l.Elipse(48, 82, 32, 10, c.Mul(0.8f)); l.Elipse(48, 60, 28, 26, c);
            l.Elipse(38, 50, 7, 4, Col.Blanco.Alfa(0.35f));
            l.Elipse(40, 62, 5.2f, 6, Col.Blanco); l.Elipse(58, 62, 5.2f, 6, Col.Blanco); l.Circulo(41, 63, 2.8f, Col.Negro); l.Circulo(59, 63, 2.8f, Col.Negro);
        }

        private static bool DibujarEnemigoNuevo(string id, Lienzo l, Col c)
        {
            switch (id)
            {
                case "nido":
                    l.Elipse(48, 74, 38, 16, c.Mul(0.75f));
                    l.Elipse(48, 66, 33, 20, c);
                    for (int i = 0; i < 10; i++)
                        l.Linea(18 + i * 6.5f, 70 - (i % 3) * 4, 22 + i * 6.5f - (i % 2) * 6, 46 + (i % 4) * 4, 2.2f, c.Mul(1.25f));
                    l.Elipse(30, 70, 6.5f, 4.5f, Col.Negro); l.Elipse(64, 74, 7.5f, 5f, Col.Negro); l.Elipse(48, 58, 6.5f, 4.5f, Col.Negro);
                    l.Circulo(62, 73, 1.7f, Col.Hex("#FF4040")); l.Circulo(67, 73, 1.7f, Col.Hex("#FF4040"));
                    l.Circulo(46, 57, 1.5f, Col.Hex("#FF4040")); l.Circulo(51, 57, 1.5f, Col.Hex("#FF4040"));
                    return true;
                case "rata":
                    RataBase(l, c);
                    return true;
                case "ladron":
                    RataBase(l, c);
                    l.Rect(18, 58, 22, 6.5f, Col.Negro);               // antifaz
                    l.Circulo(27, 62, 2.6f, Col.Blanco); l.Circulo(27.5f, 62.5f, 1.5f, Col.Negro);
                    l.Circulo(70, 72, 13, Col.Hex("#C9A15A")); l.Rect(65, 56, 10, 6, Col.Hex("#C9A15A"));
                    l.Rect(63, 59, 14, 3, Col.Hex("#7A4B2A"));
                    l.Elipse(70, 73, 6, 7, Col.Hex("#FFD84A")); l.Elipse(70, 73, 3.6f, 4.6f, Col.Hex("#B5790D"));
                    return true;
                case "bombardero":
                    l.Elipse(48, 66, 25, 22, c.Mul(1.1f)); l.Elipse(48, 74, 15, 11, c.Mezcla(Col.Blanco, 0.4f));
                    l.Poligono(c.Mul(1.1f), 26, 52, 20, 26, 40, 44); l.Poligono(c.Mul(1.1f), 70, 52, 76, 26, 56, 44);
                    l.Elipse(48, 54, 22, 16, c.Mul(1.2f));
                    OjosEnojados(l, 40, 56, 54, 5.6f, Col.Hex("#FFE066"));
                    l.Curva(1.6f, Col.Negro, 40, 66, 48, 70, 56, 66);
                    l.Rect(30, 80, 36, 5, Col.Hex("#3C3F4A"));
                    l.Linea(48, 38, 58, 24, 3f, Col.Hex("#C98A3A"));
                    l.Circulo(59, 22, 5.4f, Col.Hex("#FFB020")); l.Circulo(59, 22, 2.6f, Col.Blanco);
                    return true;
                case "slime_fuego":
                    SlimeBase(l, c);
                    l.Poligono(Col.Hex("#FFB020"), 36, 40, 42, 20, 48, 34, 54, 14, 60, 40);
                    l.Poligono(Col.Hex("#FFE066"), 42, 40, 48, 28, 54, 40);
                    return true;
                case "slime_hielo":
                    SlimeBase(l, c);
                    l.Poligono(Col.Hex("#E4F6FF"), 34, 42, 38, 16, 44, 40);
                    l.Poligono(Col.Hex("#D2EEFF"), 46, 40, 52, 10, 58, 40);
                    l.Poligono(Col.Hex("#E4F6FF"), 58, 42, 66, 22, 68, 46);
                    return true;
                case "sapo":
                    l.Elipse(48, 70, 33, 19, c); l.Elipse(48, 80, 22, 9, c.Mezcla(Col.Blanco, 0.45f));
                    l.Circulo(30, 48, 11, c.Mul(1.1f)); l.Circulo(66, 48, 11, c.Mul(1.1f));
                    l.Circulo(30, 47, 7.4f, Col.Blanco); l.Circulo(66, 47, 7.4f, Col.Blanco);
                    l.Elipse(31, 48, 2.8f, 5.4f, Col.Negro); l.Elipse(65, 48, 2.8f, 5.4f, Col.Negro);
                    l.Curva(2.2f, Col.Negro, 18, 66, 33, 74, 48, 76, 63, 74, 78, 66);
                    for (int i = 0; i < 6; i++) l.Circulo(26 + i * 9, 62 + (i % 2) * 8, 2.8f, Col.Hex("#8E44AD"));
                    l.Elipse(18, 86, 9, 3.6f, c.Mul(0.8f)); l.Elipse(78, 86, 9, 3.6f, c.Mul(0.8f));
                    return true;
                case "anguila":
                    l.Bezier(8, 72, 26, 36, 48, 72, 14f, c.Mul(0.9f));
                    l.Bezier(48, 72, 68, 106, 84, 62, 12f, c);
                    l.Bezier(10, 72, 26, 40, 48, 70, 4f, c.Mezcla(Col.Blanco, 0.5f));
                    l.Circulo(86, 58, 10, c.Mul(1.1f));
                    l.Circulo(88, 55, 3.4f, Col.Blanco); l.Circulo(89, 55, 1.8f, Col.Negro);
                    l.Curva(1.8f, Col.Negro, 82, 63, 88, 65, 94, 61);
                    l.Poligono(Col.Blanco, 30, 26, 36, 26, 33, 36, 39, 36, 28, 52, 31, 40, 26, 40);
                    l.Poligono(Col.Blanco, 64, 40, 70, 40, 67, 50, 73, 50, 62, 66, 65, 54, 60, 54);
                    return true;
                case "chaman":
                    l.Poligono(c, 22, 88, 28, 48, 68, 48, 74, 88, 62, 82, 54, 90, 42, 82, 34, 90);
                    l.Elipse(48, 46, 17, 16, Col.Hex("#EDE3CF"));
                    l.Elipse(41, 46, 4, 5.4f, Col.Negro); l.Elipse(55, 46, 4, 5.4f, Col.Negro);
                    l.Curva(1.6f, Col.Negro, 41, 56, 48, 58, 55, 56);
                    l.Poligono(Col.Hex("#E74C3C"), 34, 36, 24, 10, 44, 32);
                    l.Poligono(Col.Hex("#F1C40F"), 44, 33, 48, 4, 54, 32);
                    l.Poligono(Col.Hex("#3498DB"), 56, 34, 70, 12, 60, 38);
                    l.Linea(80, 88, 80, 34, 3.4f, Col.Hex("#6A4A2A"));
                    l.Circulo(80, 28, 7, Col.Hex("#F0E8DC")); l.Circulo(77.5f, 28, 1.8f, Col.Negro); l.Circulo(82.5f, 28, 1.8f, Col.Negro);
                    return true;
            }
            return false;
        }

        // ------------------------------------------------------------------ iconos nuevos de la interfaz
        private static bool IconoNuevo(string id, Lienzo l)
        {
            if (IconoClaves(id, l)) return true;
            switch (id)
            {
                case "moneda":
                    l.Circulo(32, 32, 25, Col.Hex("#B5790D")); l.Circulo(32, 31, 21, Col.Hex("#FFD84A"));
                    l.Anillo(32, 31, 14, 2.6f, Col.Hex("#B5790D"));
                    l.Rect(30, 22, 4, 20, Col.Hex("#B5790D"));
                    l.Elipse(22, 20, 4, 7, Col.Blanco.Alfa(0.7f));
                    return true;
                case "bombaFuego":
                    l.Linea(40, 16, 48, 6, 3, Col.Hex("#C98A3A")); l.Circulo(50, 6, 4, Col.Hex("#FFB020"));
                    l.Circulo(30, 38, 21, Col.Hex("#C0392B")); l.Elipse(23, 30, 6, 4, Col.Blanco.Alfa(0.5f)); l.Rect(24, 12, 14, 8, Col.Hex("#5A5E6C"));
                    l.Poligono(Col.Hex("#FFB020"), 30, 28, 38, 42, 30, 52, 22, 42);
                    return true;
                case "bombaHielo":
                    l.Linea(40, 16, 48, 6, 3, Col.Hex("#C98A3A")); l.Circulo(50, 6, 4, Col.Hex("#9EE0FF"));
                    l.Circulo(30, 38, 21, Col.Hex("#4FA3D9")); l.Elipse(23, 30, 6, 4, Col.Blanco.Alfa(0.6f)); l.Rect(24, 12, 14, 8, Col.Hex("#5A5E6C"));
                    l.Linea(30, 26, 30, 50, 2.6f, Col.Blanco); l.Linea(19, 32, 41, 44, 2.6f, Col.Blanco); l.Linea(19, 44, 41, 32, 2.6f, Col.Blanco);
                    return true;
                case "frascoVeneno":
                    l.Rect(25, 4, 14, 8, Col.Hex("#8A5A3A"));
                    l.Rect(26, 10, 12, 16, Col.Hex("#D8F2FF"));
                    l.Poligono(Col.Hex("#D8F2FF"), 26, 24, 38, 24, 54, 46, 54, 52, 10, 52, 10, 46);
                    l.Circulo(32, 48, 20, Col.Hex("#D8F2FF"));
                    l.Elipse(32, 50, 17, 14, Col.Hex("#8E44AD"));
                    l.Circulo(28, 48, 3, Col.Hex("#E8D0F5")); l.Circulo(37, 53, 2.2f, Col.Hex("#E8D0F5"));
                    return true;
                case "fuego":
                    l.Poligono(Col.Hex("#FF7A1A"), 32, 4, 46, 26, 54, 40, 46, 56, 32, 60, 18, 56, 10, 40, 20, 28, 24, 16);
                    l.Poligono(Col.Hex("#FFD84A"), 32, 24, 40, 40, 36, 54, 28, 54, 24, 42);
                    return true;
                case "hielo":
                    l.Linea(32, 6, 32, 58, 5, Col.Hex("#BDE9FF")); l.Linea(9, 19, 55, 45, 5, Col.Hex("#BDE9FF")); l.Linea(9, 45, 55, 19, 5, Col.Hex("#BDE9FF"));
                    l.Circulo(32, 32, 6, Col.Blanco);
                    return true;
                case "rayo":
                    l.Poligono(Col.Hex("#FFD84A"), 38, 3, 14, 34, 28, 34, 22, 61, 50, 26, 34, 26);
                    return true;
                case "veneno":
                    l.Poligono(Col.Hex("#7BC043"), 32, 4, 50, 34, 14, 34);
                    l.Circulo(32, 40, 19, Col.Hex("#7BC043"));
                    l.Circulo(26, 38, 3.4f, Col.Hex("#D8F5B0")); l.Circulo(38, 46, 2.4f, Col.Hex("#D8F5B0"));
                    return true;
                case "fuerza":
                    l.Poligono(Col.Hex("#FF6A3D"), 32, 4, 56, 32, 40, 32, 40, 58, 24, 58, 24, 32, 8, 32);
                    return true;
                case "flechaEmpuje":
                    l.Poligono(Col.Hex("#FFFFFF"), 6, 24, 38, 24, 38, 10, 60, 32, 38, 54, 38, 40, 6, 40);
                    return true;
            }
            return false;
        }

        // ------------------------------------------------------------------ icono de cada habilidad
        /// <summary>Icono redondo: el color indica el elemento (o la clase si es físico) y el dibujo, qué hace.</summary>
        public static Imagen IconoHabilidadDef(HabilidadDef h, string clase)
        {
            var l = new Lienzo(64, 64);
            Col fondo = Col.Hex(GameData.Clase(clase).ColorHex);
            switch (h.Elemento)
            {
                case Elemento.Fuego: fondo = Col.Hex("#E8642A"); break;
                case Elemento.Hielo: fondo = Col.Hex("#4FA3D9"); break;
                case Elemento.Rayo: fondo = Col.Hex("#D8A917"); break;
                case Elemento.Veneno: fondo = Col.Hex("#6FA83A"); break;
            }
            if (h.Movilidad) fondo = fondo.Mezcla(Col.Hex("#3C7FD0"), 0.4f);
            l.Circulo(32, 32, 29, fondo.Mul(0.72f)); l.Circulo(32, 30, 26, fondo.Mezcla(Col.Blanco, 0.2f));
            Col b = Col.Blanco;
            switch (h.Efecto)
            {
                case EfectoHab.Golpe:
                    if (h.RangoMax > 1) { l.Linea(14, 50, 50, 14, 3.4f, b); l.Poligono(b, 54, 10, 42, 14, 50, 22); l.Poligono(b, 12, 52, 20, 44, 14, 40); }
                    else l.Linea(16, 48, 48, 16, 6f, b);
                    break;
                case EfectoHab.AreaCruz:
                    l.Circulo(32, 32, 6, b); l.Circulo(32, 14, 5, b); l.Circulo(32, 50, 5, b); l.Circulo(14, 32, 5, b); l.Circulo(50, 32, 5, b);
                    break;
                case EfectoHab.AreaAlrededor:
                    l.Anillo(32, 32, 16, 4f, b); l.Circulo(32, 32, 5, b);
                    break;
                case EfectoHab.Curar:
                case EfectoHab.CuraArea:
                    l.RectRedondeado(27, 12, 10, 40, 4, b); l.RectRedondeado(12, 27, 40, 10, 4, b);
                    break;
                case EfectoHab.Provocar:
                    l.RectRedondeado(28, 10, 8, 28, 3, b); l.Circulo(32, 48, 5, b);
                    break;
                case EfectoHab.ZonaLinea:
                    l.RectRedondeado(10, 26, 12, 12, 3, b); l.RectRedondeado(26, 26, 12, 12, 3, b); l.RectRedondeado(42, 26, 12, 12, 3, b);
                    break;
                case EfectoHab.Teletransporte:
                case EfectoHab.TrasEnemigo:
                case EfectoHab.Intercambio:
                    l.Poligono(b, 8, 24, 36, 24, 36, 12, 58, 32, 36, 52, 36, 40, 8, 40);
                    l.Circulo(12, 12, 3, b); l.Circulo(22, 8, 2.4f, b);
                    break;
                case EfectoHab.Embestida:
                case EfectoHab.Carga:
                    l.Poligono(b, 6, 22, 34, 22, 34, 12, 58, 32, 34, 52, 34, 42, 6, 42);
                    l.Linea(4, 30, 18, 30, 3, b); l.Linea(4, 36, 18, 36, 3, b);
                    break;
                case EfectoHab.Escudo:
                    l.Poligono(b, 16, 14, 48, 14, 48, 34, 32, 54, 16, 34);
                    l.Linea(32, 20, 32, 38, 4.4f, fondo.Mul(0.7f)); l.Circulo(32, 45, 2.8f, fondo.Mul(0.7f));
                    break;
                case EfectoHab.Potenciar:
                    l.Poligono(b, 32, 8, 54, 32, 40, 32, 40, 54, 24, 54, 24, 32, 10, 32);
                    break;
                default:
                    l.Circulo(32, 32, 8, b);
                    break;
            }
            if (h.EmpujeDist > 0)      // flechita pequeña abajo a la derecha
            {
                l.Circulo(48, 48, 11, fondo.Mul(0.55f));
                l.Poligono(b, 41, 45, 49, 45, 49, 41, 56, 48, 49, 55, 49, 51, 41, 51);
            }
            Imagen im = l.Resolver(2f, Cont); im.PivX = 0.5f; im.PivYAbajo = 0.5f; return im;
        }
    }
}
