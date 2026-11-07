// Gatomate - vista. El mapa del mundo: aparece al llegar a cada nivel. Muestra los 3 niveles del mundo con el jefe marcado, un campamento
// antes de cada nivel, la party caminando de un nivel al siguiente y, al elegir un nivel, los enemigos que habrá (para decidir qué gatos llevar).
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Gatomate.Core;

namespace Gatomate.View
{
    public sealed partial class GatomateJuego
    {
        private bool _enMapa;                       // true = se ve el mapa en vez del campamento
        private string _nodoMapa = "";              // nivel (y intento) para el que ya se mostró el mapa
        private float _mapaT;
        private bool _mapaAnim;                     // la party camina desde el nivel anterior
        private int _mapaMundoVista = 1, _mapaNivelSel = 1;
        private int _prevM, _prevN, _lastM, _lastN;
        private readonly Dictionary<string, GeneradorNivel.PlanNivel> _planes = new Dictionary<string, GeneradorNivel.PlanNivel>();

        // ---------------------------------------------------------------- seguimiento de nodos (se llama desde Update)
        private void SeguirMapa()
        {
            if (_run == null) { _nodoMapa = ""; _lastM = 0; _lastN = 0; _planes.Clear(); return; }
            if (_run.EsTutorial) { _enMapa = false; return; }
            if (_run.Mundo != _lastM || _run.Nivel != _lastN) { _prevM = _lastM; _prevN = _lastN; _lastM = _run.Mundo; _lastN = _run.Nivel; }
            if (_run.Fase == FaseRun.Preparacion)
            {
                string k = _run.Mundo + "-" + _run.Nivel + "-" + _run.Intento;
                if (k != _nodoMapa)
                {
                    _nodoMapa = k;
                    _enMapa = true; _mapaT = 0f;
                    _mapaMundoVista = _run.Mundo; _mapaNivelSel = _run.Nivel;
                    _mapaAnim = (_prevM == _run.Mundo && _prevN == _run.Nivel - 1 && _run.Intento == 1) || (_prevM == _run.Mundo - 1 && _prevN == Config.NivelesPorMundo && _run.Nivel == 1 && _run.Intento == 1);
                }
            }
        }

        // ---------------------------------------------------------------- planes de nivel (enemigos y tablero que saldrán)
        private GeneradorNivel.PlanNivel PlanDe(int mundo, int nivel)
        {
            if (_run == null || _run.EsTutorial) return null;
            string k = _run.Semilla + "-" + mundo + "-" + nivel;
            GeneradorNivel.PlanNivel p;
            if (!_planes.TryGetValue(k, out p))
            {
                p = GeneradorNivel.Planear(mundo, nivel, Azar.Derivar(_run.Semilla, mundo, nivel));
                _planes[k] = p;
            }
            return p;
        }

        /// <summary>Los enemigos del nivel actual (agrupados por tipo) y el tamaño del tablero. En el tutorial salen del nivel fijo.</summary>
        private List<KeyValuePair<EnemigoDef, int>> EnemigosDe(int mundo, int nivel, out int ancho, out int alto)
        {
            var defs = new List<EnemigoDef>();
            ancho = Config.TableroAncho; alto = Config.TableroAlto;
            if (_run != null && _run.EsTutorial)
            {
                Combate c = GeneradorNivel.CrearTutorial(nivel, new List<Gato>(), new List<EfectoActivo>(), new Dictionary<Consumible, int>());
                foreach (Unidad u in c.Vivos(Bando.Enemigos)) defs.Add(u.Def);
            }
            else
            {
                GeneradorNivel.PlanNivel p = PlanDe(mundo, nivel);
                if (p != null) { defs.AddRange(p.Enemigos); ancho = p.Tab.Ancho; alto = p.Tab.Alto; }
            }
            var res = new List<KeyValuePair<EnemigoDef, int>>();
            foreach (EnemigoDef d in defs)
            {
                int i = res.FindIndex(delegate (KeyValuePair<EnemigoDef, int> kv) { return kv.Key.Id == d.Id; });
                if (i < 0) res.Add(new KeyValuePair<EnemigoDef, int>(d, 1));
                else res[i] = new KeyValuePair<EnemigoDef, int>(d, res[i].Value + 1);
            }
            return res;
        }

