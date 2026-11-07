// Gatomate - vista. Iconos de los objetos equipables nuevos (cuerpo y arma). Cada uno tiene su silueta y un toque de color.
using System;
using Gatomate.Core;

namespace Gatomate.View
{
    public static partial class Dibujos
    {
        private static readonly Col Cuero = Col.Hex("#8A5A3A"), CueroCl = Col.Hex("#B07A4A"), Acero = Col.Hex("#C0C8D4"), AceroOsc = Col.Hex("#7C8798"),
            Oro = Col.Hex("#F2B632"), Madera = Col.Hex("#8A5A3A"), Rojo = Col.Hex("#E8402E"), Azul = Col.Hex("#4A8FE0"), Verde = Col.Hex("#3FBF6A"), Morado = Col.Hex("#7E5BB0");

        private static void Hoja(Lienzo l, Col hoja, Col puno)
        {
            l.Linea(14, 50, 48, 12, 8, hoja); l.Linea(12, 52, 22, 42, 9, puno); l.Linea(18, 38, 30, 50, 7, Oro);
        }

        private static void Baculo(Lienzo l, Col gema)
        {
            l.Linea(16, 58, 42, 20, 5, Madera);
            Gema(l, 46, 16, 11, gema);
        }

        private static void Armadura(Lienzo l, Col c, Col claro)
        {
            l.Poligono(c, 12, 6, 24, 6, 32, 14, 40, 6, 52, 6, 56, 24, 50, 58, 14, 58, 8, 24);
            l.Poligono(claro, 20, 14, 28, 14, 32, 22, 36, 14, 44, 14, 46, 28, 44, 52, 20, 52, 18, 28);
        }

