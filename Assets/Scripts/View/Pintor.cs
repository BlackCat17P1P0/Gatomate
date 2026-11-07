// Gatomate - vista. Dibujo procedural (sin imágenes): un pequeño "lienzo" que rellena formas con
// bordes suavizados y contorno grueso al estilo caricatura. No depende de Unity (solo C# puro),
// así se puede probar y previsualizar fuera de Unity.
using System;
using System.Collections.Generic;

namespace Gatomate.View
{
    public struct Col
    {
        public float R, G, B, A;
        public Col(float r, float g, float b, float a) { R = r; G = g; B = b; A = a; }
        public Col(float r, float g, float b) { R = r; G = g; B = b; A = 1f; }

        public static Col Hex(string h) { return Hex(h, 1f); }
        public static Col Hex(string h, float a)
        {
            if (string.IsNullOrEmpty(h) || h.Length < 7) return new Col(0.5f, 0.5f, 0.5f, a);
            return new Col(Convert.ToInt32(h.Substring(1, 2), 16) / 255f, Convert.ToInt32(h.Substring(3, 2), 16) / 255f, Convert.ToInt32(h.Substring(5, 2), 16) / 255f, a);
        }

        public Col Mul(float k) { return new Col(Math.Min(1, R * k), Math.Min(1, G * k), Math.Min(1, B * k), A); }
        public Col Mezcla(Col o, float t) { return new Col(R + (o.R - R) * t, G + (o.G - G) * t, B + (o.B - B) * t, A + (o.A - A) * t); }
        public Col Alfa(float a) { return new Col(R, G, B, a); }

        public static readonly Col Blanco = new Col(1, 1, 1, 1);
        public static readonly Col Negro = new Col(0.12f, 0.08f, 0.1f, 1);
        public static readonly Col Contorno = new Col(0.16f, 0.1f, 0.12f, 1);
        public static readonly Col Nada = new Col(0, 0, 0, 0);
    }

    /// <summary>Imagen ya resuelta: RGBA premultiplicado, origen arriba a la izquierda.</summary>
    public sealed class Imagen
    {
        public int W, H;
        public float[] P;
        public float PivX = 0.5f;          // pivote (0..1) desde la izquierda
        public float PivYAbajo = 0.5f;     // pivote (0..1) desde abajo (como en Unity)

        /// <summary>RGBA 8 bits sin premultiplicar, fila 0 = arriba.</summary>
        public byte[] Bytes()
        {
            var b = new byte[W * H * 4];
            for (int i = 0; i < W * H; i++)
            {
                float a = P[i * 4 + 3];
                float r = a > 0.0001f ? P[i * 4] / a : 0, g = a > 0.0001f ? P[i * 4 + 1] / a : 0, bl = a > 0.0001f ? P[i * 4 + 2] / a : 0;
                b[i * 4] = (byte)Math.Max(0, Math.Min(255, r * 255f + 0.5f));
                b[i * 4 + 1] = (byte)Math.Max(0, Math.Min(255, g * 255f + 0.5f));
                b[i * 4 + 2] = (byte)Math.Max(0, Math.Min(255, bl * 255f + 0.5f));
                b[i * 4 + 3] = (byte)Math.Max(0, Math.Min(255, a * 255f + 0.5f));
            }
            return b;
        }
    }

    /// <summary>Lienzo con 3x3 muestras por píxel (bordes suaves). Las coordenadas son en píxeles, y = hacia abajo.</summary>
    public sealed class Lienzo
    {
        private const int SS = 3;
        public readonly int W, H;
        private readonly int _w, _h;
        private readonly float[] _b;
        // transformación global: x' = x*Escala + OX, y' = y*Escala + OY
        public float Escala = 1f, OX = 0f, OY = 0f;

        public Lienzo(int w, int h)
        {
            W = w; H = h; _w = w * SS; _h = h * SS;
            _b = new float[_w * _h * 4];
        }

        private float TX(float x) { return x * Escala + OX; }
        private float TY(float y) { return y * Escala + OY; }

        private void Poner(int ix, int iy, Col c)
        {
            int i = (iy * _w + ix) * 4;
            float a = c.A, ia = 1f - a;
            _b[i] = c.R * a + _b[i] * ia;
            _b[i + 1] = c.G * a + _b[i + 1] * ia;
            _b[i + 2] = c.B * a + _b[i + 2] * ia;
            _b[i + 3] = a + _b[i + 3] * ia;
        }

