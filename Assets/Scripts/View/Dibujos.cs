// Gatomate - vista. Catálogo de dibujos procedurales (casillas isométricas, gatos, enemigos, partículas, iconos).
// Todo se dibuja por código: no hay imágenes en el proyecto. Quien haga la estética puede reemplazar
// estos dibujos por arte real sin tocar las reglas. Sin dependencias de Unity.
using System;
using System.Collections.Generic;
using Gatomate.Core;

namespace Gatomate.View
{
    public sealed class Paleta
    {
        public Col Top, Lado, Detalle, Hierba, Roca, Cielo1, Cielo2;
    }

    public static partial class Dibujos
    {
        public static readonly Col Cont = Col.Contorno;

        public static Paleta PaletaMundo(int mundo)
        {
            Func<string, Col> h = delegate (string s) { return Col.Hex(s); };
            switch (mundo)
            {
                case 1: return new Paleta { Top = h("#D98C5F"), Lado = h("#8E4A3A"), Detalle = h("#B5694A"), Hierba = h("#6FAE52"), Roca = h("#8C8FA0"), Cielo1 = h("#6EA8E8"), Cielo2 = h("#FFD2A1") };
                case 2: return new Paleta { Top = h("#86C46F"), Lado = h("#4F7A3E"), Detalle = h("#5E9A4F"), Hierba = h("#4E9B3E"), Roca = h("#8A8F7A"), Cielo1 = h("#8FD3B0"), Cielo2 = h("#EAF7D0") };
                case 3: return new Paleta { Top = h("#8A9B55"), Lado = h("#5A6538"), Detalle = h("#6F7C45"), Hierba = h("#5F8A3A"), Roca = h("#7B7A62"), Cielo1 = h("#9FB070"), Cielo2 = h("#D6D9A0") };
                case 4: return new Paleta { Top = h("#7C7AA6"), Lado = h("#4B4A73"), Detalle = h("#615F8C"), Hierba = h("#5E8F6E"), Roca = h("#5B5A78"), Cielo1 = h("#221F3D"), Cielo2 = h("#4B4A73") };
                case 5: return new Paleta { Top = h("#E8C77F"), Lado = h("#B98E4E"), Detalle = h("#CFA862"), Hierba = h("#8FAF4F"), Roca = h("#B87F55"), Cielo1 = h("#FFC27A"), Cielo2 = h("#FFEBB5") };
                case 6: return new Paleta { Top = h("#CBEAF7"), Lado = h("#86B8D4"), Detalle = h("#E8F7FD"), Hierba = h("#9FD0C8"), Roca = h("#7FA0C0"), Cielo1 = h("#9CCBFF"), Cielo2 = h("#EAF6FF") };
                default: return new Paleta { Top = h("#A57BCB"), Lado = h("#5E3C86"), Detalle = h("#8458B0"), Hierba = h("#7A6BC4"), Roca = h("#4E3A6E"), Cielo1 = h("#150D2B"), Cielo2 = h("#5A2C7A") };
            }
        }

        // ------------------------------------------------------------------ casillas isométricas
        // Lienzo de 128x88: la cara superior es un rombo de 128x64 con centro en (64,40) y 14 px de grosor.
        private const float CX = 64f, CY = 40f, HW = 64f, HH = 32f, GROSOR = 14f;

        private static void PuntoRombo(float u, float v, out float x, out float y)
        {
            x = CX + (u - v) * HW;
            y = CY + (u + v - 1f) * HH;
        }

        private static Imagen Cierra(Lienzo l, float pivYpx, int altoPx, float rad)
        {
            Imagen img = l.Resolver(rad, Dibujos.Cont);
            img.PivX = 0.5f;
            img.PivYAbajo = (altoPx - pivYpx) / altoPx;
            return img;
        }

        public static Imagen Casilla(int mundo, Terreno t, int variante)
        {
            Paleta p = PaletaMundo(mundo);
            Col top = p.Top.Mul(variante % 2 == 0 ? 1f : 0.93f);
            Col lado = p.Lado;
            if (t == Terreno.HierbaAlta) { top = p.Hierba.Mul(variante % 2 == 0 ? 1f : 0.93f); lado = p.Lado.Mezcla(p.Hierba, 0.3f); }
            else if (t == Terreno.Flores) { top = p.Hierba.Mul(1.12f); lado = p.Lado.Mezcla(p.Hierba, 0.3f); }
            else if (t == Terreno.Barro) { top = new Col(0.46f, 0.33f, 0.22f); lado = new Col(0.3f, 0.2f, 0.14f); }
            else if (t == Terreno.Agua) { top = new Col(0.31f, 0.64f, 0.85f).Mul(variante % 2 == 0 ? 1f : 0.94f); lado = new Col(0.2f, 0.42f, 0.62f); }

            var l = new Lienzo(128, 88);
            l.Poligono(lado, 0, CY, CX, CY + HH, CX, CY + HH + GROSOR, 0, CY + GROSOR);
            l.Poligono(lado.Mul(0.78f), CX, CY + HH, 128, CY, 128, CY + GROSOR, CX, CY + HH + GROSOR);
            l.Poligono(top, CX, CY - HH, 128, CY, CX, CY + HH, 0, CY);
            // brillo en los bordes de arriba
            l.Linea(4, CY - 1, CX, CY - HH + 2, 2f, top.Mul(1.18f).Alfa(0.8f));
            l.Linea(CX, CY - HH + 2, 124, CY - 1, 2f, top.Mul(1.1f).Alfa(0.7f));

            int sd = mundo * 100 + (int)t * 10 + variante;
            switch (t)
            {
                case Terreno.Suelo:
                case Terreno.Roca:
                case Terreno.Muro:
                case Terreno.Caja:
                case Terreno.RocaFragil:
                case Terreno.Barril:
                    DecorSuelo(l, p, mundo, sd);
                    break;
                case Terreno.Agua:
                    DecorAgua(l, sd);
                    break;
                case Terreno.HierbaAlta:
                    for (int i = 0; i < 16; i++)
                    {
                        float u = Ruido.Hash(sd, i, 1), v = Ruido.Hash(sd, i, 2), x, y;
                        PuntoRombo(u * 0.9f + 0.05f, v * 0.9f + 0.05f, out x, out y);
                        Col c = (i % 3 == 0) ? p.Hierba.Mul(0.78f) : (i % 3 == 1 ? p.Hierba.Mul(1.2f) : p.Hierba);
                        l.Poligono(c, x - 2.2f, y, x + 2.2f, y, x + (Ruido.Hash(sd, i, 3) - 0.5f) * 4, y - 6 - Ruido.Hash(sd, i, 4) * 4);
                    }
                    break;
                case Terreno.Flores:
                    for (int i = 0; i < 7; i++)
                    {
                        float u = Ruido.Hash(sd, i, 1), v = Ruido.Hash(sd, i, 2), x, y;
                        PuntoRombo(u * 0.8f + 0.1f, v * 0.8f + 0.1f, out x, out y);
                        Col pet = i % 3 == 0 ? Col.Hex("#FF8FB8") : (i % 3 == 1 ? Col.Hex("#FFE066") : Col.Hex("#FFFFFF"));
                        l.Linea(x, y + 3, x, y - 1, 1.6f, p.Hierba.Mul(0.7f));
                        for (int k = 0; k < 5; k++)
                        {
                            double ang = k * Math.PI * 2 / 5;
                            l.Circulo(x + (float)Math.Cos(ang) * 2.6f, y - 2 + (float)Math.Sin(ang) * 2.6f, 2.0f, pet);
                        }
                        l.Circulo(x, y - 2, 1.6f, Col.Hex("#E8A020"));
                    }
                    break;
                case Terreno.Barro:
                    for (int i = 0; i < 3; i++)
                    {
                        float u = Ruido.Hash(sd, i, 1), v = Ruido.Hash(sd, i, 2), x, y;
                        PuntoRombo(u * 0.6f + 0.2f, v * 0.6f + 0.2f, out x, out y);
                        l.Elipse(x, y, 13 + i * 2, 6 + i, new Col(0.3f, 0.2f, 0.13f));
                        l.Elipse(x - 2, y - 1.5f, 8 + i, 3.5f, new Col(0.36f, 0.25f, 0.17f));
                    }
                    for (int i = 0; i < 6; i++)
                    {
                        float x, y;
                        PuntoRombo(Ruido.Hash(sd, i, 7) * 0.8f + 0.1f, Ruido.Hash(sd, i, 8) * 0.8f + 0.1f, out x, out y);
                        l.Anillo(x, y, 2.4f, 1.1f, new Col(0.7f, 0.55f, 0.4f, 0.9f));
                    }
                    break;
            }
            return Cierra(l, CY, 88, 2f);
        }

