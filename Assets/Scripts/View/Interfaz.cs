// Gatomate - vista. Herramientas para dibujar la interfaz (IMGUI) con estilo de dibujos animados: paneles de
// nueve cortes con borde grueso, botones gordos con rebote, retratos, barras y textos con contorno.
// Todas las medidas se dan en un lienzo virtual de 720 de alto (el ancho se adapta a la pantalla).
// No se usa la clase Input (solo eventos de IMGUI), así funciona con el sistema de entrada viejo o el nuevo.
using System;
using System.Collections.Generic;
using UnityEngine;
using Gatomate.Core;

namespace Gatomate.View
{
    public sealed class Interfaz
    {
        public const float VH = 720f;
        public float K = 1f, VW = 1280f;
        public Vector2 Raton;                                          // ratón en coordenadas virtuales
        public readonly List<Rect> Bloqueos = new List<Rect>();         // zonas de la interfaz (no pasan clics al tablero)
        public string Tip = "";                                        // texto de ayuda de lo que está bajo el ratón
        public HabilidadDef TipH;                                      // si no es null, el globo muestra esta habilidad con sus claves (versión corta)
        public string TipClase = "luchador";
        public string TipPista = "Clic: descripción completa";
        public bool Congelado;                                         // true mientras hay una ventana encima: los controles de abajo no responden
        private GUIStyle _lbl, _campo;
        private int _id, _abajo = -1, _sobreAntes = -1, _sobreAhora = -1;
        private float _t;

        public static readonly Color Tinta = new Color(0.24f, 0.17f, 0.31f);          // texto sobre crema
        public static readonly Color Crema = new Color(0.96f, 0.91f, 0.76f);
        public static readonly Color Oro = new Color(1f, 0.85f, 0.35f);
        public static readonly Color Sombra = new Color(0.1f, 0.05f, 0.15f, 0.75f);
        public static readonly Color Rojo = new Color(1f, 0.4f, 0.4f);
        public static readonly Color Verde = new Color(0.5f, 0.95f, 0.55f);

        public static Color Hex(string h) { Col c = Col.Hex(h); return new Color(c.R, c.G, c.B, 1f); }

        // ================================================================ ciclo
        public void Empezar()
        {
            K = Screen.height / VH;
            VW = Screen.width / K;
            Event e = Event.current;
            Raton = e.mousePosition / K;
            Bloqueos.Clear();
            Tip = ""; TipH = null;
            Congelado = false;
            _id = 0;
            _sobreAntes = _sobreAhora; _sobreAhora = -1;
            _t = Time.realtimeSinceStartup;
            if (_lbl == null)
            {
                _lbl = new GUIStyle(GUI.skin.label);
                _lbl.wordWrap = true;
                _lbl.richText = true;
                _lbl.fontStyle = FontStyle.Bold;
                _campo = new GUIStyle(GUI.skin.textField);
                _campo.fontStyle = FontStyle.Bold;
                _campo.alignment = TextAnchor.MiddleCenter;
            }
        }

        public Rect R(float x, float y, float w, float h) { return new Rect(x * K, y * K, w * K, h * K); }

        public bool Sobre(float x, float y, float w, float h) { return new Rect(x, y, w, h).Contains(Raton); }

        public bool SobreInterfaz(Vector2 virt)
        {
            for (int i = 0; i < Bloqueos.Count; i++) if (Bloqueos[i].Contains(virt)) return true;
            return false;
        }

        public void Bloquear(float x, float y, float w, float h) { Bloqueos.Add(new Rect(x, y, w, h)); }

        // ================================================================ dibujo básico
        public void Rellenar(Rect pantalla, Color c)
        {
            if (Event.current.type != EventType.Repaint) return;
            Color old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(pantalla, Texture2D.whiteTexture);
            GUI.color = old;
        }

        public void Img(float x, float y, float w, float h, Texture tex, Color tinte)
        {
            if (Event.current.type != EventType.Repaint || tex == null) return;
            Color old = GUI.color;
            GUI.color = tinte;
            GUI.DrawTexture(R(x, y, w, h), tex, ScaleMode.ScaleToFit, true);
            GUI.color = old;
        }

        public void Img(float x, float y, float w, float h, Texture tex) { Img(x, y, w, h, tex, Color.white); }

