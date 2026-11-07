// Gatomate - vista. Las piezas "limpias" de la interfaz: tarjeta de gato (preparación), tarjeta de unidad (combate, con debilidades),
// iconos de habilidad con globo corto + ficha completa al hacer clic, y la inspección con clic derecho (alcance y amenaza).
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Gatomate.Core;

namespace Gatomate.View
{
    public sealed partial class GatomateJuego
    {
        private Unidad _insp;                      // unidad que se está inspeccionando (clic derecho)
        private HabilidadDef _fichaHab;            // habilidad cuya ficha completa está abierta
        private string _fichaClase = "luchador";
        private string _fichaPasiva;               // texto completo de una pasiva (ficha)
        private bool _verRegistro;
        private Pos? _casillaInfo;                 // casilla vacía en la que se hizo clic derecho (para ver qué es)

        private static readonly Color Morado = new Color(0.45f, 0.2f, 0.4f);
        private static readonly Color Suave = new Color(0.45f, 0.35f, 0.55f);

        /// <summary>Claves en filas que se envuelven al llegar al ancho. Devuelve el alto usado (dibujar=false solo mide).</summary>
        private float ClavesEnv(float x, float y, float w, List<Clave> ks, int tam, Color col, bool tip, bool dibujar)
        {
            float cx = 0f, cy = 0f, fila = tam + 8f;
            var uno = new List<Clave>(1);
            foreach (Clave k in ks)
            {
                uno.Clear(); uno.Add(k);
                float kw = _ui.AnchoClaves(uno, tam);
                if (cx > 0f && cx + kw > w) { cx = 0f; cy += fila; }
                if (dibujar) _ui.Claves(x + cx, y + cy, uno, tam, col, tip);
                cx += kw;
            }
            return cy + fila;
        }

        private void AbrirFicha(HabilidadDef h, string clase) { _fichaHab = h; _fichaClase = clase; _fichaPasiva = null; Sonido.Tocar("carta", 0.6f, 1.2f); }

        // ---------------------------------------------------------------- inspección con clic derecho
        /// <summary>Casillas a las que puede llegar la unidad y casillas que podría atacar después de moverse.</summary>
        private void CalcularInspeccion(Combate c, out List<Pos> mover, out List<Pos> amenaza)
        {
            mover = new List<Pos>(); amenaza = new List<Pos>();
            if (_insp == null || !_insp.Vivo) { _insp = null; return; }
            Unidad u = _insp;
            var reach = c.Alcance(u, c.MovimientoTotal(u), null);
            var origenes = new List<Pos>();
            foreach (var kv in reach)
            {
                origenes.Add(kv.Key);
                if (kv.Key != u.Pos && c.UnidadEn(kv.Key) == null) mover.Add(kv.Key);
            }
            // alcance de ataque: el mayor de sus ataques
            int rMin = 99, rMax = 0;
            var ataques = new List<HabilidadDef>();
            if (u.EsGato) { ataques.AddRange(u.Habilidades); if (u.Basico != null) ataques.Add(u.Basico); }
            else if (u.Habilidad != null) ataques.Add(u.Habilidad);
            foreach (HabilidadDef h in ataques)
            {
                if (!h.EsAtaque || h.Movilidad || h.Efecto == EfectoHab.AreaAlrededor && h.Radio == 0) continue;
                int a = h.Efecto == EfectoHab.AreaAlrededor ? 1 : c.RangoMinEfectivo(u, h);
                int b = h.Efecto == EfectoHab.AreaAlrededor ? h.Radio : c.RangoMaxEfectivo(u, h);
                if (a < rMin) rMin = a;
                if (b > rMax) rMax = b;
            }
            if (u.Def != null && u.Def.Kamikaze) { rMin = 1; rMax = Math.Max(rMax, 1); }
            if (rMax < 1) return;
            for (int x = 0; x < c.Tab.Ancho; x++)
                for (int y = 0; y < c.Tab.Alto; y++)
                {
                    Pos q = new Pos(x, y);
                    if (!c.Tab.Pasable(q)) continue;
                    foreach (Pos o in origenes)
                    {
                        int d = o.Dist(q);
                        if (d >= Math.Max(1, rMin) && d <= rMax) { amenaza.Add(q); break; }
                    }
                }
        }