        private static void DecorSuelo(Lienzo l, Paleta p, int mundo, int sd)
        {
            if (mundo == 1)
            {
                // tejas: líneas paralelas a los bordes
                for (int k = 1; k < 4; k++)
                {
                    float x0, y0, x1, y1;
                    PuntoRombo(k / 4f, 0.02f, out x0, out y0); PuntoRombo(k / 4f, 0.98f, out x1, out y1);
                    l.Linea(x0, y0, x1, y1, 1.4f, p.Lado.Alfa(0.45f));
                }
                for (int i = 0; i < 4; i++)
                {
                    float x, y;
                    PuntoRombo(Ruido.Hash(sd, i, 3) * 0.8f + 0.1f, Ruido.Hash(sd, i, 4) * 0.8f + 0.1f, out x, out y);
                    l.Elipse(x, y, 3, 1.5f, p.Detalle);
                }
                return;
            }
            int n = mundo == 6 ? 5 : 6;
            for (int i = 0; i < n; i++)
            {
                float u = Ruido.Hash(sd, i, 1), v = Ruido.Hash(sd, i, 2), x, y;
                PuntoRombo(u * 0.84f + 0.08f, v * 0.84f + 0.08f, out x, out y);
                if (mundo == 6)
                {
                    l.Linea(x - 3, y, x + 3, y, 1.2f, Col.Blanco.Alfa(0.9f));
                    l.Linea(x, y - 2, x, y + 2, 1.2f, Col.Blanco.Alfa(0.9f));
                }
                else if (mundo == 7)
                {
                    l.Circulo(x, y, 1.6f, Col.Hex("#E0C8FF"));
                    l.Linea(x - 3, y, x + 3, y, 0.9f, Col.Hex("#E0C8FF").Alfa(0.7f));
                }
                else
                {
                    l.Elipse(x, y, 2.6f + Ruido.Hash(sd, i, 5) * 2f, 1.4f + Ruido.Hash(sd, i, 6), p.Detalle);
                }
            }
        }

        /// <summary>Mata de hierba alta que se dibuja ENCIMA de la unidad (tapa un poco los pies).</summary>
        public static Imagen MataHierba(int mundo, int variante)
        {
            Paleta p = PaletaMundo(mundo);
            var l = new Lienzo(128, 80);
            float cy = 56f;
            for (int i = 0; i < 20; i++)
            {
                float u = Ruido.Hash(variante, i, 11) * 0.9f + 0.05f, v = Ruido.Hash(variante, i, 12) * 0.5f + 0.5f;   // mitad delantera
                float x = CX + (u - v) * HW, y = cy + (u + v - 1f) * HH;
                float alto = 12 + Ruido.Hash(variante, i, 13) * 10;
                Col c = (i % 3 == 0) ? p.Hierba.Mul(0.8f) : (i % 3 == 1 ? p.Hierba.Mul(1.25f) : p.Hierba);
                l.Poligono(c, x - 2.6f, y, x + 2.6f, y, x + (Ruido.Hash(variante, i, 14) - 0.5f) * 6, y - alto);
            }
            return Cierra(l, cy, 80, 1.6f);
        }

        /// <summary>Bloque de roca o de muro (obstáculo). Pivote en el centro de la base.</summary>
        public static Imagen Bloque(int mundo, bool muro, int variante)
        {
            Paleta p = PaletaMundo(mundo);
            var l = new Lienzo(128, 120);
            float cx = 64, cy = 84;
            float alto = muro ? 52 : 34;
            float hw = muro ? 56 : 42, hh = hw / 2f;
            Col c = muro ? p.Roca.Mezcla(p.Top, 0.25f) : p.Roca;
            // caras
            l.Poligono(c.Mul(0.72f), cx - hw, cy, cx, cy + hh, cx, cy + hh - alto, cx - hw, cy - alto);
            l.Poligono(c.Mul(0.55f), cx, cy + hh, cx + hw, cy, cx + hw, cy - alto, cx, cy + hh - alto);
            l.Poligono(c.Mul(1.1f), cx, cy - hh - alto, cx + hw, cy - alto, cx, cy + hh - alto, cx - hw, cy - alto);
            if (muro)
            {
                for (int k = 1; k < 4; k++)
                {
                    float yy = k * alto / 4f;
                    l.Linea(cx - hw, cy - alto + yy, cx, cy + hh - alto + yy, 1.6f, Col.Negro.Alfa(0.35f));
                    l.Linea(cx, cy + hh - alto + yy, cx + hw, cy - alto + yy, 1.6f, Col.Negro.Alfa(0.35f));
                }
                for (int k = 0; k < 4; k++)
                {
                    float f = (k + 0.5f) / 4f;
                    l.Linea(cx - hw * f, cy + hh * f - alto + 4, cx - hw * f, cy + hh * f - alto + alto / 4f, 1.4f, Col.Negro.Alfa(0.3f));
                    l.Linea(cx + hw * f, cy + hh * f - alto + alto / 2f, cx + hw * f, cy + hh * f - alto + alto * 0.75f, 1.4f, Col.Negro.Alfa(0.3f));
                }
            }
            else
            {
                l.Linea(cx - 18, cy - alto + 6, cx - 6, cy - alto + 20, 1.8f, Col.Negro.Alfa(0.35f));
                l.Linea(cx + 14, cy - alto + 12, cx + 24, cy - alto + 22, 1.8f, Col.Negro.Alfa(0.35f));
                l.Elipse(cx - 8, cy - alto - 2, 10, 4, p.Hierba.Alfa(0.9f));
                l.Elipse(cx + 12, cy - alto + 2, 6, 2.6f, p.Hierba.Mul(1.2f).Alfa(0.9f));
            }
            Imagen img = Cierra(l, cy, 120, 2f);
            return img;
        }

        // ------------------------------------------------------------------ gatos
        public static Col PeloDe(string clase)
        {
            switch (clase)
            {
                case "luchador": return Col.Hex("#EE9A45");
                case "mago": return Col.Hex("#B8A4EE");
                case "tanque": return Col.Hex("#93A0AE");
                case "cazador": return Col.Hex("#B58257");
                case "clerigo": return Col.Hex("#F7F2E6");
                case "picaro": return Col.Hex("#4A4E5C");
                case "druida": return Col.Hex("#A5D8A6");
                default: return Col.Hex("#F5A9BD");
            }
        }