        private void Rellenar(float x0, float y0, float x1, float y1, Func<float, float, bool> dentro, Col c)
        {
            int ix0 = Math.Max(0, (int)Math.Floor(x0 * SS)), ix1 = Math.Min(_w - 1, (int)Math.Ceiling(x1 * SS));
            int iy0 = Math.Max(0, (int)Math.Floor(y0 * SS)), iy1 = Math.Min(_h - 1, (int)Math.Ceiling(y1 * SS));
            for (int iy = iy0; iy <= iy1; iy++)
            {
                float py = (iy + 0.5f) / SS;
                for (int ix = ix0; ix <= ix1; ix++)
                {
                    float px = (ix + 0.5f) / SS;
                    if (dentro(px, py)) Poner(ix, iy, c);
                }
            }
        }

        /// <summary>Pinta con una función que devuelve el color de cada punto (A=0 = no pintar).</summary>
        public void Funcion(float x0, float y0, float x1, float y1, Func<float, float, Col> f)
        {
            x0 = TX(x0); y0 = TY(y0); x1 = TX(x1); y1 = TY(y1);
            int ix0 = Math.Max(0, (int)Math.Floor(x0 * SS)), ix1 = Math.Min(_w - 1, (int)Math.Ceiling(x1 * SS));
            int iy0 = Math.Max(0, (int)Math.Floor(y0 * SS)), iy1 = Math.Min(_h - 1, (int)Math.Ceiling(y1 * SS));
            for (int iy = iy0; iy <= iy1; iy++)
                for (int ix = ix0; ix <= ix1; ix++)
                {
                    Col c = f((ix + 0.5f) / SS, (iy + 0.5f) / SS);
                    if (c.A > 0.001f) Poner(ix, iy, c);
                }
        }

        public void Elipse(float cx, float cy, float rx, float ry, Col c)
        {
            cx = TX(cx); cy = TY(cy); rx *= Escala; ry *= Escala;
            Rellenar(cx - rx, cy - ry, cx + rx, cy + ry, delegate (float x, float y)
            {
                float dx = (x - cx) / rx, dy = (y - cy) / ry;
                return dx * dx + dy * dy <= 1f;
            }, c);
        }

        public void Circulo(float cx, float cy, float r, Col c) { Elipse(cx, cy, r, r, c); }

        /// <summary>Elipse hueca (anillo) con grosor t.</summary>
        public void AnilloElipse(float cx, float cy, float rx, float ry, float t, Col c)
        {
            cx = TX(cx); cy = TY(cy); rx *= Escala; ry *= Escala; t *= Escala;
            float irx = Math.Max(0.1f, rx - t), iry = Math.Max(0.1f, ry - t * ry / Math.Max(rx, 0.1f));
            Rellenar(cx - rx, cy - ry, cx + rx, cy + ry, delegate (float x, float y)
            {
                float dx = (x - cx) / rx, dy = (y - cy) / ry;
                float ex = (x - cx) / irx, ey = (y - cy) / iry;
                return dx * dx + dy * dy <= 1f && ex * ex + ey * ey >= 1f;
            }, c);
        }

        public void Anillo(float cx, float cy, float r, float t, Col c) { AnilloElipse(cx, cy, r, r, t, c); }

        public void Rect(float x, float y, float w, float h, Col c) { RectRedondeado(x, y, w, h, 0f, c); }

        public void RectRedondeado(float x, float y, float w, float h, float r, Col c)
        {
            x = TX(x); y = TY(y); w *= Escala; h *= Escala; r *= Escala;
            float cx = x + w / 2, cy = y + h / 2;
            Rellenar(x, y, x + w, y + h, delegate (float px, float py)
            {
                float dx = Math.Max(Math.Abs(px - cx) - (w / 2 - r), 0f), dy = Math.Max(Math.Abs(py - cy) - (h / 2 - r), 0f);
                return dx * dx + dy * dy <= r * r + 0.0001f;
            }, c);
        }

        /// <summary>Polígono relleno; puntos como x0,y0,x1,y1,...</summary>
        public void Poligono(Col c, params float[] pts)
        {
            int n = pts.Length / 2;
            var xs = new float[n]; var ys = new float[n];
            float minx = float.MaxValue, miny = float.MaxValue, maxx = float.MinValue, maxy = float.MinValue;
            for (int i = 0; i < n; i++)
            {
                xs[i] = TX(pts[2 * i]); ys[i] = TY(pts[2 * i + 1]);
                minx = Math.Min(minx, xs[i]); maxx = Math.Max(maxx, xs[i]); miny = Math.Min(miny, ys[i]); maxy = Math.Max(maxy, ys[i]);
            }
            Rellenar(minx, miny, maxx, maxy, delegate (float px, float py)
            {
                bool dentro = false;
                for (int i = 0, j = n - 1; i < n; j = i++)
                {
                    if ((ys[i] > py) != (ys[j] > py) && px < (xs[j] - xs[i]) * (py - ys[i]) / (ys[j] - ys[i]) + xs[i]) dentro = !dentro;
                }
                return dentro;
            }, c);
        }