        /// <summary>Dibuja una textura de 64x64 como panel de nueve cortes (el borde mide 'borde' unidades virtuales).</summary>
        public void Panel9(float x, float y, float w, float h, Texture2D tex, float borde, Color tinte)
        {
            if (Event.current.type != EventType.Repaint || tex == null) return;
            float b = Mathf.Min(borde, w * 0.5f, h * 0.5f) * K;
            Rect r = R(x, y, w, h);
            float u = 20f / 64f;   // borde de la textura (en fracción)
            float[] xs = { r.x, r.x + b, r.xMax - b, r.xMax };
            float[] ys = { r.y, r.y + b, r.yMax - b, r.yMax };
            float[] us = { 0f, u, 1f - u, 1f };
            float[] vs = { 1f, 1f - u, u, 0f };       // v crece hacia arriba en la textura
            Color old = GUI.color;
            GUI.color = tinte;
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    GUI.DrawTextureWithTexCoords(new Rect(xs[i], ys[j], xs[i + 1] - xs[i], ys[j + 1] - ys[j]), tex, new Rect(us[i], vs[j + 1], us[i + 1] - us[i], vs[j] - vs[j + 1]));
            GUI.color = old;
        }

        public void PanelCrema(float x, float y, float w, float h) { Panel9(x, y, w, h, Arte.PanelCrema, 22f, Color.white); Bloquear(x, y, w, h); }
        public void PanelOscuro(float x, float y, float w, float h) { Panel9(x, y, w, h, Arte.PanelOscuro, 22f, Color.white); Bloquear(x, y, w, h); }
        public void PanelRosa(float x, float y, float w, float h) { Panel9(x, y, w, h, Arte.PanelRosa, 22f, Color.white); Bloquear(x, y, w, h); }

        // ================================================================ texto
        public void Texto(float x, float y, float w, float h, string s, int tam, Color c, TextAnchor a, bool sombra)
        {
            if (Event.current.type != EventType.Repaint || string.IsNullOrEmpty(s)) return;
            _lbl.fontSize = Mathf.Max(6, Mathf.RoundToInt(tam * K));
            _lbl.alignment = a;
            Rect r = R(x, y, w, h);
            if (sombra)
            {
                _lbl.normal.textColor = new Color(0.1f, 0.05f, 0.15f, 0.55f);
                GUI.Label(new Rect(r.x + 1.5f * K, r.y + 1.5f * K, r.width, r.height), s, _lbl);
            }
            _lbl.normal.textColor = c;
            GUI.Label(r, s, _lbl);
        }

        public void Texto(float x, float y, float w, float h, string s, int tam, Color c, TextAnchor a) { Texto(x, y, w, h, s, tam, c, a, false); }
        public void Tx(float x, float y, float w, float h, string s, int tam) { Texto(x, y, w, h, s, tam, Tinta, TextAnchor.UpperLeft, false); }

        /// <summary>Texto con contorno grueso (títulos, números flotantes).</summary>
        public void Contorno(float x, float y, float w, float h, string s, int tam, Color c, TextAnchor a)
        {
            if (Event.current.type != EventType.Repaint || string.IsNullOrEmpty(s)) return;
            _lbl.fontSize = Mathf.Max(6, Mathf.RoundToInt(tam * K));
            _lbl.alignment = a;
            Rect r = R(x, y, w, h);
            float g = Mathf.Max(1.5f, tam * 0.07f) * K;
            _lbl.normal.textColor = new Color(0.12f, 0.06f, 0.18f, 0.95f);
            for (int i = 0; i < 8; i++)
            {
                float ang = i * Mathf.PI / 4f;
                GUI.Label(new Rect(r.x + Mathf.Cos(ang) * g, r.y + Mathf.Sin(ang) * g, r.width, r.height), s, _lbl);
            }
            _lbl.normal.textColor = c;
            GUI.Label(r, s, _lbl);
        }

        public float AltoTexto(string s, int tam, float ancho)
        {
            _lbl.fontSize = Mathf.Max(6, Mathf.RoundToInt(tam * K));
            return _lbl.CalcHeight(new GUIContent(s), ancho * K) / K;
        }

        public float AnchoTexto(string s, int tam)
        {
            _lbl.fontSize = Mathf.Max(6, Mathf.RoundToInt(tam * K));
            Vector2 v = _lbl.CalcSize(new GUIContent(s));
            return v.x / K;
        }