        /// <summary>Gato de caricatura mirando a la derecha. Lienzo 96x96, pivote en los pies.</summary>
        public static Imagen Gato(string clase)
        {
            var l = new Lienzo(96, 96);
            DibujarGato(l, PeloDe(clase), clase);
            Imagen img = l.Resolver(2f, Cont);
            img.PivX = 0.5f; img.PivYAbajo = (96f - 88f) / 96f;
            return img;
        }

        private static void DibujarGato(Lienzo l, Col pelo, string clase)
        {
            Col claro = pelo.Mezcla(Col.Blanco, 0.5f), oscuro = pelo.Mul(0.68f), rosa = Col.Hex("#FF9FB5");
            // cola
            l.Bezier(24, 68, 2, 66, 10, 36, 7f, oscuro);
            l.Bezier(24, 68, 2, 66, 10, 36, 4.5f, pelo);
            l.Circulo(10.5f, 36.5f, 4.2f, claro);
            // patas
            l.RectRedondeado(27, 74, 11, 14, 4.5f, oscuro);
            l.RectRedondeado(39, 76, 11, 12, 4.5f, oscuro);
            l.RectRedondeado(52, 74, 11, 14, 4.5f, pelo);
            l.RectRedondeado(64, 76, 11, 12, 4.5f, pelo);
            // cuerpo
            l.Elipse(46, 64, 26, 18, pelo);
            l.Elipse(50, 72, 17, 8, claro);
            l.Linea(34, 49, 38, 58, 2.6f, oscuro.Alfa(0.8f));
            l.Linea(43, 47, 46, 57, 2.6f, oscuro.Alfa(0.8f));
            l.Linea(52, 47, 54, 56, 2.6f, oscuro.Alfa(0.8f));
            AccesorioAtras(l, clase);
            // orejas
            l.Poligono(pelo, 44, 36, 45, 8, 63, 28);
            l.Poligono(pelo, 66, 28, 82, 8, 84, 38);
            l.Poligono(rosa, 48, 31, 48, 16, 58, 28);
            l.Poligono(rosa, 70, 28, 78, 16, 80, 32);
            // cabeza
            l.Elipse(63, 46, 23, 19, pelo);
            l.Elipse(63, 55, 15, 9, claro);
            // ojos
            Col blanco = Col.Blanco;
            l.Elipse(54, 45, 6.6f, 8f, blanco); l.Elipse(72, 45, 6.6f, 8f, blanco);
            l.Elipse(56, 46, 3.6f, 5.4f, Col.Negro); l.Elipse(74, 46, 3.6f, 5.4f, Col.Negro);
            l.Elipse(57.2f, 43.2f, 1.4f, 1.8f, blanco); l.Elipse(75.2f, 43.2f, 1.4f, 1.8f, blanco);
            // nariz y boca
            l.Poligono(rosa, 60, 52, 67, 52, 63.5f, 56);
            l.Linea(63.5f, 56, 63.5f, 58.5f, 1.3f, Col.Negro);
            l.Curva(1.3f, Col.Negro, 63.5f, 58.5f, 60.5f, 60.5f, 57.5f, 59f);
            l.Curva(1.3f, Col.Negro, 63.5f, 58.5f, 66.5f, 60.5f, 69.5f, 59f);
            // bigotes
            l.Linea(51, 55, 39, 53, 1.1f, Col.Negro.Alfa(0.55f)); l.Linea(51, 58, 39, 60, 1.1f, Col.Negro.Alfa(0.55f));
            l.Linea(76, 55, 88, 53, 1.1f, Col.Negro.Alfa(0.55f)); l.Linea(76, 58, 88, 60, 1.1f, Col.Negro.Alfa(0.55f));
            // mejillas
            l.Elipse(47, 55, 3.4f, 2.2f, rosa.Alfa(0.55f)); l.Elipse(79, 55, 3.4f, 2.2f, rosa.Alfa(0.55f));
            AccesorioFrente(l, clase);
        }

        private static void AccesorioAtras(Lienzo l, string clase)
        {
            Col marron = Col.Hex("#7A4B2A");
            switch (clase)
            {
                case "luchador":   // espada a la espalda
                    l.Linea(20, 76, 36, 44, 4.5f, Col.Hex("#D5DCE6")); l.Linea(16, 80, 22, 70, 5f, marron);
                    break;
                case "mago":       // bastón
                    l.Linea(80, 90, 85, 40, 3.6f, marron); l.Circulo(85.5f, 36, 6, Col.Hex("#7FE0FF")); l.Circulo(87, 34, 2f, Col.Blanco);
                    break;
                case "cazador":    // arco
                    l.Bezier(18, 40, 2, 62, 18, 86, 3.4f, marron); l.Linea(18, 40, 18, 86, 1.1f, Col.Blanco.Alfa(0.9f));
                    break;
                case "druida":     // bastón con hoja
                    l.Linea(82, 90, 86, 40, 3.6f, marron); l.Elipse(86, 34, 6, 8, Col.Hex("#4FB56A")); l.Linea(86, 41, 86, 28, 1.2f, Col.Hex("#2E7D45"));
                    break;
            }
        }

        private static void AccesorioFrente(Lienzo l, string clase)
        {
            switch (clase)
            {
                case "luchador":
                    l.RectRedondeado(41, 34, 44, 8, 3.5f, Col.Hex("#E03A3A"));
                    l.Poligono(Col.Hex("#E03A3A"), 41, 36, 31, 31, 33, 41);
                    l.Linea(48, 38, 58, 38, 1.6f, Col.Hex("#FFB4B4"));
                    break;
                case "mago":
                    l.Elipse(63, 33, 27, 6.5f, Col.Hex("#5B3AA8"));
                    l.Poligono(Col.Hex("#7A55D6"), 46, 32, 80, 32, 68, 1);
                    l.Elipse(66, 28, 4, 4, Col.Hex("#FFE066"));
                    l.Poligono(Col.Hex("#FFE066"), 66, 19, 68, 25, 74, 26, 69, 30, 71, 36, 66, 32, 61, 36, 63, 30, 58, 26, 64, 25);
                    break;
                case "tanque":
                    l.Elipse(63, 32, 23, 13, Col.Hex("#A9B4C2"));
                    l.Rect(40, 31, 46, 5, Col.Hex("#7C8896"));
                    l.Rect(61, 32, 4, 14, Col.Hex("#7C8896"));
                    l.Elipse(36, 70, 12, 15, Col.Hex("#5C7CA8")); l.Elipse(36, 70, 8, 11, Col.Hex("#7FA0D0"));
                    l.Linea(36, 62, 36, 78, 2.6f, Col.Hex("#FFE066")); l.Linea(30, 70, 42, 70, 2.6f, Col.Hex("#FFE066"));
                    break;
                case "cazador":
                    l.Poligono(Col.Hex("#4E9B4E"), 42, 36, 84, 36, 74, 22, 56, 20);
                    l.Rect(41, 34, 44, 5, Col.Hex("#356E35"));
                    l.Poligono(Col.Hex("#E8503A"), 76, 28, 94, 10, 88, 32);
                    break;
                case "clerigo":
                    l.AnilloElipse(63, 14, 15, 5.5f, 3.2f, Col.Hex("#FFD84A"));
                    l.Poligono(Col.Hex("#FFFFFF"), 45, 62, 81, 62, 74, 76, 52, 76);
                    l.Linea(46, 62, 80, 62, 3f, Col.Hex("#FFD84A"));
                    l.Linea(63, 64, 63, 74, 2.4f, Col.Hex("#FFD84A")); l.Linea(58, 68, 68, 68, 2.4f, Col.Hex("#FFD84A"));
                    break;
                case "picaro":
                    l.RectRedondeado(41, 38, 44, 11, 5f, Col.Negro);
                    l.Elipse(54, 44, 4.5f, 2.4f, Col.Blanco); l.Elipse(72, 44, 4.5f, 2.4f, Col.Blanco);
                    l.Elipse(55, 44, 1.6f, 2f, Col.Negro); l.Elipse(73, 44, 1.6f, 2f, Col.Negro);
                    l.Poligono(Col.Hex("#C03A5A"), 82, 36, 94, 38, 90, 44, 82, 44);
                    l.Linea(80, 74, 92, 58, 3.6f, Col.Hex("#D8E0EA")); l.Linea(77, 78, 82, 70, 4.4f, Col.Hex("#5A3A2A"));
                    break;
                case "druida":
                    for (int i = 0; i < 5; i++)
                    {
                        float x = 46 + i * 9, y = 32 - (i == 2 ? 3 : (i == 1 || i == 3 ? 1 : -1));
                        l.Elipse(x, y, 6, 3.6f, i % 2 == 0 ? Col.Hex("#4FB56A") : Col.Hex("#7AD68A"));
                    }
                    l.Circulo(63, 28, 3.2f, Col.Hex("#FF8FB8"));
                    break;
                case "alquimista":
                    l.Linea(40, 37, 86, 37, 3f, Col.Hex("#7A4B2A"));
                    l.Anillo(55, 37, 7.5f, 2.6f, Col.Hex("#C98A3A")); l.Circulo(55, 37, 5, Col.Hex("#9FE8FF").Alfa(0.75f));
                    l.Anillo(73, 37, 7.5f, 2.6f, Col.Hex("#C98A3A")); l.Circulo(73, 37, 5, Col.Hex("#9FE8FF").Alfa(0.75f));
                    l.Rect(82, 64, 7, 8, Col.Hex("#D8F2FF")); l.Rect(81, 61, 9, 4, Col.Hex("#8A5A3A"));
                    l.Elipse(85.5f, 77, 9, 9, Col.Hex("#B07AF0")); l.Elipse(83, 74, 2.6f, 3.4f, Col.Blanco.Alfa(0.6f));
                    break;
            }
        }