        private static bool IconoObjeto(string id, Lienzo l)
        {
            switch (id)
            {
                // ------------------------------------------------ mapa
                case "ob_tienda":
                    l.Poligono(Col.Hex("#E8402E"), 32, 6, 60, 52, 4, 52);
                    l.Poligono(Col.Hex("#FF8A70"), 32, 10, 44, 52, 20, 52);
                    l.Poligono(Col.Hex("#5A2A2A"), 32, 28, 40, 52, 24, 52);
                    l.Linea(32, 2, 32, 12, 2.4f, Madera);
                    l.Linea(2, 56, 62, 56, 4, Col.Hex("#6B4A2A"));
                    l.Poligono(Col.Hex("#FFB020"), 52, 38, 56, 48, 48, 48);
                    return true;

                // ------------------------------------------------ CUERPO
                case "ob_armaduraplacas": Armadura(l, AceroOsc, Acero); l.Linea(20, 30, 44, 30, 3, AceroOsc); l.Linea(20, 40, 44, 40, 3, AceroOsc); return true;
                case "ob_capasombras":
                    l.Poligono(Col.Hex("#3A2F55"), 16, 6, 48, 6, 58, 58, 32, 50, 6, 58); l.Poligono(Col.Hex("#5B4A8A"), 22, 12, 42, 12, 48, 48, 32, 42, 16, 48);
                    l.Circulo(32, 10, 4, Oro); return true;
                case "ob_tunicaarcana":
                    l.Poligono(Col.Hex("#4A5FC1"), 18, 6, 46, 6, 56, 58, 8, 58); l.Poligono(Col.Hex("#6F85E8"), 24, 12, 40, 12, 46, 52, 18, 52);
                    l.Poligono(Col.Hex("#FFD84A"), 32, 24, 35, 32, 44, 32, 37, 37, 40, 46, 32, 41, 24, 46, 27, 37, 20, 32, 29, 32); return true;
                case "ob_collarespinas":
                    l.Curva(4f, Cuero, 8, 6, 14, 34, 50, 34, 56, 6);
                    for (int i = 0; i < 5; i++) l.Poligono(Acero, 14 + i * 8, 30 + (i == 2 ? 6 : (i % 3 == 1 ? 2 : 0)), 18 + i * 8, 30 + (i == 2 ? 6 : 2), 16 + i * 8, 48 + (i == 2 ? 8 : 0));
                    return true;
                case "ob_brazaletesalto":
                    l.Anillo(32, 34, 20, 8, Azul); l.Anillo(32, 34, 20, 4, Col.Hex("#9ED0FF"));
                    l.Poligono(Oro, 32, 4, 40, 16, 24, 16); l.Linea(32, 14, 32, 22, 4, Oro); return true;
                case "ob_colmillovampirico":
                    l.Curva(3f, Cuero, 6, 6, 14, 26, 50, 26, 58, 6);
                    l.Poligono(Col.Hex("#F6EDE0"), 24, 28, 40, 28, 32, 58); l.Poligono(Rojo, 29, 28, 35, 28, 32, 40); return true;
                case "ob_botasviento":
                    l.RectRedondeado(14, 6, 22, 38, 8, Col.Hex("#7FD0FF")); l.RectRedondeado(12, 36, 42, 18, 9, Col.Hex("#7FD0FF"));
                    l.Linea(40, 14, 58, 14, 3, Col.Blanco); l.Linea(44, 24, 60, 24, 3, Col.Blanco); l.Linea(40, 34, 56, 34, 3, Col.Blanco); return true;
                case "ob_camisaseda":
                    l.Poligono(Col.Hex("#E8A0D0"), 6, 14, 22, 6, 32, 12, 42, 6, 58, 14, 50, 26, 46, 22, 46, 58, 18, 58, 18, 22, 14, 26);
                    l.Linea(32, 12, 32, 58, 2.4f, Col.Hex("#C070A8")); return true;
                case "ob_mantomusgo":
                    l.Poligono(Col.Hex("#3E7A3A"), 14, 6, 50, 6, 58, 58, 32, 52, 6, 58); l.Elipse(24, 30, 6, 3, Col.Hex("#7AD68A")); l.Elipse(40, 40, 7, 3, Col.Hex("#7AD68A")); l.Elipse(30, 18, 5, 2.4f, Col.Hex("#7AD68A")); return true;
                case "ob_coronagato":
                    l.Poligono(Oro, 8, 50, 8, 20, 20, 34, 32, 12, 44, 34, 56, 20, 56, 50);
                    l.Poligono(Col.Hex("#FFD84A"), 14, 46, 14, 30, 22, 38, 32, 22, 42, 38, 50, 30, 50, 46);
                    l.Circulo(32, 12, 4, Rojo); l.Circulo(8, 20, 3.4f, Azul); l.Circulo(56, 20, 3.4f, Azul); return true;
                case "ob_petohierro": Armadura(l, Col.Hex("#5A6270"), Col.Hex("#8A94A6")); l.Rect(30, 14, 4, 38, Col.Hex("#5A6270")); l.Circulo(32, 32, 5, Oro); return true;
                case "ob_capuchacazador":
                    l.Poligono(Col.Hex("#3E7A3A"), 32, 4, 54, 30, 50, 58, 14, 58, 10, 30); l.Elipse(32, 40, 12, 14, Col.Hex("#1F3A20"));
                    l.Poligono(Col.Hex("#C0C8D4"), 46, 6, 58, 14, 50, 14); return true;
                case "ob_medallonsagrado":
                    l.Curva(3f, Cuero, 8, 4, 14, 24, 50, 24, 56, 4);
                    l.Circulo(32, 40, 17, Oro); l.Circulo(32, 40, 13, Col.Hex("#FFF2B0"));
                    l.RectRedondeado(29, 28, 6, 24, 2, Rojo); l.RectRedondeado(21, 37, 22, 6, 2, Rojo); return true;
                case "ob_amuletolealtad":
                    l.Curva(3f, Cuero, 8, 4, 14, 22, 50, 22, 56, 4);
                    l.Poligono(Col.Hex("#58A6E8"), 18, 24, 46, 24, 46, 40, 32, 58, 18, 40);
                    l.Circulo(32, 36, 6, Col.Hex("#FF8FB0")); return true;

                // ------------------------------------------------ ARMA
                case "ob_espadaafilada": Hoja(l, Col.Hex("#E8F0FA"), Cuero); l.Linea(20, 46, 44, 18, 2.4f, Col.Blanco); return true;
                case "ob_garrashierro":
                    for (int i = 0; i < 3; i++) l.Poligono(Acero, 14 + i * 14, 8, 22 + i * 14, 8, 18 + i * 14, 54);
                    l.RectRedondeado(8, 46, 48, 10, 4, Cuero); return true;
                case "ob_baculocristal": Baculo(l, Col.Hex("#9AD8FF")); return true;
                case "ob_arcolargo":
                    l.Curva(5f, Madera, 46, 6, 14, 14, 14, 50, 46, 58); l.Linea(46, 6, 46, 58, 1.8f, Col.Blanco);
                    l.Linea(14, 32, 56, 32, 2.6f, Acero); l.Poligono(Acero, 60, 32, 50, 27, 50, 37); return true;
                case "ob_dagasangrienta":
                    l.Linea(20, 46, 44, 18, 6, Col.Hex("#D8A0A8")); l.Linea(14, 52, 22, 44, 8, Cuero); l.Linea(18, 40, 28, 50, 5, Oro);
                    l.Circulo(46, 14, 3.4f, Rojo); l.Circulo(50, 24, 2.4f, Rojo); return true;
                case "ob_martillopesado":
                    l.Linea(16, 58, 40, 24, 6, Madera); l.RectRedondeado(28, 6, 32, 20, 5, AceroOsc); l.RectRedondeado(30, 8, 28, 8, 3, Acero); return true;
                case "ob_lanzaperforante":
                    l.Linea(8, 58, 44, 22, 4, Madera); l.Poligono(Acero, 58, 6, 46, 26, 38, 18); l.Poligono(Col.Blanco, 54, 10, 46, 22, 42, 18); return true;
                case "ob_varitachispas":
                    l.Linea(14, 58, 42, 22, 4, Madera); l.Poligono(Col.Hex("#FFD84A"), 50, 2, 38, 18, 46, 18, 36, 32, 58, 14, 48, 14); return true;
                case "ob_cetrollamas": Baculo(l, Col.Hex("#FF7A1A")); l.Poligono(Col.Hex("#FFD84A"), 46, 0, 50, 8, 42, 8); return true;
                case "ob_tridentehielo":
                    l.Linea(32, 58, 32, 20, 4, Col.Hex("#9ED0FF")); l.Linea(14, 20, 50, 20, 4, Col.Hex("#9ED0FF"));
                    l.Poligono(Col.Hex("#7FD0FF"), 14, 4, 18, 22, 10, 22); l.Poligono(Col.Hex("#7FD0FF"), 32, 2, 36, 22, 28, 22); l.Poligono(Col.Hex("#7FD0FF"), 50, 4, 54, 22, 46, 22); return true;
                case "ob_bastontormenta":
                    l.Linea(14, 58, 40, 24, 4, Madera); Gema(l, 44, 16, 10, Col.Hex("#FFD84A"));
                    l.Poligono(Col.Blanco, 54, 28, 48, 38, 52, 38, 46, 50, 58, 34, 54, 34); return true;
                case "ob_aguijonveneno":
                    l.Poligono(Col.Hex("#7BC043"), 16, 56, 26, 40, 50, 6, 30, 48); l.Circulo(18, 54, 5, Col.Hex("#3E7A3A")); l.Circulo(52, 18, 3, Col.Hex("#C8F060")); return true;
                case "ob_guantesboxeo":
                    l.RectRedondeado(10, 12, 40, 36, 14, Rojo); l.RectRedondeado(14, 16, 32, 12, 6, Col.Hex("#FF8070")); l.RectRedondeado(18, 48, 28, 12, 4, Col.Blanco); return true;
                case "ob_escudopinchos":
                    l.Poligono(Col.Hex("#7FA0D0"), 8, 8, 56, 8, 56, 34, 32, 60, 8, 34);
                    for (int i = 0; i < 4; i++) l.Poligono(Acero, 12 + i * 12, 6, 18 + i * 12, 6, 15 + i * 12, -2);
                    l.Circulo(32, 30, 9, Acero); l.Circulo(32, 30, 4, AceroOsc); return true;
                case "ob_cuchillosgemelos":
                    l.Linea(10, 48, 32, 14, 5, Acero); l.Linea(8, 50, 14, 44, 7, Cuero); l.Linea(30, 52, 52, 14, 5, Acero); l.Linea(28, 54, 34, 48, 7, Cuero); return true;
                case "ob_librohechizos":
                    l.RectRedondeado(10, 8, 44, 48, 5, Col.Hex("#5B3F86")); l.RectRedondeado(14, 12, 38, 40, 3, Col.Hex("#F6E7C1"));
                    l.Poligono(Col.Hex("#7E5BB0"), 32, 18, 36, 28, 46, 28, 38, 34, 41, 44, 32, 38, 23, 44, 26, 34, 18, 28, 28, 28); return true;
                case "ob_varasanadora":
                    l.Linea(16, 58, 42, 22, 4, Madera); l.Poligono(Col.Hex("#FF4D6A"), 46, 6, 56, 14, 46, 28, 36, 14); l.Circulo(41, 12, 6, Col.Hex("#FF4D6A")); l.Circulo(51, 12, 6, Col.Hex("#FF4D6A")); return true;
                case "ob_estacavampira":
                    l.Linea(12, 56, 46, 14, 5, Col.Hex("#E8DCC8")); l.Poligono(Col.Hex("#E8DCC8"), 54, 4, 44, 20, 38, 12); l.Circulo(48, 12, 3.4f, Rojo); return true;
                case "ob_hachadoble":
                    l.Linea(18, 58, 40, 20, 5, Madera);
                    l.Poligono(AceroOsc, 40, 8, 58, 14, 58, 34, 40, 38); l.Poligono(AceroOsc, 40, 8, 22, 14, 22, 34, 40, 38);
                    l.Poligono(Acero, 40, 12, 54, 16, 54, 30, 40, 34); return true;
                case "ob_latigoagil":
                    l.Curva(3.4f, Cuero, 8, 54, 8, 26, 36, 44, 40, 14); l.Poligono(Oro, 40, 4, 46, 16, 34, 16); l.RectRedondeado(4, 50, 10, 10, 3, Madera); return true;
                case "ob_pipetaalquimica":
                    l.Rect(26, 6, 12, 14, Col.Hex("#C9D6E8")); l.Poligono(Col.Hex("#C9D6E8"), 26, 18, 38, 18, 54, 54, 10, 54);
                    l.Poligono(Col.Hex("#7BC043"), 22, 36, 42, 36, 50, 52, 14, 52); l.Circulo(28, 42, 3, Col.Blanco.Alfa(0.7f)); return true;
            }
            return false;
        }
    }
}