        // ================================================================ controles
        private bool Pulsacion(Rect vr, bool activo, out bool sobre, out bool pres)
        {
            int id = ++_id;
            if (Congelado) activo = false;
            sobre = activo && vr.Contains(Raton);
            Event e = Event.current;
            bool click = false;
            if (activo)
            {
                if (e.type == EventType.MouseDown && e.button == 0 && sobre) { _abajo = id; e.Use(); }
                else if (e.type == EventType.MouseUp && e.button == 0 && _abajo == id) { _abajo = -1; if (sobre) click = true; e.Use(); }
            }
            if (e.type == EventType.MouseUp && e.button == 0 && _abajo == id) _abajo = -1;
            pres = _abajo == id && sobre;
            if (sobre) _sobreAhora = id;
            if (e.type == EventType.Repaint && sobre && _sobreAntes != id && _sobreAhora == id) Sonido.Tocar("hover", 0.4f, 1f);
            Bloqueos.Add(vr);
            if (click) Sonido.Tocar("clic", 0.8f, 1f);
            return click;
        }

        /// <summary>Botón gordito. tono: amarillo, rojo, verde, azul, gris.</summary>
        public bool Boton(float x, float y, float w, float h, string txt, string tono, bool activo, int tam)
        {
            bool sobre, pres;
            bool click = Pulsacion(new Rect(x, y, w, h), activo, out sobre, out pres);
            if (Event.current.type == EventType.Repaint)
            {
                float lev = sobre && !pres ? -2f : (pres ? 2f : 0f);
                Panel9(x, y + lev, w, h, Arte.Boton(activo ? tono : "gris", pres), 20f, activo ? Color.white : new Color(1f, 1f, 1f, 0.75f));
                Texto(x, y + lev + (pres ? 1f : -1f), w, h - 4, txt, tam, activo ? new Color(0.2f, 0.1f, 0.2f) : new Color(0.45f, 0.4f, 0.5f), TextAnchor.MiddleCenter, false);
            }
            return click;
        }

        /// <summary>Botón con icono (acciones del combate). Devuelve true al hacer clic.</summary>
        public bool BotonAccion(float x, float y, float w, float h, Texture icono, string etiqueta, string tecla, string tono, bool activo, bool elegido, string insignia)
        {
            return BotonAccion(x, y, w, h, icono, etiqueta, tecla, tono, activo, elegido, insignia, "azul");
        }

        public bool BotonAccion(float x, float y, float w, float h, Texture icono, string etiqueta, string tecla, string tono, bool activo, bool elegido, string insignia, string tonoBase)
        {
            bool sobre, pres;
            bool click = Pulsacion(new Rect(x, y, w, h), activo, out sobre, out pres);
            if (Event.current.type == EventType.Repaint)
            {
                float lev = sobre && !pres ? -3f : (pres ? 2f : 0f);
                if (elegido) lev -= 4f;
                Panel9(x, y + lev, w, h, Arte.Boton(!activo ? "gris" : (elegido ? "amarillo" : tonoBase), pres), 20f, activo ? Color.white : new Color(1f, 1f, 1f, 0.7f));
                if (icono != null) Img(x + w * 0.5f - h * 0.30f, y + lev + 4f, h * 0.6f, h * 0.6f, icono, activo ? Color.white : new Color(1f, 1f, 1f, 0.45f));
                Texto(x, y + lev + h * 0.64f, w, h * 0.3f, etiqueta, 12, activo ? new Color(0.15f, 0.08f, 0.2f) : new Color(0.4f, 0.35f, 0.45f), TextAnchor.MiddleCenter, false);
                if (!string.IsNullOrEmpty(tecla))
                {
                    Panel9(x + 2f, y + lev - 3f, 20f, 18f, Arte.PanelOscuro, 8f, Color.white);
                    Texto(x + 2f, y + lev - 3f, 20f, 18f, tecla, 11, Crema, TextAnchor.MiddleCenter, false);
                }
                if (!string.IsNullOrEmpty(insignia))
                {
                    Panel9(x + w - 24f, y + lev + h - 24f, 24f, 20f, Arte.PanelRosa, 8f, Color.white);
                    Texto(x + w - 24f, y + lev + h - 24f, 24f, 20f, insignia, 12, Tinta, TextAnchor.MiddleCenter, false);
                }
            }
            return click;
        }