        // ------------------------------------------------------------------ enemigos
        private static void OjosEnojados(Lienzo l, float x1, float x2, float y, float r, Col iris)
        {
            l.Elipse(x1, y, r, r * 1.15f, Col.Blanco); l.Elipse(x2, y, r, r * 1.15f, Col.Blanco);
            l.Circulo(x1 + r * 0.25f, y + 1, r * 0.55f, iris); l.Circulo(x2 + r * 0.25f, y + 1, r * 0.55f, iris);
            l.Circulo(x1 + r * 0.25f, y + 1, r * 0.28f, Col.Negro); l.Circulo(x2 + r * 0.25f, y + 1, r * 0.28f, Col.Negro);
            l.Linea(x1 - r - 1, y - r - 2, x1 + r, y - r + 2, 2.6f, Col.Negro);
            l.Linea(x2 + r + 1, y - r - 2, x2 - r, y - r + 2, 2.6f, Col.Negro);
        }

        private static void Colmillos(Lienzo l, float x, float y, float w)
        {
            l.Poligono(Col.Blanco, x - w, y, x - w + 3.2f, y, x - w + 1.6f, y + 5);
            l.Poligono(Col.Blanco, x + w - 3.2f, y, x + w, y, x + w - 1.6f, y + 5);
        }