        // ---------------------------------------------------------------- iconos de habilidad (globo corto, clic = ficha)
        private void IconoHabilidadBoton(float x, float y, float sz, HabilidadDef h, string clase)
        {
            bool gratis = h.CosteMana == 0;
            Color tinte = gratis ? new Color(0.8f, 1f, 0.8f) : Color.white;
            _ui.Panel9(x, y, sz, sz, Arte.PanelOscuro, sz * 0.28f, tinte);
            _ui.Img(x + sz * 0.1f, y + sz * 0.06f, sz * 0.8f, sz * 0.8f, Arte.IconoHab(h, clase));
            _ui.Img(x + sz - 16f, y + sz - 16f, 16f, 16f, Arte.IconoClave(gratis ? "gratis" : "mana"));
            if (!gratis) _ui.Texto(x + sz - 16f, y + sz - 17f, 16f, 16f, h.CosteMana.ToString(), 10, Color.white, TextAnchor.MiddleCenter, true);
            if (h.Movilidad) _ui.Img(x - 2f, y - 2f, 14f, 14f, Arte.IconoClave("libre"));
            if (_ui.Zona(x, y, sz, sz, true)) AbrirFicha(h, clase);
            if (_ayudas) _ui.PonerTipHab(x, y, sz, sz, h, clase, null);
        }

        private float DatoNum(float x, float y, string icono, int valor, int tam, string tip)
        {
            return _ui.Dato(x, y, icono, valor.ToString(), tam, Interfaz.Tinta, _ayudas ? tip : null);
        }