        /// <summary>Segmento grueso con extremos redondeados.</summary>
        public void Linea(float x0, float y0, float x1, float y1, float grosor, Col c)
        {
            x0 = TX(x0); y0 = TY(y0); x1 = TX(x1); y1 = TY(y1);
            float r = grosor * Escala / 2f;
            float dx = x1 - x0, dy = y1 - y0, l2 = dx * dx + dy * dy;
            Rellenar(Math.Min(x0, x1) - r, Math.Min(y0, y1) - r, Math.Max(x0, x1) + r, Math.Max(y0, y1) + r, delegate (float px, float py)
            {
                float t = l2 < 0.0001f ? 0f : ((px - x0) * dx + (py - y0) * dy) / l2;
                t = Math.Max(0f, Math.Min(1f, t));
                float qx = x0 + t * dx - px, qy = y0 + t * dy - py;
                return qx * qx + qy * qy <= r * r;
            }, c);
        }

        /// <summary>Línea quebrada gruesa (curva aproximada): x0,y0,x1,y1,...</summary>
        public void Curva(float grosor, Col c, params float[] pts)
        {
            for (int i = 0; i + 3 < pts.Length; i += 2) Linea(pts[i], pts[i + 1], pts[i + 2], pts[i + 3], grosor, c);
        }

        /// <summary>Curva suave (Bézier cuadrática) con N tramos.</summary>
        public void Bezier(float x0, float y0, float cx, float cy, float x1, float y1, float grosor, Col c)
        {
            float px = x0, py = y0;
            for (int i = 1; i <= 14; i++)
            {
                float t = i / 14f, u = 1 - t;
                float x = u * u * x0 + 2 * u * t * cx + t * t * x1, y = u * u * y0 + 2 * u * t * cy + t * t * y1;
                Linea(px, py, x, y, grosor, c);
                px = x; py = y;
            }
        }

        /// <summary>Convierte a imagen final (promedia las muestras) y agrega un contorno de 'radio' píxeles.</summary>
        public Imagen Resolver(float radio, Col contorno)
        {
            var img = new Imagen { W = W, H = H, P = new float[W * H * 4] };
            float k = 1f / (SS * SS);
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float r = 0, g = 0, b = 0, a = 0;
                    for (int sy = 0; sy < SS; sy++)
                        for (int sx = 0; sx < SS; sx++)
                        {
                            int i = ((y * SS + sy) * _w + (x * SS + sx)) * 4;
                            r += _b[i]; g += _b[i + 1]; b += _b[i + 2]; a += _b[i + 3];
                        }
                    int o = (y * W + x) * 4;
                    img.P[o] = r * k; img.P[o + 1] = g * k; img.P[o + 2] = b * k; img.P[o + 3] = a * k;
                }
            if (radio > 0f)
            {
                var src = (float[])img.P.Clone();
                int R = (int)Math.Ceiling(radio);
                for (int y = 0; y < H; y++)
                    for (int x = 0; x < W; x++)
                    {
                        int o = (y * W + x) * 4;
                        float a = src[o + 3];
                        if (a >= 0.995f) continue;
                        float m = 0f;
                        for (int dy = -R; dy <= R; dy++)
                            for (int dx = -R; dx <= R; dx++)
                            {
                                int nx = x + dx, ny = y + dy;
                                if (nx < 0 || ny < 0 || nx >= W || ny >= H) continue;
                                float d = (float)Math.Sqrt(dx * dx + dy * dy);
                                if (d > radio + 0.5f) continue;
                                float v = src[(ny * W + nx) * 4 + 3] * Math.Min(1f, radio + 0.5f - d);
                                if (v > m) m = v;
                            }
                        float oa = m * (1f - a) * contorno.A;
                        if (oa <= 0f) continue;
                        img.P[o] += contorno.R * oa; img.P[o + 1] += contorno.G * oa; img.P[o + 2] += contorno.B * oa; img.P[o + 3] += oa;
                    }
            }
            return img;
        }
    }

    /// <summary>Números "al azar" repetibles para decorar (misma casilla = mismo dibujo).</summary>
    public static class Ruido
    {
        public static float Hash(int a, int b, int c)
        {
            unchecked
            {
                uint h = (uint)(a * 374761393 + b * 668265263 + c * 1274126177);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / 16777216f;
            }
        }
    }
}