        public static Imagen Enemigo(string id)
        {
            bool jefe = id.StartsWith("jefe");
            var l = new Lienzo(jefe ? 128 : 96, jefe ? 128 : 96);
            Col c = Col.Hex(GameData.Enemigo(id).ColorHex);
            float ph = jefe ? 120f : 88f;
            switch (id)
            {
                case "maton":
                    l.Elipse(48, 60, 27, 24, c.Mul(1.15f)); l.Elipse(48, 70, 18, 11, c.Mezcla(Col.Blanco, 0.4f));
                    l.Circulo(26, 36, 9, c.Mul(1.15f)); l.Circulo(70, 36, 9, c.Mul(1.15f)); l.Circulo(26, 36, 4.5f, Col.Hex("#FF9FB5")); l.Circulo(70, 36, 4.5f, Col.Hex("#FF9FB5"));
                    OjosEnojados(l, 38, 58, 50, 6.5f, Col.Hex("#FFB020"));
                    l.Elipse(48, 62, 6, 4, Col.Negro); Colmillos(l, 48, 67, 8);
                    l.Linea(78, 80, 88, 40, 6f, Col.Hex("#8A5A3A")); l.Circulo(88, 36, 8.5f, Col.Hex("#6E4426"));
                    break;
                case "arquero":
                    l.Elipse(48, 62, 22, 22, c.Mul(1.1f));
                    l.Poligono(c.Mul(1.1f), 26, 44, 4, 36, 28, 56); l.Poligono(c.Mul(1.1f), 70, 44, 92, 36, 68, 56);
                    l.Elipse(48, 48, 22, 18, c.Mul(1.2f));
                    OjosEnojados(l, 40, 56, 46, 6, Col.Hex("#FFE066"));
                    l.Elipse(48, 56, 4, 3, c.Mul(0.6f)); l.Curva(1.4f, Col.Negro, 41, 62, 48, 65, 55, 62);
                    l.Bezier(78, 40, 94, 62, 78, 88, 3.6f, Col.Hex("#7A4B2A")); l.Linea(78, 40, 78, 88, 1.1f, Col.Blanco.Alfa(0.9f));
                    break;
                case "bruto":
                    l.RectRedondeado(14, 34, 68, 54, 24, c.Mul(1.1f));
                    l.Elipse(48, 74, 20, 11, c.Mezcla(Col.Blanco, 0.35f));
                    l.Poligono(Col.Hex("#F0E6D2"), 22, 28, 12, 8, 34, 26); l.Poligono(Col.Hex("#F0E6D2"), 74, 28, 84, 8, 62, 26);
                    OjosEnojados(l, 36, 60, 46, 6.5f, Col.Hex("#FF4040"));
                    l.Elipse(48, 56, 7, 4.5f, c.Mul(0.55f));
                    l.Poligono(Col.Hex("#F0E6D2"), 32, 62, 36, 62, 33, 72); l.Poligono(Col.Hex("#F0E6D2"), 60, 62, 64, 62, 63, 72);
                    l.Circulo(10, 70, 9, c.Mul(1.2f)); l.Circulo(86, 70, 9, c.Mul(1.2f));
                    break;
                case "hechicero":
                    l.Poligono(c, 24, 88, 30, 44, 66, 44, 72, 88, 62, 82, 54, 90, 46, 82, 38, 90, 30, 82);
                    l.Elipse(48, 46, 19, 17, c.Mezcla(Col.Blanco, 0.3f));
                    l.Poligono(c.Mul(0.6f), 24, 40, 72, 40, 52, 4); l.Elipse(48, 40, 28, 6, c.Mul(0.5f));
                    l.Elipse(40, 48, 4.2f, 5.4f, Col.Hex("#FFE066")); l.Elipse(56, 48, 4.2f, 5.4f, Col.Hex("#FFE066"));
                    l.Linea(80, 88, 84, 34, 3.4f, Col.Hex("#6A4A2A")); l.Circulo(85, 30, 7, Col.Hex("#FF6AD5")); l.Circulo(86.5f, 28, 2.4f, Col.Blanco);
                    break;
                case "sanador":
                    l.Elipse(48, 66, 29, 22, c.Mul(1.1f)); l.Elipse(48, 52, 22, 20, c.Mul(1.15f));
                    l.Elipse(38, 46, 7, 5, Col.Blanco.Alfa(0.5f));
                    l.Elipse(40, 56, 5, 6, Col.Blanco); l.Elipse(57, 56, 5, 6, Col.Blanco); l.Circulo(41, 57, 2.8f, Col.Negro); l.Circulo(58, 57, 2.8f, Col.Negro);
                    l.Curva(1.4f, Col.Negro, 43, 68, 49, 71, 55, 68);
                    l.Rect(43, 74, 12, 3.4f, Col.Blanco); l.Rect(47.3f, 70, 3.4f, 11, Col.Blanco);
                    l.Rect(43, 74, 12, 3.4f, Col.Hex("#FF5A6A")); l.Rect(47.3f, 70, 3.4f, 11, Col.Hex("#FF5A6A"));
                    break;
                case "acosador":
                    l.Poligono(c.Mul(0.8f), 16, 62, 2, 38, 36, 54); l.Poligono(c.Mul(0.8f), 80, 62, 94, 38, 60, 54);
                    l.Elipse(48, 64, 20, 18, c); l.Elipse(48, 70, 12, 8, c.Mezcla(Col.Blanco, 0.45f));
                    l.Poligono(c, 28, 52, 24, 22, 42, 44); l.Poligono(c, 68, 52, 72, 22, 54, 44);
                    OjosEnojados(l, 41, 56, 58, 5, Col.Hex("#40FF80"));
                    Colmillos(l, 48, 70, 6);
                    l.Linea(66, 80, 84, 70, 3.2f, c.Mul(0.6f)); l.Poligono(c.Mul(0.6f), 84, 66, 92, 70, 84, 76);
                    break;
                case "lodo":
                    l.Poligono(c, 18, 88, 22, 56, 36, 40, 60, 40, 74, 56, 78, 88);
                    l.Elipse(48, 82, 32, 10, c.Mul(0.8f)); l.Elipse(48, 60, 28, 26, c);
                    l.Elipse(38, 50, 7, 4, Col.Blanco.Alfa(0.35f));
                    l.Elipse(40, 62, 5.2f, 6, Col.Blanco); l.Elipse(58, 62, 5.2f, 6, Col.Blanco); l.Circulo(41, 63, 2.8f, Col.Negro); l.Circulo(59, 63, 2.8f, Col.Negro);
                    l.Poligono(c.Mul(0.9f), 70, 80, 74, 80, 72, 92);
                    break;
                case "jefe1":
                    l.Escala = 1.3f; l.OX = 0f; l.OY = 12f;
                    DibujarGato(l, Col.Hex("#7A4B3A"), "");
                    l.Escala = 1f; l.OX = 0f; l.OY = 0f;
                    l.Linea(78, 52, 98, 80, 3f, Col.Hex("#E8D0C0")); l.Linea(84, 50, 102, 74, 2.4f, Col.Hex("#E8D0C0"));   // cicatriz
                    l.RectRedondeado(58, 101, 48, 7, 3.5f, Col.Hex("#C03A3A"));
                    l.Circulo(82, 111, 4.5f, Col.Hex("#FFD84A"));
                    break;
                case "jefe2":
                    l.Poligono(Col.Hex("#6B4423"), 34, 120, 44, 52, 84, 52, 94, 120);
                    l.Linea(48, 56, 20, 22, 8f, Col.Hex("#6B4423")); l.Linea(80, 56, 108, 22, 8f, Col.Hex("#6B4423")); l.Linea(64, 50, 64, 12, 9f, Col.Hex("#6B4423"));
                    foreach (float[] hoja in new[] { new float[] { 20, 20 }, new float[] { 108, 20 }, new float[] { 64, 12 }, new float[] { 34, 36 }, new float[] { 94, 36 } })
                        l.Circulo(hoja[0], hoja[1], 14, Col.Hex("#4FB56A"));
                    l.Elipse(52, 74, 7, 9, Col.Hex("#CFFFD8")); l.Elipse(76, 74, 7, 9, Col.Hex("#CFFFD8")); l.Circulo(53, 75, 3.4f, Col.Hex("#1E6B34")); l.Circulo(77, 75, 3.4f, Col.Hex("#1E6B34"));
                    l.Elipse(64, 94, 9, 6, Col.Hex("#3A2410"));
                    break;
                case "jefe3":
                    l.Poligono(c, 14, 120, 20, 70, 40, 36, 88, 36, 108, 70, 114, 120);
                    l.Elipse(64, 112, 52, 12, c.Mul(0.8f)); l.Elipse(64, 76, 46, 40, c);
                    l.Elipse(46, 58, 12, 6, Col.Blanco.Alfa(0.3f));
                    for (int i = 0; i < 5; i++) l.Anillo(32 + i * 16, 90 - (i % 2) * 18, 5, 2f, Col.Hex("#C8A878"));
                    OjosEnojados(l, 48, 82, 74, 9, Col.Hex("#FFC030"));
                    l.Poligono(c.Mul(0.9f), 96, 108, 104, 108, 100, 126);
                    break;
                case "jefe4":
                    l.Poligono(Col.Hex("#5A2E7A"), 4, 40, 40, 66, 44, 100, 20, 92, 24, 72, 10, 62);
                    l.Poligono(Col.Hex("#5A2E7A"), 124, 40, 88, 66, 84, 100, 108, 92, 104, 72, 118, 62);
                    l.Elipse(64, 80, 28, 34, c.Mul(1.1f)); l.Elipse(64, 90, 17, 21, c.Mezcla(Col.Blanco, 0.35f));
                    l.Poligono(c.Mul(1.1f), 42, 62, 38, 22, 58, 48); l.Poligono(c.Mul(1.1f), 86, 62, 90, 22, 70, 48);
                    l.Elipse(64, 62, 24, 20, c.Mul(1.2f));
                    OjosEnojados(l, 54, 74, 60, 6, Col.Hex("#FF2A2A"));
                    Colmillos(l, 64, 72, 8);
                    l.Poligono(Col.Hex("#FFD84A"), 46, 44, 82, 44, 88, 22, 76, 34, 64, 16, 52, 34, 40, 22);
                    break;
                case "jefe5":
                    l.RectRedondeado(30, 52, 68, 60, 12, Col.Hex("#8A8F98")); l.RectRedondeado(40, 14, 48, 42, 10, Col.Hex("#9AA0AA"));
                    l.RectRedondeado(6, 56, 26, 46, 10, Col.Hex("#7A808A")); l.RectRedondeado(96, 56, 26, 46, 10, Col.Hex("#7A808A"));
                    l.Linea(40, 70, 56, 84, 2.4f, Col.Negro.Alfa(0.4f)); l.Linea(80, 64, 70, 96, 2.4f, Col.Negro.Alfa(0.4f));
                    l.Rect(46, 28, 14, 8, Col.Hex("#5AF0FF")); l.Rect(68, 28, 14, 8, Col.Hex("#5AF0FF"));
                    l.Rect(54, 44, 20, 5, Col.Negro.Alfa(0.7f));
                    l.Elipse(64, 12, 10, 4, Col.Hex("#6A9A4A"));
                    break;
                case "jefe6":
                    l.Circulo(40, 66, 26, Col.Hex("#5A7EA8")); l.Circulo(88, 66, 26, Col.Hex("#5A7EA8")); l.Circulo(64, 46, 30, Col.Hex("#6C94C2")); l.Circulo(64, 78, 30, Col.Hex("#6C94C2"));
                    l.Poligono(Col.Hex("#FFE55A"), 70, 70, 54, 98, 66, 98, 56, 126, 84, 90, 72, 90, 82, 70);
                    OjosEnojados(l, 50, 78, 46, 7.5f, Col.Hex("#FFE55A"));
                    break;
                default:
                    if (DibujarEnemigoNuevo(id, l, c)) break;
                    l.Poligono(Col.Hex("#2A1A44"), 20, 120, 34, 60, 94, 60, 108, 120);
                    l.Elipse(64, 66, 32, 28, c.Mezcla(Col.Hex("#6A2A9A"), 0.4f));
                    l.Poligono(Col.Hex("#E8E0F0"), 38, 48, 22, 8, 54, 34); l.Poligono(Col.Hex("#E8E0F0"), 90, 48, 106, 8, 74, 34);
                    l.Poligono(Col.Hex("#FFD84A"), 44, 40, 84, 40, 88, 20, 76, 30, 64, 12, 52, 30, 40, 20);
                    l.Elipse(52, 66, 7, 9, Col.Hex("#FF3AD0")); l.Elipse(76, 66, 7, 9, Col.Hex("#FF3AD0")); l.Circulo(53, 67, 3, Col.Negro); l.Circulo(77, 67, 3, Col.Negro);
                    Colmillos(l, 64, 82, 10);
                    break;
            }
            Imagen img = l.Resolver(2.2f, Cont);
            img.PivX = 0.5f; img.PivYAbajo = ((jefe ? 128f : 96f) - ph) / (jefe ? 128f : 96f);
            return img;
        }