        /// <summary>Retratos de los enemigos con su cantidad y debilidad. Devuelve el alto usado.</summary>
        private float ListaEnemigos(float x, float y, float w, List<KeyValuePair<EnemigoDef, int>> lista, float tam)
        {
            float cx = 0f, cy = 0f;
            foreach (var kv in lista)
            {
                EnemigoDef d = kv.Key;
                if (cx + tam > w) { cx = 0f; cy += tam + 6f; }
                float ex = x + cx, ey = y + cy;
                _ui.Retrato(ex, ey, tam, Arte.TexEnemigo(d.Id), false, d.EsJefe);
                if (kv.Value > 1) { _ui.Panel9(ex + tam - 22f, ey - 4f, 26f, 20f, Arte.PanelRosa, 8f, Color.white); _ui.Texto(ex + tam - 22f, ey - 4f, 26f, 20f, "x" + kv.Value, 12, Interfaz.Tinta, TextAnchor.MiddleCenter); }
                if (d.Debil != Elemento.Fisico) _ui.Img(ex + tam - 18f, ey + tam - 18f, 18f, 18f, Arte.IconoClave(Textos.IconoElemento(d.Debil)));
                if (_ayudas) _ui.PonerTip(ex, ey, tam, tam, TextoEnemigo(d));
                cx += tam + 8f;
            }
            return cy + tam;
        }

        /// <summary>Resumen de debilidades: cuántos enemigos son débiles a cada elemento (ayuda a elegir gatos).</summary>
        private string ResumenDebilidades(List<KeyValuePair<EnemigoDef, int>> lista)
        {
            var cuenta = new Dictionary<Elemento, int>();
            foreach (var kv in lista)
                if (kv.Key.Debil != Elemento.Fisico) { int n; cuenta.TryGetValue(kv.Key.Debil, out n); cuenta[kv.Key.Debil] = n + kv.Value; }
            if (cuenta.Count == 0) return "Sin debilidades elementales: sirven los ataques físicos.";
            var sb = new StringBuilder("Débiles a: ");
            bool primero = true;
            foreach (var kv in cuenta) { if (!primero) sb.Append(", "); primero = false; sb.Append(Textos.Nombre(kv.Key).ToLower()).Append(" (").Append(kv.Value).Append(")"); }
            return sb.ToString();
        }

        // ---------------------------------------------------------------- mini tablero
        private static Color ColorTerreno(Terreno t)
        {
            switch (t)
            {
                case Terreno.HierbaAlta: return new Color(0.45f, 0.72f, 0.4f);
                case Terreno.Flores: return new Color(0.95f, 0.6f, 0.75f);
                case Terreno.Barro: return new Color(0.52f, 0.36f, 0.22f);
                case Terreno.Agua: return new Color(0.3f, 0.55f, 0.92f);
                case Terreno.Roca: return new Color(0.5f, 0.5f, 0.58f);
                case Terreno.Muro: return new Color(0.32f, 0.32f, 0.4f);
                case Terreno.RocaFragil: return new Color(0.68f, 0.65f, 0.7f);
                case Terreno.Caja: return new Color(0.85f, 0.62f, 0.3f);
                case Terreno.Barril: return new Color(0.75f, 0.3f, 0.25f);
                default: return new Color(0.86f, 0.8f, 0.62f);
            }
        }

        private void MiniTablero(float x, float y, float cell, GeneradorNivel.PlanNivel p)
        {
            Tablero t = p.Tab;
            _ui.Rellenar(_ui.R(x - 3f, y - 3f, t.Ancho * cell + 6f, t.Alto * cell + 6f), new Color(0.2f, 0.12f, 0.25f));
            for (int i = 0; i < t.Ancho; i++)
                for (int j = 0; j < t.Alto; j++)
                    _ui.Rellenar(_ui.R(x + i * cell, y + j * cell, cell - 1f, cell - 1f), ColorTerreno(t.TerrenoEn(new Pos(i, j))));
            foreach (Pos q in p.PosGatos) _ui.Rellenar(_ui.R(x + q.X * cell + 2f, y + q.Y * cell + 2f, cell - 5f, cell - 5f), new Color(0.2f, 0.95f, 0.95f));
            for (int k = 0; k < p.PosEnemigos.Count; k++)
            {
                Pos q = p.PosEnemigos[k];
                bool jefe = p.Enemigos[k].EsJefe;
                _ui.Rellenar(_ui.R(x + q.X * cell + (jefe ? 0f : 2f), y + q.Y * cell + (jefe ? 0f : 2f), cell - (jefe ? 1f : 5f), cell - (jefe ? 1f : 5f)), jefe ? new Color(0.7f, 0f, 0.15f) : new Color(1f, 0.25f, 0.3f));
            }
        }