        /// <summary>Zona clicable invisible (tarjetas, retratos). Devuelve true al hacer clic.</summary>
        public bool Zona(float x, float y, float w, float h, bool activo)
        {
            bool sobre, pres;
            return Pulsacion(new Rect(x, y, w, h), activo, out sobre, out pres);
        }

        public string Campo(float x, float y, float w, float h, string valor, int max, int tam)
        {
            Bloquear(x, y, w, h);
            _campo.fontSize = Mathf.RoundToInt(tam * K);
            return GUI.TextField(R(x, y, w, h), valor, max, _campo);
        }

        // ================================================================ piezas compuestas
        public void Barra(float x, float y, float w, float h, float frac, Color color)
        {
            if (Event.current.type != EventType.Repaint) return;
            Panel9(x, y, w, h, Arte.BarraFondo, h * 0.5f, Color.white);
            frac = Mathf.Clamp01(frac);
            if (frac > 0.001f)
            {
                float ww = Mathf.Max(h - 4f, (w - 4f) * frac);
                Panel9(x + 2f, y + 2f, ww, h - 4f, Arte.BarraRelleno, (h - 4f) * 0.5f, color);
            }
        }

        public void Retrato(float x, float y, float tam, Texture sprite, bool gato, bool activo)
        {
            if (Event.current.type != EventType.Repaint) return;
            Panel9(x, y, tam, tam, gato ? Arte.PanelCrema : Arte.PanelRosa, tam * 0.3f, activo ? new Color(1f, 0.95f, 0.6f) : Color.white);
            Img(x + tam * 0.06f, y + tam * 0.04f, tam * 0.88f, tam * 0.88f, sprite);
            if (activo)
            {
                float p = 0.5f + 0.5f * Mathf.Sin(_t * 6f);
                Panel9(x - 3f, y - 3f, tam + 6f, tam + 6f, Arte.PanelOscuro, tam * 0.3f, new Color(1f, 0.85f, 0.2f, 0.25f + 0.25f * p));
            }
        }

        public void Corazones(float x, float y, float tam, int llenos, int total)
        {
            for (int i = 0; i < total; i++) Img(x + i * (tam + 2f), y, tam, tam, Arte.Icono(i < llenos ? "corazon" : "corazon_vacio"));
        }

        public void Gotas(float x, float y, float tam, int llenas, int total)
        {
            for (int i = 0; i < total; i++) Img(x + i * (tam * 0.8f), y, tam, tam, Arte.Icono(i < llenas ? "gota" : "gota_vacia"));
        }

        /// <summary>Etiqueta pequeña tipo "pastilla" (chip).</summary>
        public float Chip(float x, float y, string txt, Color fondo, int tam)
        {
            float w = AnchoTexto(txt, tam) + 14f;
            if (Event.current.type == EventType.Repaint)
            {
                Panel9(x, y, w, tam + 8f, Arte.PanelCrema, (tam + 8f) * 0.4f, fondo);
                Texto(x, y, w, tam + 8f, txt, tam, Tinta, TextAnchor.MiddleCenter, false);
            }
            return w;
        }

        public void PonerTip(float x, float y, float w, float h, string texto)
        {
            if (!Congelado && new Rect(x, y, w, h).Contains(Raton)) { Tip = texto; TipH = null; }
        }

        /// <summary>Globo corto de una habilidad: nombre, coste y palabras clave con icono. El clic abre la ficha completa.</summary>
        public void PonerTipHab(float x, float y, float w, float h, HabilidadDef hab, string clase, string pista)
        {
            if (!Congelado && new Rect(x, y, w, h).Contains(Raton)) { TipH = hab; TipClase = clase; TipPista = pista ?? "Clic: descripción completa"; Tip = ""; }
        }