        // ------------------------------------------------------------------ partículas y formas sueltas
        public static Imagen Disco(int tam, bool suave)
        {
            var l = new Lienzo(tam, tam);
            float c = tam / 2f;
            if (suave)
                l.Funcion(0, 0, tam, tam, delegate (float x, float y)
                {
                    float d = (float)Math.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                    if (d >= 1f) return Col.Nada;
                    float a = (1f - d); a = a * a;
                    return new Col(1, 1, 1, a);
                });
            else l.Circulo(c, c, c - 1, Col.Blanco);
            Imagen im = l.Resolver(0, Cont); im.PivX = 0.5f; im.PivYAbajo = 0.5f; return im;
        }

        public static Imagen Estrella(int tam)
        {
            var l = new Lienzo(tam, tam);
            float c = tam / 2f, R = c - 1, r = R * 0.45f;
            var pts = new float[20];
            for (int i = 0; i < 10; i++)
            {
                double a = -Math.PI / 2 + i * Math.PI / 5;
                float rr = i % 2 == 0 ? R : r;
                pts[2 * i] = c + (float)Math.Cos(a) * rr; pts[2 * i + 1] = c + (float)Math.Sin(a) * rr;
            }
            l.Poligono(Col.Blanco, pts);
            Imagen im = l.Resolver(0, Cont); im.PivX = 0.5f; im.PivYAbajo = 0.5f; return im;
        }

        public static Imagen MasCruz(int tam)
        {
            var l = new Lienzo(tam, tam);
            float c = tam / 2f, g = tam * 0.3f;
            l.RectRedondeado(c - g / 2, 2, g, tam - 4, g / 2.2f, Col.Blanco);
            l.RectRedondeado(2, c - g / 2, tam - 4, g, g / 2.2f, Col.Blanco);
            Imagen im = l.Resolver(0, Cont); im.PivX = 0.5f; im.PivYAbajo = 0.5f; return im;
        }

        public static Imagen AnilloImg(int tam)
        {
            var l = new Lienzo(tam, tam);
            l.Anillo(tam / 2f, tam / 2f, tam / 2f - 2, tam * 0.07f, Col.Blanco);
            Imagen im = l.Resolver(0, Cont); im.PivX = 0.5f; im.PivYAbajo = 0.5f; return im;
        }

        public static Imagen Tajo(int tam)
        {
            var l = new Lienzo(tam, tam);
            l.Funcion(0, 0, tam, tam, delegate (float x, float y)
            {
                float c = tam / 2f;
                float d1 = (float)Math.Sqrt((x - c * 0.75f) * (x - c * 0.75f) + (y - c * 1.05f) * (y - c * 1.05f));
                float d2 = (float)Math.Sqrt((x - c * 0.95f) * (x - c * 0.95f) + (y - c * 1.15f) * (y - c * 1.15f));
                float R = tam * 0.46f;
                return d1 <= R && d2 > R * 0.92f && x > c * 0.2f ? Col.Blanco : Col.Nada;
            });
            Imagen im = l.Resolver(0, Cont); im.PivX = 0.5f; im.PivYAbajo = 0.5f; return im;
        }

        public static Imagen Chispa(int tam)
        {
            var l = new Lienzo(tam, tam);
            float c = tam / 2f;
            l.Poligono(Col.Blanco, c, 1, c + 3, c - 3, tam - 1, c, c + 3, c + 3, c, tam - 1, c - 3, c + 3, 1, c, c - 3, c - 3);
            Imagen im = l.Resolver(0, Cont); im.PivX = 0.5f; im.PivYAbajo = 0.5f; return im;
        }

        public static Imagen Llama(int tam)
        {
            var l = new Lienzo(tam, tam);
            float c = tam / 2f;
            l.Poligono(Col.Blanco, c, 1, c + tam * 0.3f, tam * 0.55f, c + tam * 0.22f, tam - 3, c, tam - 1, c - tam * 0.22f, tam - 3, c - tam * 0.3f, tam * 0.55f);
            Imagen im = l.Resolver(0, Cont); im.PivX = 0.5f; im.PivYAbajo = 0.5f; return im;
        }

        public static Imagen Hoja(int tam)
        {
            var l = new Lienzo(tam, tam);
            float c = tam / 2f;
            l.Elipse(c, c, tam * 0.2f, tam * 0.45f, Col.Blanco);
            Imagen im = l.Resolver(0, Cont); im.PivX = 0.5f; im.PivYAbajo = 0.5f; return im;
        }

        public static Imagen Burbuja(int tam)
        {
            var l = new Lienzo(tam, tam);
            float c = tam / 2f;
            l.Circulo(c, c, c - 1, Col.Blanco.Alfa(0.35f));
            l.Anillo(c, c, c - 1, tam * 0.08f, Col.Blanco);
            l.Circulo(c * 0.7f, c * 0.7f, tam * 0.1f, Col.Blanco);
            Imagen im = l.Resolver(0, Cont); im.PivX = 0.5f; im.PivYAbajo = 0.5f; return im;
        }

        /// <summary>Círculo mágico con símbolos (para los hechizos).</summary>
        public static Imagen Runa(int tam)
        {
            var l = new Lienzo(tam, tam);
            float c = tam / 2f;
            l.Anillo(c, c, c - 2, 2.6f, Col.Blanco); l.Anillo(c, c, c * 0.72f, 1.6f, Col.Blanco);
            for (int i = 0; i < 6; i++)
            {
                double a = i * Math.PI / 3;
                float x0 = c + (float)Math.Cos(a) * c * 0.72f, y0 = c + (float)Math.Sin(a) * c * 0.72f;
                double b = a + Math.PI * 2 / 3;
                l.Linea(x0, y0, c + (float)Math.Cos(b) * c * 0.72f, c + (float)Math.Sin(b) * c * 0.72f, 1.4f, Col.Blanco);
                l.Circulo(c + (float)Math.Cos(a) * (c - 5), c + (float)Math.Sin(a) * (c - 5), 2.2f, Col.Blanco);
            }
            Imagen im = l.Resolver(0, Cont); im.PivX = 0.5f; im.PivYAbajo = 0.5f; return im;
        }

