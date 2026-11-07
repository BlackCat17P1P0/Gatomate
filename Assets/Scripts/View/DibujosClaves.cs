// Gatomate - vista. Iconos de las palabras clave (daño, rango, empuje...), de las estadísticas y de los objetos de equipo.
// Mismo estilo que Dibujos.cs: formas planas con contorno, dibujadas por código en un lienzo de 64x64.
using System;
using Gatomate.Core;

namespace Gatomate.View
{
    public static partial class Dibujos
    {
        private static Col ColElem(string e)
        {
            switch (e)
            {
                case "fuego": return Col.Hex("#FF7A1A");
                case "hielo": return Col.Hex("#7FD0FF");
                case "rayo": return Col.Hex("#FFD84A");
                default: return Col.Hex("#7BC043");
            }
        }

        private static void Gema(Lienzo l, float cx, float cy, float r, Col c)
        {
            l.Poligono(c.Mul(0.75f), cx, cy - r, cx + r, cy - r * 0.2f, cx, cy + r, cx - r, cy - r * 0.2f);
            l.Poligono(c, cx, cy - r * 0.85f, cx + r * 0.8f, cy - r * 0.2f, cx, cy + r * 0.8f, cx - r * 0.8f, cy - r * 0.2f);
            l.Poligono(Col.Blanco.Alfa(0.7f), cx - r * 0.1f, cy - r * 0.7f, cx + r * 0.35f, cy - r * 0.25f, cx - r * 0.1f, cy - r * 0.2f, cx - r * 0.45f, cy - r * 0.25f);
        }