        // ================================================================ palabras clave con icono
        /// <summary>Una fila de claves (icono + texto corto). Devuelve el ancho usado. Cada clave tiene su explicación al pasar el ratón.</summary>
        public float Claves(float x, float y, List<Clave> claves, int tam, Color texto, bool conTip)
        {
            float cx = x;
            float ic = tam + 6f;
            foreach (Clave k in claves)
            {
                float tw = string.IsNullOrEmpty(k.Texto) ? 0f : AnchoTexto(k.Texto, tam) + 2f;
                Img(cx, y + 1f, ic, ic, Arte.IconoClave(k.Icono));
                if (tw > 0f) Texto(cx + ic, y, tw + 4f, ic + 2f, k.Texto, tam, texto, TextAnchor.MiddleLeft, false);
                float ww = ic + tw + 6f;
                if (conTip) PonerTip(cx, y, ww, ic + 2f, Textos.Glosario(k.Icono));
                cx += ww;
            }
            return cx - x;
        }

        public float AnchoClaves(List<Clave> claves, int tam)
        {
            float w = 0f;
            foreach (Clave k in claves) w += (tam + 6f) + (string.IsNullOrEmpty(k.Texto) ? 0f : AnchoTexto(k.Texto, tam) + 2f) + 6f;
            return w;
        }

        /// <summary>Número con icono delante (vida, defensa, maná, movimiento...).</summary>
        public float Dato(float x, float y, string icono, string valor, int tam, Color color, string tip)
        {
            float ic = tam + 8f;
            Img(x, y, ic, ic, Arte.IconoClave(icono));
            float tw = AnchoTexto(valor, tam) + 4f;
            Texto(x + ic, y, tw, ic, valor, tam, color, TextAnchor.MiddleLeft, false);
            if (!string.IsNullOrEmpty(tip)) PonerTip(x, y, ic + tw, ic, tip);
            return ic + tw + 6f;
        }

        /// <summary>Dibuja el globo de ayuda junto al ratón (llamar al final de OnGUI).</summary>
        public void DibujarTip()
        {
            if (TipH != null) { DibujarTipHab(); return; }
            if (string.IsNullOrEmpty(Tip)) return;
            float ancho = 280f;
            float alto = AltoTexto(Tip, 13, ancho - 20f) + 16f;
            float x = Mathf.Min(Raton.x + 16f, VW - ancho - 8f), y = Raton.y - alto - 10f;
            if (y < 6f) y = Raton.y + 22f;
            if (x < 6f) x = 6f;
            Panel9(x, y, ancho, alto, Arte.PanelOscuro, 14f, new Color(1f, 1f, 1f, 0.97f));
            Texto(x + 10f, y + 7f, ancho - 20f, alto - 10f, Tip, 13, Crema, TextAnchor.UpperLeft, false);
        }

        private void DibujarTipHab()
        {
            HabilidadDef h = TipH;
            var claves = Textos.Claves(h);
            float ancho = Mathf.Max(190f, AnchoClaves(claves, 14) + 24f, AnchoTexto(h.Nombre, 16) + 90f);
            float alto = 78f;
            float x = Mathf.Min(Raton.x + 16f, VW - ancho - 8f), y = Raton.y - alto - 10f;
            if (y < 6f) y = Raton.y + 22f;
            if (x < 6f) x = 6f;
            Panel9(x, y, ancho, alto, Arte.PanelOscuro, 14f, new Color(1f, 1f, 1f, 0.97f));
            Img(x + 8f, y + 8f, 30f, 30f, Arte.IconoHab(h, TipClase));
            Texto(x + 44f, y + 6f, ancho - 90f, 24f, h.Nombre, 16, Oro, TextAnchor.MiddleLeft, false);
            Img(x + ancho - 46f, y + 9f, 20f, 20f, Arte.IconoClave(h.CosteMana == 0 ? "gratis" : "mana"));
            if (h.CosteMana > 0) Texto(x + ancho - 26f, y + 8f, 20f, 22f, h.CosteMana.ToString(), 15, Crema, TextAnchor.MiddleLeft, false);
            Claves(x + 10f, y + 38f, claves, 14, Crema, false);
            Texto(x + 10f, y + 58f, ancho - 20f, 16f, TipPista, 10, new Color(1f, 1f, 1f, 0.55f), TextAnchor.MiddleLeft, false);
        }

        public void Fundido(float alfa)
        {
            if (alfa <= 0.001f) return;
            Rellenar(new Rect(0, 0, Screen.width, Screen.height), new Color(0.06f, 0.03f, 0.1f, alfa));
        }

        public void Vineta()
        {
            if (Event.current.type != EventType.Repaint) return;
            Color old = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Arte.Vineta, ScaleMode.StretchToFill, true);
            GUI.color = old;
        }
    }
}