        public static Imagen Rombo(int ancho, bool relleno)
        {
            int alto = ancho / 2;
            var l = new Lienzo(ancho, alto);
            if (relleno) l.Poligono(Col.Blanco, ancho / 2f, 0, ancho, alto / 2f, ancho / 2f, alto, 0, alto / 2f);
            else
            {
                float g = 2.6f;
                l.Poligono(Col.Blanco, ancho / 2f, 0, ancho, alto / 2f, ancho / 2f, alto, 0, alto / 2f);
                l.Poligono(Col.Nada, ancho / 2f, g, ancho - g * 2, alto / 2f, ancho / 2f, alto - g, g * 2, alto / 2f);
            }
            Imagen im = l.Resolver(0, Cont); im.PivX = 0.5f; im.PivYAbajo = 0.5f; return im;
        }

        public static Imagen Flecha()
        {
            var l = new Lienzo(64, 16);
            l.Linea(4, 8, 54, 8, 3f, Col.Hex("#C98A3A"));
            l.Poligono(Col.Hex("#E8EEF5"), 62, 8, 50, 2, 50, 14);
            l.Poligono(Col.Hex("#FFFFFF"), 2, 8, 10, 1, 14, 8, 10, 15);
            Imagen im = l.Resolver(1.4f, Cont); im.PivX = 0.5f; im.PivYAbajo = 0.5f; return im;
        }

        public static Imagen Rayo()
        {
            var l = new Lienzo(40, 112);
            l.Poligono(Col.Hex("#FFF27A"), 24, 0, 6, 58, 18, 58, 10, 112, 36, 40, 22, 40, 34, 0);
            Imagen im = l.Resolver(2f, Cont); im.PivX = 0.5f; im.PivYAbajo = 0f; return im;
        }

        /// <summary>Cuadrado blanco con pivote a la izquierda (para barras).</summary>
        public static Imagen BarraBlanca()
        {
            var l = new Lienzo(16, 8);
            l.RectRedondeado(0, 0, 16, 8, 3.5f, Col.Blanco);
            Imagen im = l.Resolver(0, Cont); im.PivX = 0f; im.PivYAbajo = 0.5f; return im;
        }

        public static Imagen Cuadrado()
        {
            var l = new Lienzo(8, 8);
            l.Rect(0, 0, 8, 8, Col.Blanco);
            Imagen im = l.Resolver(0, Cont); im.PivX = 0.5f; im.PivYAbajo = 0.5f; return im;
        }

        /// <summary>Contorno de rombo (solo borde), para marcar casillas.</summary>
        public static Imagen RomboBorde(int ancho)
        {
            int alto = ancho / 2;
            var l = new Lienzo(ancho, alto);
            float g = 4f;
            l.Linea(ancho / 2f, 2, ancho - 3, alto / 2f, g, Col.Blanco); l.Linea(ancho - 3, alto / 2f, ancho / 2f, alto - 2, g, Col.Blanco);
            l.Linea(ancho / 2f, alto - 2, 3, alto / 2f, g, Col.Blanco); l.Linea(3, alto / 2f, ancho / 2f, 2, g, Col.Blanco);
            Imagen im = l.Resolver(0, Cont); im.PivX = 0.5f; im.PivYAbajo = 0.5f; return im;
        }

        public static Imagen Sombra()
        {
            var l = new Lienzo(64, 32);
            l.Funcion(0, 0, 64, 32, delegate (float x, float y)
            {
                float dx = (x - 32) / 32f, dy = (y - 16) / 16f, d = (float)Math.Sqrt(dx * dx + dy * dy);
                return d >= 1f ? Col.Nada : new Col(0.05f, 0.03f, 0.08f, 0.55f * (1f - d * d));
            });
            Imagen im = l.Resolver(0, Cont); im.PivX = 0.5f; im.PivYAbajo = 0.5f; return im;
        }

        public static Imagen Nube(int w, int variante)
        {
            int h = w / 2;
            var l = new Lienzo(w, h);
            Col c = Col.Blanco;
            l.RectRedondeado(w * 0.08f, h * 0.55f, w * 0.84f, h * 0.35f, h * 0.17f, c);
            for (int i = 0; i < 4; i++)
                l.Circulo(w * (0.25f + 0.17f * i), h * (0.55f - (i == 1 || i == 2 ? 0.18f : 0.03f) - Ruido.Hash(variante, i, 1) * 0.1f), h * (0.22f + Ruido.Hash(variante, i, 2) * 0.1f), c);
            Imagen im = l.Resolver(0, Cont); im.PivX = 0.5f; im.PivYAbajo = 0.5f; return im;
        }

        /// <summary>Viñeta: bordes oscuros suaves para dar atmósfera.</summary>
        public static Imagen Vineta()
        {
            var l = new Lienzo(64, 64);
            l.Funcion(0, 0, 64, 64, delegate (float x, float y)
            {
                float dx = (x - 32) / 32f, dy = (y - 32) / 32f;
                float d = (float)Math.Sqrt(dx * dx * 0.8f + dy * dy);
                float a = Math.Max(0f, (d - 0.55f) / 0.75f);
                return new Col(0.04f, 0.02f, 0.08f, Math.Min(0.75f, a * a));
            });
            Imagen im = l.Resolver(0, Cont); im.PivX = 0.5f; im.PivYAbajo = 0.5f; return im;
        }