        // ---------------------------------------------------------------- pantalla del mapa
        private void PantallaMapa()
        {
            _mapaT += Time.deltaTime;
            CabeceraRun("");
            BarraSuperiorDerecha(false);
            float px = 40f, py = 72f, pw = _ui.VW - 80f, ph = Interfaz.VH - py - 16f;
            _ui.PanelCrema(px, py, pw, ph);
            _ui.Texto(px + 24f, py + 10f, 400f, 30f, "Mapa del mundo", 24, Interfaz.Tinta, TextAnchor.MiddleLeft);

            // ---- pestañas de mundos
            int total = Math.Min(Config.MundosJugables, GameData.Mundos.Count);
            float tx = px + pw - 24f - total * 46f;
            for (int m = 1; m <= total; m++)
            {
                string tono = m == _mapaMundoVista ? "amarillo" : (m < _run.Mundo ? "verde" : (m == _run.Mundo ? "azul" : "gris"));
                if (_ui.Boton(tx + (m - 1) * 46f, py + 10f, 42f, 32f, m.ToString(), tono, true, 15)) { _mapaMundoVista = m; _mapaNivelSel = m == _run.Mundo ? _run.Nivel : 1; }
                if (_ayudas) _ui.PonerTip(tx + (m - 1) * 46f, py + 10f, 42f, 32f, NombreMundo(m) + (m < _run.Mundo ? " (superado)" : (m == _run.Mundo ? " (aquí estás)" : "")));
            }
            int mv = _mapaMundoVista;
            MundoDef mundo = GameData.Mundos[Math.Min(mv, 7) - 1];
            _ui.Texto(px + 24f, py + 40f, 700f, 24f, NombreMundo(mv), 15, Suave, TextAnchor.MiddleLeft);

            // ---- camino: campamento 1, nivel 1, campamento 2, nivel 2, campamento 3, jefe
            const int Paradas = 6;
            float x0 = px + 100f, paso = (pw - 200f) / (Paradas - 1), cy = py + 140f;
            float[] xs = new float[Paradas];
            for (int i = 0; i < Paradas; i++) xs[i] = x0 + paso * i;
            int progreso = mv < _run.Mundo ? Paradas : (mv == _run.Mundo ? 2 * (_run.Nivel - 1) : -1);   // índice de la parada donde está la party
            for (int i = 0; i < Paradas - 1; i++)
            {
                bool hecho = i < progreso;
                for (float d = xs[i] + 30f; d < xs[i + 1] - 36f; d += 22f)
                    _ui.Rellenar(_ui.R(d, cy - 3f, 12f, 6f), hecho ? new Color(0.95f, 0.7f, 0.15f) : new Color(0.55f, 0.48f, 0.62f));
            }
            for (int i = 0; i < Paradas; i++)
            {
                bool campamento = i % 2 == 0;
                int nivel = i / 2 + 1;
                if (campamento)
                {
                    bool aqui = i == progreso;
                    _ui.Img(xs[i] - 26f, cy - 26f, 52f, 52f, Arte.Icono("ob_tienda"), i <= progreso || progreso < 0 && false ? Color.white : new Color(1f, 1f, 1f, 0.5f));
                    _ui.Texto(xs[i] - 50f, cy + 26f, 100f, 18f, "Campamento", 11, Suave, TextAnchor.MiddleCenter);
                    if (mv == _run.Mundo && nivel == _run.Nivel && _ui.Zona(xs[i] - 28f, cy - 28f, 56f, 56f, true)) _enMapa = false;
                    if (_ayudas && mv == _run.Mundo && nivel == _run.Nivel) _ui.PonerTip(xs[i] - 28f, cy - 28f, 56f, 56f, "Entrar al campamento: elige a tus 4 gatos y su equipo");
                }
                else
                {
                    bool jefe = nivel >= Config.NivelesPorMundo;
                    bool superado = mv < _run.Mundo || (mv == _run.Mundo && nivel < _run.Nivel);
                    bool actual = mv == _run.Mundo && nivel == _run.Nivel;
                    bool sel = nivel == _mapaNivelSel;
                    float sz = jefe ? 74f : 58f;
                    string tono = superado ? "verde" : (jefe ? "rojo" : (actual ? "amarillo" : "gris"));
                    if (_ui.Boton(xs[i] - sz / 2f, cy - sz / 2f - (sel ? 4f : 0f), sz, sz, "", tono, true, 12)) { _mapaNivelSel = nivel; Sonido.Tocar("clic"); }
                    if (jefe) _ui.Img(xs[i] - 26f, cy - 28f, 52f, 52f, Arte.TexEnemigo(mundo.JefeId));
                    else _ui.Texto(xs[i] - sz / 2f, cy - sz / 2f - 3f, sz, sz, nivel.ToString(), 26, new Color(0.2f, 0.1f, 0.2f), TextAnchor.MiddleCenter);
                    _ui.Texto(xs[i] - 60f, cy + sz / 2f + 2f, 120f, 18f, jefe ? "¡JEFE!" : "Nivel " + nivel, 12, jefe ? new Color(0.75f, 0.2f, 0.3f) : Interfaz.Tinta, TextAnchor.MiddleCenter);
                    if (superado) _ui.Texto(xs[i] + sz / 2f - 12f, cy - sz / 2f - 6f, 24f, 24f, "OK", 11, new Color(0.1f, 0.5f, 0.2f), TextAnchor.MiddleCenter, true);
                    if (_ayudas) _ui.PonerTip(xs[i] - sz / 2f, cy - sz / 2f, sz, sz, (jefe ? "Jefe de " : "Nivel ") + (jefe ? mundo.Nombre : nivel.ToString()) + ". Clic para ver qué enemigos habrá.");
                }
            }

            // ---- la party (4 gatitos) sobre su parada, caminando si viene del nivel anterior
            if (mv == _run.Mundo)
            {
                float destino = xs[Math.Max(0, Math.Min(Paradas - 1, progreso))];
                float origen = destino;
                float u = 1f;
                if (_mapaAnim)
                {
                    origen = _run.Nivel == 1 ? px - 10f : xs[progreso - 1];
                    u = Mathf.Clamp01((_mapaT - 0.5f) / 1.8f);
                    u = u * u * (3f - 2f * u);
                }
                float x = Mathf.Lerp(origen, destino, u);
                bool andando = _mapaAnim && u > 0f && u < 1f;
                for (int i = 0; i < _run.Expedicion.Count; i++)
                {
                    float rebote = andando ? Mathf.Abs(Mathf.Sin(_mapaT * 11f + i * 0.9f)) * 9f : Mathf.Sin(Time.realtimeSinceStartup * 2f + i) * 1.5f;
                    _ui.Retrato(x - 62f + i * 32f, cy - 78f - rebote, 36f, Arte.TexGato(_run.Expedicion[i].ClaseId), true, false);
                }
                _ui.Texto(x - 60f, cy - 96f, 120f, 18f, andando ? "¡En marcha!" : "Tu party", 12, Interfaz.Tinta, TextAnchor.MiddleCenter);
            }

            // ---- detalle del nivel elegido
            float dy = py + 236f, dh = ph - 236f - 12f;
            _ui.Panel9(px + 18f, dy, pw - 36f, dh, Arte.PanelOscuro, 22f, Color.white);
            bool jefeSel = _mapaNivelSel >= Config.NivelesPorMundo;
            int aw, ah;
            var enem = EnemigosDe(mv, _mapaNivelSel, out aw, out ah);
            _ui.Texto(px + 40f, dy + 8f, 560f, 28f, (jefeSel ? "Jefe de " + mundo.Nombre : "Nivel " + _mapaNivelSel + " de " + mundo.Nombre) + "  -  tablero " + aw + "x" + ah, 18, Interfaz.Crema, TextAnchor.MiddleLeft);
            _ui.Texto(px + 40f, dy + 36f, 300f, 20f, "Enemigos que te esperan:", 13, new Color(0.85f, 0.8f, 0.95f), TextAnchor.MiddleLeft);
            float lh = ListaEnemigos(px + 40f, dy + 62f, 640f, enem, 56f);
            _ui.Texto(px + 40f, dy + 62f + lh + 8f, 700f, 22f, ResumenDebilidades(enem), 13, new Color(1f, 0.9f, 0.6f), TextAnchor.MiddleLeft);
            GeneradorNivel.PlanNivel plan = PlanDe(mv, _mapaNivelSel);
            if (plan != null)
            {
                float cell = Mathf.Min(16f, (dh - 56f) / Mathf.Max(plan.Tab.Alto, 1));
                float mx = px + pw - 36f - plan.Tab.Ancho * cell - 140f;
                MiniTablero(mx, dy + 36f, cell, plan);
                _ui.Texto(mx + plan.Tab.Ancho * cell + 12f, dy + 34f, 130f, 80f, "Azul: tus gatos\nRojo: enemigos\nAzul oscuro: agua\nMarrón: barro", 10, new Color(0.85f, 0.8f, 0.95f), TextAnchor.UpperLeft);
                if (_ayudas) _ui.PonerTip(mx, dy + 36f, plan.Tab.Ancho * cell, plan.Tab.Alto * cell, "Así será el tablero: los gatos empiezan en el lado azul y los enemigos juntos en el rojo.");
            }
            bool esElActual = mv == _run.Mundo && _mapaNivelSel == _run.Nivel;
            if (_ui.Boton(px + pw - 300f, py + ph - 76f, 270f, 54f, esElActual ? "IR AL CAMPAMENTO" : "Ver mi campamento", "amarillo", mv == _run.Mundo, 20)) { _enMapa = false; Sonido.Tocar("clic"); }
            if (mv != _run.Mundo) _ui.Texto(px + pw - 300f, py + ph - 96f, 270f, 18f, "Solo puedes acampar en el mundo actual", 11, Suave, TextAnchor.MiddleRight);
        }
    }
}