        // ---------------------------------------------------------------- tarjeta del gato en la preparación
        private void TarjetaGato(Gato g, float x, float y, float w, float h)
        {
            bool sel = g == _selExp;
            Stats s = g.Stats;
            _ui.Panel9(x, y, w, h, Arte.PanelCrema, 22f, sel ? new Color(1f, 0.95f, 0.55f) : Color.white);
            _ui.Bloquear(x, y, w, h);
            _ui.Retrato(x + 10f, y + 10f, 58f, Arte.TexGato(g.ClaseId), true, sel);
            _ui.Texto(x + 76f, y + 6f, w - 86f, 24f, g.Nombre, 18, Interfaz.Tinta, TextAnchor.UpperLeft);
            _ui.Texto(x + 76f, y + 8f, w - 88f, 20f, "Nv " + g.Nivel, 13, Morado, TextAnchor.UpperRight);
            if (_ayudas) _ui.PonerTip(x + 76f, y + 6f, w - 86f, 24f, "Nivel " + g.Nivel + " de " + Config.NivelMaximo + (g.Nivel < Config.NivelMaximo ? ". Experiencia: " + g.Xp + "/" + Progreso.XpParaSubir(g.Nivel) + " (1 por enemigo que derrote; los jefes dan " + Config.XpJefe + "). Cada nivel: +2 de vida y +1 a su estadística principal. En los niveles 3 y 6 aprende una habilidad más." : " (máximo)."));
            _ui.Barra(x + 76f, y + 30f, w - 88f, 16f, (float)g.Vida / Mathf.Max(1, g.VidaMax), new Color(0.45f, 0.85f, 0.4f));
            if (g.Nivel < Config.NivelMaximo) _ui.Barra(x + 76f, y + 46f, w - 88f, 5f, (float)g.Xp / Progreso.XpParaSubir(g.Nivel), new Color(0.95f, 0.8f, 0.3f));
            _ui.Texto(x + 76f, y + 29f, w - 88f, 18f, g.Vida + "/" + g.VidaMax, 11, Color.white, TextAnchor.MiddleCenter, true);
            if (_ayudas) _ui.PonerTip(x + 76f, y + 30f, w - 88f, 16f, "Vida actual / máxima");
            float dx = x + 76f;
            dx += DatoNum(dx, y + 48f, "defensa", s.Defensa, 12, "Defensa: resta daño de cada golpe");
            dx += DatoNum(dx, y + 48f, "gota", s.Mana, 12, "Maná: lo gastan las habilidades (+1 por turno)");
            dx += DatoNum(dx, y + 48f, "bota", g.Movimiento, 12, "Movimiento: casillas por turno");
            DatoNum(dx, y + 48f, "st_vel", s.Velocidad, 12, "Velocidad: quién juega antes");
            float sx = x + 10f;
            sx += DatoNum(sx, y + 72f, "st_fue", s.Fuerza, 12, "Fuerza (Fue): suma daño cuerpo a cuerpo");
            sx += DatoNum(sx, y + 72f, "st_int", s.Inteligencia, 12, "Inteligencia (Int): suma daño mágico y curación");
            sx += DatoNum(sx, y + 72f, "st_des", s.Destreza, 12, "Destreza (Des): suma daño a distancia y de pícaro");
            DatoNum(sx, y + 72f, "st_sue", s.Suerte, 12, "Suerte (Sue): ayuda en los desafíos de recompensa");

            // habilidades: ataque básico + las 2 elegidas
            float hx = x + 10f, hy = y + 100f, sz = g.Habilidades.Count >= 3 ? 36f : 40f;
            if (g.Basico != null) { IconoHabilidadBoton(hx, hy, sz, g.Basico, g.ClaseId); hx += sz + 6f; }
            foreach (HabilidadDef hab in g.Habilidades) { IconoHabilidadBoton(hx, hy, sz, hab, g.ClaseId); hx += sz + 6f; }

            // pasiva: nombre + claves
            float py = y + 142f;
            _ui.Texto(x + 10f, py, w - 20f, 16f, Textos.PasivaNombre(g.Clase), 12, Morado, TextAnchor.MiddleLeft);
            if (_ayudas) _ui.PonerTip(x + 10f, py, w - 20f, 16f, g.Clase.PasivaTexto);
            ClavesEnv(x + 10f, py + 16f, w - 20f, Textos.ClavesPasiva(g.Clase.Pasiva), 11, Interfaz.Tinta, _ayudas, true);

            // equipo: dos espacios, Cuerpo y Arma
            float ew = (w - 28f) / 2f, ey = y + h - 34f;
            for (int j = 0; j < 2; j++)
            {
                SlotObjeto slot = j == 0 ? SlotObjeto.Cuerpo : SlotObjeto.Arma;
                float ex = x + 10f + j * (ew + 8f);
                ObjetoDef od = g.Objeto(slot);
                if (od != null)
                {
                    EquipoTipo q = od.Id;
                    _ui.Panel9(ex, ey, ew, 28f, Arte.PanelOscuro, 10f, TinteSlot(q));
                    _ui.Img(ex + 3f, ey + 1f, 26f, 26f, Arte.IconoEquipo(q));
                    _ui.Texto(ex + 30f, ey, ew - 32f, 28f, Textos.Nombre(q), 9, Interfaz.Crema, TextAnchor.MiddleLeft);
                    if (_ui.Zona(ex, ey, ew, 28f, true)) { _run.Desequipar(g, q); Sonido.Tocar("clic"); return; }
                    if (_ayudas) _ui.PonerTip(ex, ey, ew, 28f, TextoObjeto(q) + "\nClic para quitarlo.");
                }
                else
                {
                    _ui.Panel9(ex, ey, ew, 28f, Arte.BarraFondo, 10f, new Color(1f, 1f, 1f, 0.35f));
                    _ui.Texto(ex, ey, ew, 28f, "+ " + Objetos.NombreSlot(slot), 11, new Color(1f, 1f, 1f, 0.55f), TextAnchor.MiddleCenter);
                    if (_ayudas) _ui.PonerTip(ex, ey, ew, 28f, "Espacio de " + Objetos.NombreSlot(slot).ToLower() + " vacío. Elige un objeto de ese tipo en el equipo sin asignar.");
                }
            }
            if (_ui.Zona(x, y, w, h, true))
            {
                if (_selEquipo >= 0 && _selEquipo < _run.EquipoSinAsignar.Count)
                {
                    if (_run.Equipar(g, _run.EquipoSinAsignar[_selEquipo])) Tut(TutAccion.Equipar);
                    _selEquipo = -1;
                    Sonido.Tocar("carta");
                }
                else _selExp = sel ? null : g;
            }
        }