        // ------------------------------------------------------------------ iconos para la interfaz
        public static Imagen Icono(string id)
        {
            var l = new Lienzo(64, 64);
            switch (id)
            {
                case "corazon":
                    l.Circulo(21, 24, 14, Col.Hex("#FF4D6A")); l.Circulo(43, 24, 14, Col.Hex("#FF4D6A"));
                    l.Poligono(Col.Hex("#FF4D6A"), 8, 30, 56, 30, 32, 58);
                    l.Elipse(18, 19, 5, 3.4f, Col.Blanco.Alfa(0.75f));
                    break;
                case "corazon_vacio":
                    l.Circulo(21, 24, 14, Col.Hex("#5A4650")); l.Circulo(43, 24, 14, Col.Hex("#5A4650"));
                    l.Poligono(Col.Hex("#5A4650"), 8, 30, 56, 30, 32, 58);
                    break;
                case "gota":
                    l.Poligono(Col.Hex("#4FB0FF"), 32, 4, 52, 36, 12, 36);
                    l.Circulo(32, 40, 19, Col.Hex("#4FB0FF"));
                    l.Elipse(25, 38, 4.5f, 7, Col.Blanco.Alfa(0.75f));
                    break;
                case "gota_vacia":
                    l.Poligono(Col.Hex("#3A4660"), 32, 4, 52, 36, 12, 36);
                    l.Circulo(32, 40, 19, Col.Hex("#3A4660"));
                    break;
                case "bota":
                    l.RectRedondeado(14, 6, 22, 38, 8, Col.Hex("#C98A3A"));
                    l.RectRedondeado(12, 36, 42, 18, 9, Col.Hex("#C98A3A"));
                    l.Rect(14, 14, 22, 5, Col.Hex("#F0D090"));
                    l.Rect(12, 50, 42, 4, Col.Hex("#7A4B2A"));
                    break;
                case "escudo":
                    l.Poligono(Col.Hex("#7FA0D0"), 8, 8, 56, 8, 56, 34, 32, 60, 8, 34);
                    l.Poligono(Col.Hex("#A8C4EC"), 32, 12, 52, 12, 52, 33, 32, 54);
                    l.Linea(32, 16, 32, 46, 4, Col.Hex("#FFE066")); l.Linea(20, 28, 44, 28, 4, Col.Hex("#FFE066"));
                    break;
                case "espada":
                    l.Linea(14, 50, 46, 14, 8, Col.Hex("#DDE6F2")); l.Linea(12, 52, 22, 42, 9, Col.Hex("#7A4B2A")); l.Linea(18, 38, 30, 50, 7, Col.Hex("#E8B030"));
                    break;
                case "estrella":
                    l.Poligono(Col.Hex("#FFD84A"), 32, 4, 40, 24, 60, 24, 44, 38, 50, 58, 32, 46, 14, 58, 20, 38, 4, 24, 24, 24);
                    break;
                case "pocionVida": Pocion(l, Col.Hex("#FF4D6A")); break;
                case "pocionMana": Pocion(l, Col.Hex("#4FB0FF")); break;
                case "pocionVel": Pocion(l, Col.Hex("#FFD84A")); break;
                case "antidoto": Pocion(l, Col.Hex("#5AD67A")); break;
                case "bomba":
                    l.Linea(40, 16, 48, 6, 3, Col.Hex("#C98A3A")); l.Circulo(50, 6, 4, Col.Hex("#FFB020"));
                    l.Circulo(30, 38, 21, Col.Hex("#3C3F4A")); l.Elipse(23, 30, 6, 4, Col.Blanco.Alfa(0.5f)); l.Rect(24, 12, 14, 8, Col.Hex("#5A5E6C"));
                    break;
                case "calavera":
                    l.Elipse(32, 28, 21, 19, Col.Hex("#F0E8DC")); l.RectRedondeado(20, 40, 24, 16, 5, Col.Hex("#F0E8DC"));
                    l.Elipse(23, 30, 6, 7, Col.Negro); l.Elipse(41, 30, 6, 7, Col.Negro); l.Poligono(Col.Negro, 32, 38, 28, 46, 36, 46);
                    break;
                default:
                    if (!IconoNuevo(id, l)) l.Circulo(32, 32, 20, Col.Blanco);
                    break;
            }
            Imagen im = l.Resolver(2f, Cont); im.PivX = 0.5f; im.PivYAbajo = 0.5f; return im;
        }

        private static void Pocion(Lienzo l, Col liquido)
        {
            l.Rect(25, 4, 14, 8, Col.Hex("#8A5A3A"));
            l.Rect(26, 10, 12, 16, Col.Hex("#D8F2FF"));
            l.Poligono(Col.Hex("#D8F2FF"), 26, 24, 38, 24, 54, 46, 54, 52, 10, 52, 10, 46);
            l.Circulo(32, 48, 20, Col.Hex("#D8F2FF"));
            l.Elipse(32, 50, 17, 14, liquido);
            l.Elipse(25, 46, 4, 6, Col.Blanco.Alfa(0.6f));
        }

        /// <summary>Icono redondo de la habilidad de cada clase.</summary>
        public static Imagen IconoHabilidad(string clase)
        {
            var l = new Lienzo(64, 64);
            Col fondo = Col.Hex(GameData.Clase(clase).ColorHex);
            l.Circulo(32, 32, 29, fondo.Mul(0.75f)); l.Circulo(32, 30, 26, fondo.Mezcla(Col.Blanco, 0.2f));
            Col b = Col.Blanco;
            switch (clase)
            {
                case "luchador":
                    for (int i = 0; i < 3; i++) l.Linea(20 + i * 9, 14, 30 + i * 9, 50, 4.6f, b);
                    break;
                case "mago":
                    l.Poligono(Col.Hex("#FFB020"), 32, 8, 46, 34, 42, 50, 32, 56, 22, 50, 18, 34);
                    l.Poligono(Col.Hex("#FFE066"), 32, 24, 40, 40, 32, 52, 24, 40);
                    break;
                case "tanque":
                    l.Poligono(b, 16, 14, 48, 14, 48, 34, 32, 54, 16, 34);
                    l.Linea(32, 20, 32, 38, 4.4f, fondo.Mul(0.7f)); l.Circulo(32, 45, 2.8f, fondo.Mul(0.7f));
                    break;
                case "cazador":
                    l.Linea(14, 50, 50, 14, 3.4f, b); l.Poligono(b, 54, 10, 42, 14, 50, 22);
                    l.Poligono(b, 12, 52, 20, 44, 14, 40);
                    break;
                case "clerigo":
                    l.RectRedondeado(27, 12, 10, 40, 4, b); l.RectRedondeado(15, 23, 34, 10, 4, b);
                    break;
                case "picaro":
                    l.Linea(16, 48, 46, 16, 5.4f, b); l.Linea(14, 50, 22, 42, 7, Col.Hex("#7A4B2A"));
                    break;
                case "druida":
                    l.Elipse(32, 32, 12, 22, Col.Hex("#7AD68A")); l.Linea(32, 54, 32, 12, 2.6f, Col.Hex("#2E7D45"));
                    break;
                default:
                    l.Rect(27, 8, 10, 12, b); l.Poligono(b, 27, 18, 37, 18, 50, 48, 14, 48);
                    l.Elipse(32, 46, 17, 10, Col.Hex("#B07AF0"));
                    break;
            }
            Imagen im = l.Resolver(2f, Cont); im.PivX = 0.5f; im.PivYAbajo = 0.5f; return im;
        }

        // ------------------------------------------------------------------ texturas de la interfaz (nueve cortes)
        /// <summary>Panel redondeado con borde grueso y sombra. Para usar con borde 24 px.</summary>
        public static Imagen Panel(Col fondo, Col borde, bool brillo)
        {
            var l = new Lienzo(64, 64);
            l.RectRedondeado(1, 4, 62, 59, 18, Col.Negro.Alfa(0.28f));
            l.RectRedondeado(1, 1, 62, 58, 18, borde);
            l.RectRedondeado(5, 5, 54, 50, 14, fondo);
            if (brillo) l.RectRedondeado(7, 7, 50, 16, 9, Col.Blanco.Alfa(0.16f));
            Imagen im = l.Resolver(0, Cont); im.PivX = 0.5f; im.PivYAbajo = 0.5f; return im;
        }

        /// <summary>Botón gordito: borde, cara y labio inferior más oscuro.</summary>
        public static Imagen Boton(Col cara, bool presionado)
        {
            var l = new Lienzo(64, 64);
            float dy = presionado ? 3f : 0f;
            l.RectRedondeado(1, 7, 62, 56, 18, Col.Negro.Alfa(0.3f));
            l.RectRedondeado(1, 1 + dy, 62, 56 - dy, 18, Cont);
            l.RectRedondeado(4, 4 + dy, 56, 51 - dy, 15, cara.Mul(0.72f));
            l.RectRedondeado(4, 4 + dy, 56, 44 - dy, 15, cara);
            l.RectRedondeado(8, 8 + dy, 48, 14, 7, Col.Blanco.Alfa(0.28f));
            Imagen im = l.Resolver(0, Cont); im.PivX = 0.5f; im.PivYAbajo = 0.5f; return im;
        }

        public static Imagen Degradado(Col arriba, Col abajo)
        {
            var l = new Lienzo(4, 64);
            l.Funcion(0, 0, 4, 64, delegate (float x, float y) { return arriba.Mezcla(abajo, y / 64f); });
            Imagen im = l.Resolver(0, Cont); im.PivX = 0.5f; im.PivYAbajo = 0.5f; return im;
        }
    }
}