        private static bool IconoClaves(string id, Lienzo l)
        {
            Col oro = Col.Hex("#F2B632"), oroOsc = Col.Hex("#B5790D");
            if (id.StartsWith("ob_")) return IconoObjeto(id, l);
            switch (id)
            {
                // ------------------------------------------------ palabras clave
                case "dmg":
                    l.Poligono(Col.Hex("#E8402E"), 32, 2, 39, 20, 58, 14, 46, 30, 62, 38, 44, 42, 48, 60, 32, 48, 16, 60, 20, 42, 2, 38, 18, 30, 6, 14, 25, 20);
                    l.Poligono(Col.Hex("#FFB02E"), 32, 14, 37, 26, 48, 26, 40, 34, 44, 46, 32, 40, 20, 46, 24, 34, 16, 26, 27, 26);
                    return true;
                case "rango":
                    l.Anillo(32, 32, 22, 5, Col.Hex("#F0C040")); l.Anillo(32, 32, 10, 4, Col.Hex("#F0C040"));
                    l.Linea(32, 2, 32, 16, 4.4f, Col.Hex("#F0C040")); l.Linea(32, 48, 32, 62, 4.4f, Col.Hex("#F0C040"));
                    l.Linea(2, 32, 16, 32, 4.4f, Col.Hex("#F0C040")); l.Linea(48, 32, 62, 32, 4.4f, Col.Hex("#F0C040"));
                    return true;
                case "gratis":
                    l.Circulo(32, 32, 26, Col.Hex("#3E9E56")); l.Circulo(32, 32, 21, Col.Hex("#6CCB7A"));
                    l.Anillo(32, 32, 10, 5.2f, Col.Blanco);
                    return true;
                case "libre":
                    l.Poligono(Col.Hex("#58A6E8"), 4, 14, 22, 32, 4, 50, 14, 50, 32, 32, 14, 14);
                    l.Poligono(Col.Hex("#9ED0FF"), 28, 14, 46, 32, 28, 50, 38, 50, 56, 32, 38, 14);
                    return true;
                case "espalda":
                    l.Circulo(32, 32, 27, Col.Hex("#5B3F86")); l.Circulo(32, 31, 23, Col.Hex("#7E5BB0"));
                    l.Poligono(Col.Blanco, 29, 8, 35, 8, 36, 38, 32, 50, 28, 38);
                    l.Rect(22, 36, 20, 5, Col.Hex("#E8B030")); l.Rect(30, 40, 4, 10, Col.Hex("#C98A3A"));
                    return true;
                case "aislado":
                    l.Anillo(32, 32, 24, 3.4f, Col.Hex("#C9B8E8")); l.Circulo(32, 32, 9, Col.Hex("#FFB4C8"));
                    l.Linea(12, 12, 20, 20, 3.4f, Col.Hex("#C9B8E8")); l.Linea(52, 12, 44, 20, 3.4f, Col.Hex("#C9B8E8"));
                    l.Linea(12, 52, 20, 44, 3.4f, Col.Hex("#C9B8E8")); l.Linea(52, 52, 44, 44, 3.4f, Col.Hex("#C9B8E8"));
                    return true;
                case "fisico":
                    l.Circulo(32, 32, 22, Col.Hex("#9A94A6")); l.Circulo(32, 31, 18, Col.Hex("#C6C0D2"));
                    l.Linea(22, 34, 42, 34, 3.6f, Col.Hex("#9A94A6"));
                    return true;
                case "cruz":
                    l.RectRedondeado(24, 24, 16, 16, 4, Col.Hex("#F0C040")); l.RectRedondeado(24, 4, 16, 16, 4, Col.Hex("#F0C040"));
                    l.RectRedondeado(24, 44, 16, 16, 4, Col.Hex("#F0C040")); l.RectRedondeado(4, 24, 16, 16, 4, Col.Hex("#F0C040")); l.RectRedondeado(44, 24, 16, 16, 4, Col.Hex("#F0C040"));
                    return true;
                case "amigo":
                    l.Circulo(24, 28, 12, Col.Hex("#F0A050")); l.Circulo(40, 28, 12, Col.Hex("#F07070"));
                    l.Linea(8, 54, 56, 8, 5f, Col.Hex("#D93B3B")); l.Linea(8, 8, 56, 54, 0.1f, Col.Hex("#D93B3B"));
                    return true;
                case "alrededor":
                    l.Anillo(32, 32, 24, 5, Col.Hex("#F0C040")); l.Circulo(32, 32, 8, Col.Hex("#FFE9A0"));
                    return true;
                case "carga":
                    l.Poligono(Col.Hex("#E07040"), 6, 22, 36, 22, 36, 10, 60, 32, 36, 54, 36, 42, 6, 42);
                    l.Linea(2, 28, 16, 28, 3.4f, Col.Blanco); l.Linea(2, 36, 16, 36, 3.4f, Col.Blanco);
                    return true;
                case "cura":
                    l.RectRedondeado(23, 6, 18, 52, 6, Col.Hex("#3FBF6A")); l.RectRedondeado(6, 23, 52, 18, 6, Col.Hex("#3FBF6A"));
                    l.RectRedondeado(27, 12, 10, 40, 4, Col.Hex("#8FF0A8")); l.RectRedondeado(12, 27, 40, 10, 4, Col.Hex("#8FF0A8"));
                    return true;
                case "limpiar":
                    l.Poligono(Col.Hex("#BDE9FF"), 32, 2, 38, 26, 62, 32, 38, 38, 32, 62, 26, 38, 2, 32, 26, 26);
                    l.Circulo(32, 32, 5, Col.Blanco);
                    return true;
                case "provocar":
                    l.Circulo(32, 32, 27, Col.Hex("#C93A4A")); l.Circulo(32, 31, 23, Col.Hex("#F0586A"));
                    l.RectRedondeado(28, 12, 8, 24, 3, Col.Blanco); l.Circulo(32, 46, 4.6f, Col.Blanco);
                    return true;
                case "defensa":
                    l.Poligono(Col.Hex("#7FA0D0"), 8, 8, 56, 8, 56, 34, 32, 60, 8, 34);
                    l.Poligono(Col.Hex("#A8C4EC"), 32, 12, 52, 12, 52, 33, 32, 54);
                    return true;
                case "potenciar":
                    l.Poligono(Col.Hex("#FF6A3D"), 32, 4, 58, 32, 42, 32, 42, 58, 22, 58, 22, 32, 6, 32);
                    l.Poligono(Col.Hex("#FFB08A"), 32, 14, 46, 30, 38, 30, 38, 52, 26, 52, 26, 30, 18, 30);
                    return true;
                case "salto":
                    l.Curva(5f, Col.Hex("#B58CF0"), 8, 50, 20, 12, 44, 12, 54, 40);
                    l.Poligono(Col.Hex("#B58CF0"), 46, 38, 62, 36, 54, 52);
                    l.Circulo(10, 52, 5, Col.Hex("#E8D8FF"));
                    return true;
                case "intercambio":
                    l.Poligono(Col.Hex("#58A6E8"), 6, 18, 40, 18, 40, 8, 58, 22, 40, 36, 40, 26, 6, 26);
                    l.Poligono(Col.Hex("#F5B642"), 58, 38, 24, 38, 24, 28, 6, 42, 24, 56, 24, 46, 58, 46);
                    return true;
                case "embestida":
                    l.Poligono(Col.Hex("#9ED0FF"), 6, 22, 36, 22, 36, 10, 60, 32, 36, 54, 36, 42, 6, 42);
                    return true;
                case "enredadera":
                    l.Curva(5f, Col.Hex("#3E9E56"), 10, 58, 14, 34, 40, 36, 38, 10);
                    l.Elipse(18, 40, 8, 4, Col.Hex("#6CCB7A")); l.Elipse(46, 24, 8, 4, Col.Hex("#6CCB7A")); l.Elipse(30, 20, 7, 3.6f, Col.Hex("#6CCB7A"));
                    return true;
                case "zona_fuego": return IconoNuevo("fuego", l);
                case "zona_veneno": return IconoNuevo("veneno", l);
                case "zona_enredadera": return IconoClaves("enredadera", l);
                case "zona_frasco": return IconoNuevo("frascoVeneno", l);
                case "hierba":
                    l.Poligono(Col.Hex("#3E9E56"), 10, 58, 18, 14, 26, 58);
                    l.Poligono(Col.Hex("#58C070"), 24, 58, 32, 6, 40, 58);
                    l.Poligono(Col.Hex("#3E9E56"), 38, 58, 46, 18, 54, 58);
                    return true;

                // ------------------------------------------------ estadísticas
                case "st_def": return IconoClaves("defensa", l);
                case "st_vel":
                    l.Poligono(Col.Hex("#F59A3A"), 4, 10, 24, 32, 4, 54, 14, 54, 34, 32, 14, 10);
                    l.Poligono(Col.Hex("#FFD08A"), 30, 10, 50, 32, 30, 54, 40, 54, 60, 32, 40, 10);
                    return true;
                case "st_fue": return IconoNuevo("fuerza", l);
                case "st_int":
                    l.RectRedondeado(8, 10, 48, 44, 5, Col.Hex("#4A5FC1")); l.RectRedondeado(12, 14, 40, 36, 3, Col.Hex("#F6E7C1"));
                    l.Poligono(Col.Hex("#4A5FC1"), 32, 18, 36, 28, 46, 28, 38, 34, 41, 44, 32, 38, 23, 44, 26, 34, 18, 28, 28, 28);
                    return true;
                case "st_des":
                    l.Linea(8, 56, 52, 12, 5, Col.Hex("#8A5A3A")); l.Poligono(Col.Hex("#C0C8D4"), 60, 4, 44, 10, 54, 20);
                    l.Poligono(Col.Hex("#58C070"), 8, 56, 8, 44, 18, 56);
                    return true;
                case "st_sue":
                    l.Circulo(22, 22, 12, Col.Hex("#3FBF6A")); l.Circulo(42, 22, 12, Col.Hex("#3FBF6A"));
                    l.Circulo(22, 42, 12, Col.Hex("#3FBF6A")); l.Circulo(42, 42, 12, Col.Hex("#3FBF6A"));
                    l.Circulo(32, 32, 6, Col.Hex("#8FF0A8")); l.Linea(32, 36, 36, 62, 4, Col.Hex("#2E8A4A"));
                    return true;

                // ------------------------------------------------ objetos de equipo
                case "it_collar_fue": case "it_collar_int": case "it_collar_des":
                    {
                        Col g = id == "it_collar_fue" ? Col.Hex("#E8402E") : (id == "it_collar_int" ? Col.Hex("#4A8FE0") : Col.Hex("#3FBF6A"));
                        l.Curva(4f, Col.Hex("#8A5A3A"), 8, 6, 14, 34, 50, 34, 56, 6);
                        Gema(l, 32, 46, 11, g);
                        return true;
                    }
                case "it_chaleco":
                    l.Poligono(Col.Hex("#8A5A3A"), 12, 6, 24, 6, 32, 16, 40, 6, 52, 6, 56, 24, 50, 58, 14, 58, 8, 24);
                    l.Poligono(Col.Hex("#B07A4A"), 20, 14, 28, 14, 32, 22, 36, 14, 44, 14, 46, 28, 44, 52, 20, 52, 18, 28);
                    l.Linea(32, 22, 32, 52, 3, Col.Hex("#6B4020"));
                    return true;
                case "it_trebol": return IconoClaves("st_sue", l);
                case "it_cascabel":
                    l.Circulo(32, 34, 21, oroOsc); l.Circulo(32, 33, 18, oro);
                    l.Linea(14, 40, 50, 40, 3, oroOsc); l.Circulo(32, 52, 4.4f, oroOsc);
                    l.Elipse(24, 26, 5, 8, Col.Blanco.Alfa(0.6f)); l.Circulo(32, 8, 4, oroOsc);
                    return true;
                case "it_anillo_fuego": case "it_anillo_hielo": case "it_anillo_rayo": case "it_anillo_veneno":
                    {
                        Col g = ColElem(id.Substring(10));
                        l.Anillo(32, 40, 17, 7, oroOsc); l.Anillo(32, 39, 16, 5, oro);
                        Gema(l, 32, 14, 10, g);
                        return true;
                    }
                case "it_gema_fuego": case "it_gema_hielo": case "it_gema_rayo": case "it_gema_veneno":
                    Gema(l, 32, 32, 25, ColElem(id.Substring(8)));
                    return true;
            }
            return false;
        }
    }
}