        // ---------------------------------------------------------------- tarjeta de una casilla (clic derecho en una casilla)
        private void TarjetaCasilla(Combate c, Pos p, float x, float y, float w, float h)
        {
            Terreno t = c.Tab.TerrenoEn(p);
            _ui.Panel9(x, y, w, h, Arte.PanelCrema, 22f, new Color(1f, 0.97f, 0.8f));
            _ui.Bloquear(x, y, w, h);
            string nombre = Textos.Nombre(t);
            int par = nombre.IndexOf(" (");
            string titulo = par > 0 ? nombre.Substring(0, par) : nombre;
            string detalle = par > 0 ? nombre.Substring(par + 2).TrimEnd(')') : "Casilla normal: sin efectos.";
            _ui.Rellenar(_ui.R(x + 14f, y + 14f, 44f, 44f), ColorTerreno(t));
            _ui.Texto(x + 70f, y + 8f, w - 80f, 28f, titulo, 20, Interfaz.Tinta, TextAnchor.UpperLeft);
            _ui.Texto(x + 70f, y + 36f, w - 80f, 22f, "Casilla " + p.X + "," + p.Y, 11, Suave, TextAnchor.UpperLeft);
            float dh = _ui.AltoTexto(detalle, 13, w - 28f);
            _ui.Texto(x + 14f, y + 66f, w - 28f, dh + 4f, char.ToUpper(detalle[0]) + detalle.Substring(1) + (par > 0 ? "." : ""), 13, Interfaz.Tinta, TextAnchor.UpperLeft);
            float yy = y + 66f + dh + 8f;
            if (c.Tab.EsRompible(p))
            {
                int vida = c.Tab.VidaObstaculo(p), max = Math.Max(1, Tablero.VidaInicial(t));
                _ui.Barra(x + 14f, yy, w - 28f, 16f, (float)vida / max, new Color(0.9f, 0.55f, 0.25f));
                _ui.Texto(x + 14f, yy - 1f, w - 28f, 18f, "Resistencia " + vida + " / " + max, 11, Color.white, TextAnchor.MiddleCenter, true);
                yy += 22f;
                _ui.Texto(x + 14f, yy, w - 28f, 30f, "Se rompe atacándola: lo agrietado 8, la roca 16, la roca grande 24.", 10, Suave, TextAnchor.UpperLeft);
                yy += 28f;
            }
            Zona z = c.Tab.ZonaEn(p);
            if (z != null) { _ui.Texto(x + 14f, yy, w - 28f, 34f, Textos.Nombre(z.Tipo) + " - " + z.Turnos + " rondas", 12, Morado, TextAnchor.UpperLeft); yy += 34f; }
            int mon = c.Tab.MonedasEn(p);
            if (mon > 0) { _ui.Img(x + 14f, yy, 22f, 22f, Arte.Icono("moneda")); _ui.Texto(x + 40f, yy, w - 54f, 22f, mon + " moneda(s): pásate por encima", 12, Interfaz.Tinta, TextAnchor.MiddleLeft); }
            _ui.Texto(x + w - 150f, y + 2f, 142f, 16f, "clic derecho: cerrar", 10, Morado, TextAnchor.MiddleRight);
        }

        // ---------------------------------------------------------------- tarjeta de unidad en combate
        private void TarjetaUnidad(Combate c, Unidad u, float x, float y, float w, float h)
        {
            bool inspeccionada = u == _insp;
            _ui.Panel9(x, y, w, h, u.EsGato ? Arte.PanelCrema : Arte.PanelRosa, 22f, inspeccionada ? new Color(1f, 0.95f, 0.7f) : Color.white);
            _ui.Bloquear(x, y, w, h);
            VU v = _esc.Vis(u);
            int vida = v != null ? Mathf.RoundToInt(v.VidaVis) : u.Vida;
            _ui.Retrato(x + 10f, y + 10f, 62f, Arte.TexUnidad(u), u.EsGato, u == c.Activa);
            _ui.Texto(x + 80f, y + 6f, w - 90f, 24f, u.Nombre, 18, Interfaz.Tinta, TextAnchor.UpperLeft);
            if (u.EsGato && u.Gato != null) _ui.Texto(x + 80f, y + 8f, w - 94f, 20f, "Nv " + u.Gato.Nivel, 13, Morado, TextAnchor.UpperRight);
            _ui.Barra(x + 80f, y + 32f, w - 94f, 16f, (float)vida / Mathf.Max(1, u.VidaMax), u.EsGato ? new Color(0.45f, 0.85f, 0.4f) : new Color(1f, 0.4f, 0.5f));
            _ui.Texto(x + 80f, y + 31f, w - 94f, 18f, vida + " / " + u.VidaMax, 11, Color.white, TextAnchor.MiddleCenter, true);
            if (u.EsGato) _ui.Gotas(x + 80f, y + 50f, 16f, u.Mana, u.ManaMax);
            float dx = x + 10f, dy = y + 76f;
            dx += DatoNum(dx, dy, "defensa", u.DefensaEfectiva, 12, "Defensa");
            dx += DatoNum(dx, dy, "bota", c.MovimientoTotal(u), 12, "Movimiento por turno (ahora)");
            dx += DatoNum(dx, dy, "st_vel", u.VelocidadEfectiva, 12, "Velocidad: quién juega antes");
            if (u.EsGato)
            {
                dx += DatoNum(dx, dy, "st_fue", u.Fuerza, 12, "Fuerza");
                dx += DatoNum(dx, dy, "st_int", u.Inteligencia, 12, "Inteligencia");
                DatoNum(dx, dy, "st_des", u.Destreza, 12, "Destreza");
                float hx = x + 10f, hy = y + 102f, sz = u.Habilidades.Count >= 3 ? 34f : 38f;
                if (u.Basico != null) { IconoHabilidadBoton(hx, hy, sz, u.Basico, u.Gato.ClaseId); hx += sz + 6f; }
                foreach (HabilidadDef hab in u.Habilidades) { IconoHabilidadBoton(hx, hy, sz, hab, u.Gato.ClaseId); hx += sz + 6f; }
                if (u.Habilidades.Count < 3)
                {
                    _ui.Claves(hx + 4f, hy + 8f, Textos.ClavesPasiva(u.Pasiva), 11, Interfaz.Tinta, _ayudas);
                    if (_ayudas) _ui.PonerTip(hx + 4f, hy, w - hx - 10f, sz, u.Gato.Clase.PasivaTexto);
                }
                else
                {
                    ClavesEnv(x + 10f, hy + sz + 4f, w - 20f, Textos.ClavesPasiva(u.Pasiva), 10, Interfaz.Tinta, _ayudas, true);
                }
                // objetos equipados
                float ox = x + w - 66f;
                foreach (EquipoTipo q in u.Equipo)
                {
                    _ui.Img(ox, y + 54f, 20f, 20f, Arte.IconoEquipo(q));
                    if (_ayudas) _ui.PonerTip(ox, y + 54f, 20f, 20f, TextoObjeto(q));
                    ox += 22f;
                }
            }
            else if (u.Def != null && u.Habilidad != null)
            {
                // débil / resiste con icono, a la derecha de los números
                float ex = x + 150f;
                _ui.Texto(ex, dy - 1f, 40f, 20f, "Débil", 11, Morado, TextAnchor.MiddleLeft);
                if (u.Def.Debil != Elemento.Fisico) { _ui.Img(ex + 38f, dy, 18f, 18f, Arte.IconoClave(Textos.IconoElemento(u.Def.Debil))); if (_ayudas) _ui.PonerTip(ex, dy, 60f, 20f, Textos.Nombre(u.Def.Debil) + ": recibe más daño de este elemento"); }
                else _ui.Texto(ex + 38f, dy - 1f, 20f, 20f, "-", 12, Suave, TextAnchor.MiddleLeft);
                ex += 70f;
                _ui.Texto(ex, dy - 1f, 50f, 20f, "Resiste", 11, Morado, TextAnchor.MiddleLeft);
                if (u.Def.Resiste != Elemento.Fisico) { _ui.Img(ex + 48f, dy, 18f, 18f, Arte.IconoClave(Textos.IconoElemento(u.Def.Resiste))); if (_ayudas) _ui.PonerTip(ex, dy, 70f, 20f, Textos.Nombre(u.Def.Resiste) + ": recibe menos daño de este elemento"); }
                else _ui.Texto(ex + 48f, dy - 1f, 20f, 20f, "-", 12, Suave, TextAnchor.MiddleLeft);
                // su ataque en claves
                float hy = y + 102f;
                _ui.Texto(x + 10f, hy, w - 20f, 20f, u.Habilidad.Nombre, 14, Interfaz.Tinta, TextAnchor.MiddleLeft);
                ClavesEnv(x + 10f, hy + 22f, w - 20f, Textos.Claves(u.Habilidad), 12, Interfaz.Tinta, _ayudas, true);
                if (inspeccionada && !string.IsNullOrEmpty(u.Def.Nota))
                {
                    string nota = u.Def.Nota.Length > 120 ? u.Def.Nota.Substring(0, 118) + "..." : u.Def.Nota;
                    _ui.Texto(x + 10f, y + 146f, w - 20f, 30f, nota, 10, Morado, TextAnchor.UpperLeft);
                }
            }
            float cx = x + 10f;
            foreach (EstadoActivo es in u.Estados)
            {
                float ww = _ui.Chip(cx, y + h - 26f, Textos.Nombre(es.Tipo) + " " + es.Turnos, EsEstadoMalo(es.Tipo) ? new Color(1f, 0.7f, 0.6f) : new Color(1f, 0.9f, 0.5f), 10);
                if (_ayudas) _ui.PonerTip(cx, y + h - 26f, ww, 18f, Textos.Nombre(es.Tipo) + " (" + es.Turnos + " turnos)");
                cx += ww + 4f;
            }
            if (inspeccionada) _ui.Texto(x + w - 150f, y + 2f, 142f, 16f, "inspeccionando", 10, Morado, TextAnchor.MiddleRight);
        }

        // ---------------------------------------------------------------- ficha completa de una habilidad (ventana)
        private void DibujarFicha()
        {
            if (_fichaHab == null) return;
            HabilidadDef h = _fichaHab;
            _ui.Rellenar(new Rect(0, 0, Screen.width, Screen.height), new Color(0.05f, 0.02f, 0.1f, 0.6f));
            _ui.Bloquear(0, 0, _ui.VW, Interfaz.VH);
            float w = Mathf.Min(560f, _ui.VW - 40f);
            string desc = h.Descripcion;
            float dh = _ui.AltoTexto(desc, 15, w - 48f);
            var claves = Textos.Claves(h);
            float kh = ClavesEnv(0, 0, w - 140f, claves, 15, Interfaz.Tinta, false, false);
            float extra = (h.Movilidad ? 44f : 0f) + (h.EmpujeDist > 0 ? 44f : 0f);
            float cabecera = Mathf.Max(112f, 80f + kh + 8f);
            float alto = cabecera + 14f + dh + extra + 84f;
            float x = (_ui.VW - w) / 2f, y = (Interfaz.VH - alto) / 2f;
            _ui.PanelCrema(x, y, w, alto);
            _ui.Img(x + 18f, y + 16f, 80f, 80f, Arte.IconoHab(h, _fichaClase));
            _ui.Texto(x + 112f, y + 14f, w - 130f, 34f, h.Nombre, 26, Interfaz.Tinta, TextAnchor.UpperLeft);
            float cxp = x + 112f, cyp = y + 52f;
            cxp += _ui.Chip(cxp, cyp, h.Basico ? "Ataque básico" : h.Categoria, ColorCategoria(h.Categoria), 12) + 4f;
            cxp += _ui.Chip(cxp, cyp, h.CosteMana == 0 ? "Gratis" : h.CosteMana + " de maná", new Color(0.7f, 0.85f, 1f), 12) + 4f;
            if (h.Elemento != Elemento.Fisico) _ui.Chip(cxp, cyp, Textos.Nombre(h.Elemento), EscenaIso.ColorElem(h.Elemento, Color.white), 12);
            ClavesEnv(x + 112f, y + 82f, w - 140f, claves, 15, Interfaz.Tinta, true, true);
            float ty = y + cabecera + 14f;
            _ui.Texto(x + 24f, ty, w - 48f, dh + 6f, desc, 15, Interfaz.Tinta, TextAnchor.UpperLeft);
            ty += dh + 10f;
            if (h.Movilidad)
            {
                _ui.Panel9(x + 24f, ty, w - 48f, 38f, Arte.PanelCrema, 12f, new Color(0.75f, 0.9f, 1f));
                _ui.Texto(x + 32f, ty, w - 64f, 38f, "Movilidad: no gasta tu acción del turno (puedes atacar después). Solo cuesta maná.", 12, Interfaz.Tinta, TextAnchor.MiddleLeft);
                ty += 44f;
            }
            if (h.EmpujeDist > 0)
            {
                _ui.Panel9(x + 24f, ty, w - 48f, 38f, Arte.PanelCrema, 12f, new Color(1f, 0.93f, 0.75f));
                _ui.Texto(x + 32f, ty, w - 64f, 38f, "Empuja " + h.EmpujeDist + (h.EmpujeDist == 1 ? " casilla" : " casillas") + ": si choca con algo, recibe daño.", 12, Interfaz.Tinta, TextAnchor.MiddleLeft);
            }
            if (_ui.Boton(x + w / 2f - 80f, y + alto - 56f, 160f, 42f, "Cerrar", "amarillo", true, 18)) _fichaHab = null;
        }

        private bool ModalAbierto { get { return _fichaHab != null || TutorialEsVentana(); } }
    }
}
