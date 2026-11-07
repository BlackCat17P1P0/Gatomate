// Gatomate - vista de Unity (prototipo).
// Escena isométrica dibujada por código (EscenaIso) con la interfaz encima (IMGUI, estilo "dibujos" tipo Mewgenics),
// animaciones, partículas y sonido procedural. No hay escenas, prefabs ni imágenes: se arranca sola al dar Play en
// cualquier escena (RuntimeInitializeOnLoadMethod). Las reglas viven en Core y no se tocan desde aquí.
// Controles de combate: clic = actuar, clic derecho en un gato/enemigo = inspeccionar (movimiento y amenaza),
// clic derecho en una habilidad = descripción completa, Q mover, A ataque básico, W/E/R/S habilidades 1 a 4, 1-8 objetos, | menú de desarrollador,
// Espacio terminar turno, Esc cancelar, T turno automático, F velocidad, M sonido.
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Gatomate.Core;
using Random = UnityEngine.Random;

namespace Gatomate.View
{
    public sealed partial class GatomateJuego : MonoBehaviour
    {
        private enum Modo { Mover, Habilidad, Consumible }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Arrancar()
        {
            if (GameObject.Find("Gatomate") != null) return;
            var go = new GameObject("Gatomate");
            go.AddComponent<GatomateJuego>();
            DontDestroyOnLoad(go);
        }

        // ---------------------------------------------------------------- estado de la vista
        private Run _run;
        private EscenaIso _esc;
        private readonly Interfaz _ui = new Interfaz();
        private Modo _modo = Modo.Mover;
        private Consumible _cons;
        private int _habIdx;                 // qué habilidad está elegida en el modo Habilidad: -1 = ataque básico, 0 y 1 = las dos habilidades
        private Gato _selExp;
        private int _selEquipo = -1;
        private string _semillaTxt = "";
        private int _mundosElegidos = 7;
        private bool _cuatroClases;
        private string _clave = "";
        private Combate _combateCargado;
        private float _fade = 1f, _tPantalla;
        private int _velIdx;
        private static readonly float[] Velocidades = { 1f, 2f, 3.5f };
        private List<Gato> _gatosTitulo;
        private float _confirmaSalir;
        private float _resultadoT;
        private bool _resultadoBien, _resultadoPendiente;

        // marcas calculadas cada fotograma (en Update)
        private List<Pos> _mover = new List<Pos>(), _objs = new List<Pos>(), _area = new List<Pos>(), _camino = new List<Pos>();
        private Pos? _hover;
        private Unidad _hoverU;
        private bool _turnoJugador;

        // ---------------------------------------------------------------- arranque
        private void Awake()
        {
            _semillaTxt = (Environment.TickCount & 0xFFFFF).ToString();
            CargarOpciones();
            Config.GrabarEventos = true;           // la vista necesita el registro de eventos para animar
            Sonido.Iniciar();
            Arte.PrecargarBase();
            Camera cam = Camera.main;
            if (cam == null)
            {
                var cg = new GameObject("Camara Gatomate");
                cam = cg.AddComponent<Camera>();
                cg.AddComponent<AudioListener>();
                cg.tag = "MainCamera";
            }
            _esc = gameObject.AddComponent<EscenaIso>();
            _esc.Iniciar(cam);
            string[] clases = { "luchador", "mago", "tanque", "cazador", "clerigo", "picaro", "druida", "alquimista" };
            _gatosTitulo = new List<Gato>();
            var usadas = new List<string>(clases);
            for (int i = 0; i < 4; i++)
            {
                int k = Random.Range(0, usadas.Count);
                string id = usadas[k]; usadas.RemoveAt(k);
                var g = new Gato { Id = 900 + i, ClaseId = id, Nombre = GameData.Clase(id).Nombre };
                g.Vida = g.VidaMax;
                _gatosTitulo.Add(g);
            }
        }

        // ---------------------------------------------------------------- pantallas
        private enum Pantalla { Titulo, Mapa, Preparacion, Combate, Recompensa, ElegirHabilidad, Tienda, Final }

        private Pantalla Actual
        {
            get
            {
                if (_run == null) return Pantalla.Titulo;
                switch (_run.Fase)
                {
                    case FaseRun.Preparacion: return _enMapa && !_run.EsTutorial ? Pantalla.Mapa : Pantalla.Preparacion;
                    case FaseRun.Combate: return Pantalla.Combate;
                    case FaseRun.Recompensa: return Pantalla.Recompensa;
                    case FaseRun.ElegirHabilidad: return Pantalla.ElegirHabilidad;
                    case FaseRun.Tienda: return Pantalla.Tienda;
                    default: return Pantalla.Final;
                }
            }
        }

        private string ClaveEscena()
        {
            Pantalla p = Actual;
            if (p == Pantalla.Titulo) return "titulo";
            if (p == Pantalla.Mapa) p = Pantalla.Preparacion;       // el mapa y el campamento comparten escena
            var sb = new StringBuilder();
            sb.Append(p).Append('-').Append(_run.Mundo).Append('-').Append(_run.Nivel).Append('-').Append(_run.Intento);
            if (p == Pantalla.Preparacion || p == Pantalla.Recompensa || p == Pantalla.ElegirHabilidad || p == Pantalla.Tienda)
                foreach (Gato g in _run.Expedicion) sb.Append('-').Append(g.Id);
            return sb.ToString();
        }

        private void NuevaRun()
        {
            int sem;
            if (!int.TryParse(_semillaTxt, out sem)) sem = Environment.TickCount;
            Config.MundosJugables = _mundosElegidos;
            Config.ClasesHabilitadas = _cuatroClases ? new HashSet<string> { "luchador", "mago", "cazador", "clerigo" } : null;
            _run = new Run(sem);
            _selExp = null; _selEquipo = -1; _modo = Modo.Mover; _insp = null;
            Sonido.Tocar("victoria", 0.5f, 1.2f);
        }
        // ---------------------------------------------------------------- Update
        private void Update()
        {
            float dt = Time.deltaTime;
            Sonido.Actualizar(dt);
            if (_fade > 0f) _fade = Mathf.Max(0f, _fade - dt * 1.8f);
            if (_resultadoT > 0f) _resultadoT -= dt;
            if (_confirmaSalir > 0f) _confirmaSalir -= dt;
            _tPantalla += dt;
            _esc.Vel = Actual == Pantalla.Combate ? Velocidades[_velIdx] : 1f;

            SeguirMapa();
            // ¿cambió la pantalla?
            string k = ClaveEscena();
            if (k != _clave)
            {
                _clave = k;
                _fade = 1f; _tPantalla = 0f;
                _selExp = null; _selEquipo = -1; _modo = Modo.Mover; _insp = null; _fichaHab = null; _casillaInfo = null;
                AutoGuardar();
                CargarEscena();
            }
            if (_run != null && _run.Fase == FaseRun.Combate && _run.Combate != _combateCargado)
            {
                _combateCargado = _run.Combate;
                _insp = null;
                _esc.CargarNivel(_run.Combate);
                _esc.Encolar(_run.Combate.TomarEventos());
                Sonido.Musica(_run.EsNivelDeJefe ? "jefe" : "combate");
            }
            if (Actual == Pantalla.Preparacion || Actual == Pantalla.Mapa) _esc.CampSel = Actual == Pantalla.Mapa ? null : _selExp;
            else if (Actual == Pantalla.ElegirHabilidad) _esc.CampSel = _run.GatoEligiendo;
            else _esc.CampSel = null;
            if (Actual == Pantalla.Combate) CalcularMarcas();
            else _esc.SinMarcas();
        }

        private void CargarEscena()
        {
            Pantalla p = Actual;
            _combateCargado = null;
            if (p == Pantalla.Titulo)
            {
                _esc.CargarCampamento(Random.Range(1, 8), _gatosTitulo);
                Sonido.Musica("calma");
            }
            else if (p == Pantalla.Combate) { /* lo carga Update al ver el combate nuevo */ }
            else
            {
                _esc.CargarCampamento(Math.Min(_run.Mundo, 7), _run.Expedicion);
                Sonido.Musica("calma");
                if (p == Pantalla.Final) _esc.Celebrar(_run.Fase == FaseRun.CampanaCompletada);
                if (_resultadoPendiente)
                {
                    _resultadoPendiente = false;
                    _esc.Anunciar(_resultadoBien ? "¡Salió bien!" : "Salió mal...", _resultadoBien ? new Color(0.7f, 1f, 0.7f) : new Color(1f, 0.6f, 0.6f));
                    _esc.Celebrar(_resultadoBien);
                }
            }
        }

        /// <summary>La habilidad elegida en este momento (A = ataque básico, W = la primera, E = la segunda).</summary>
        private HabilidadDef HabSel(Unidad act)
        {
            if (act == null) return null;
            if (_habIdx < 0 && act.Basico != null) return act.Basico;
            if (act.Habilidades != null && act.Habilidades.Count > 0) return act.Habilidades[Math.Max(0, Math.Min(_habIdx, act.Habilidades.Count - 1))];
            return act.Habilidad;
        }

        private void CalcularMarcas()
        {
            Combate c = _run.Combate;
            Unidad act = c.Activa;
            bool enCurso = c.Estado == EstadoCombate.EnCurso;
            bool ocupado = _esc.Ocupado;
            _turnoJugador = enCurso && act != null && act.EsGato && !ocupado && !ModalAbierto;
            _mover = new List<Pos>(); _objs = new List<Pos>(); _area = new List<Pos>(); _camino = new List<Pos>();
            _hover = null; _hoverU = null;

            // inspección (clic derecho): se ve aunque no sea tu turno
            List<Pos> im, am;
            CalcularInspeccion(c, out im, out am);
            _esc.MarcarInspeccion(im, am);

            // ratón → casilla (el ratón de IMGUI tiene el origen arriba; el de la cámara abajo)
            Vector2 px = new Vector2(_ui.Raton.x * _ui.K, Screen.height - _ui.Raton.y * _ui.K);
            if (!_ui.SobreInterfaz(_ui.Raton) && !ocupado)
            {
                _hover = _esc.CasillaBajo(px);
                if (_hover.HasValue && c.Tab.Dentro(_hover.Value)) _hoverU = c.UnidadEn(_hover.Value);
                else _hover = null;
            }
            if (!_turnoJugador) { _esc.Marcar(null, null, null, _hover, null); return; }

            if (_modo == Modo.Mover) _mover = c.CasillasDeMovimiento();
            else if (_modo == Modo.Habilidad && c.PuedeUsar(act, HabSel(act))) _objs = c.CasillasObjetivo(act, HabSel(act));
            else if (_modo == Modo.Consumible && c.PuedeUsarConsumible(act, _cons)) _objs = c.CasillasConsumible(act, _cons);
            if (_hover.HasValue)
            {
                Pos h = _hover.Value;
                if (_modo == Modo.Mover && _mover.Contains(h)) _camino = c.Camino(h);
                else if (_objs.Contains(h))
                {
                    if (_modo == Modo.Habilidad) _area = c.AreaDeEfecto(act, HabSel(act), h);
                    else if (Combate.EsLanzable(_cons)) _area = c.Tab.Cruz(h);
                }
            }
            _esc.Marcar(_mover, _objs, _area, _hover, _camino);
        }

        // ---------------------------------------------------------------- OnGUI
        private void OnGUI()
        {
            if (_esc == null) return;
            _ui.Empezar();
            Event e = Event.current;
            TeclaDebug(e);
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.M && Actual != Pantalla.Titulo) { Sonido.Silencio = !Sonido.Silencio; GuardarOpciones(); e.Use(); }
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape && _fichaHab != null) { _fichaHab = null; e.Use(); }

            if (Actual == Pantalla.Combate) _ui.Vineta();
            _ui.Congelado = ModalAbierto;          // con una ventana abierta, lo de debajo no responde
            switch (Actual)
            {
                case Pantalla.Titulo: PantallaTitulo(); break;
                case Pantalla.Mapa: PantallaMapa(); break;
                case Pantalla.Preparacion: PantallaPreparacion(); break;
                case Pantalla.Combate: PantallaCombate(); break;
                case Pantalla.Recompensa: PantallaRecompensa(); break;
                case Pantalla.ElegirHabilidad: PantallaElegirHabilidad(); break;
                case Pantalla.Tienda: PantallaTienda(); break;
                default: PantallaFinal(); break;
            }
            _ui.Congelado = false;
            DibujarTutorial();
            DibujarFicha();
            PantallaDebug();
            DibujarBanner();
            _ui.DibujarTip();
            _ui.Fundido(_fade);
            if (_esc.Destello > 0.01f) _ui.Rellenar(new Rect(0, 0, Screen.width, Screen.height), new Color(1f, 1f, 1f, _esc.Destello));
        }

        private void DibujarBanner()
        {
            if (_esc.BannerT <= 0f || string.IsNullOrEmpty(_esc.Banner)) return;
            float u = 1f - _esc.BannerT / _esc.BannerDur;           // 0..1
            float entra = Mathf.Clamp01(u * 6f), sale = Mathf.Clamp01((1f - u) * 5f);
            float a = Mathf.Min(entra, sale);
            float esc = 1f + (1f - entra) * 0.8f;
            float w = Mathf.Min(_ui.VW - 40f, 760f);
            float y = Actual == Pantalla.Combate ? 112f : 150f;
            _ui.Panel9((_ui.VW - w) / 2f, y, w, 74f, Arte.PanelOscuro, 22f, new Color(1f, 1f, 1f, 0.92f * a));
            Color c = _esc.BannerCol; c.a = a;
            _ui.Contorno((_ui.VW - w) / 2f, y + 6f, w, 62f, _esc.Banner, Mathf.RoundToInt(40f * esc), c, TextAnchor.MiddleCenter);
        }

        // ================================================================ utilidades de interfaz
        private void BarraSuperiorDerecha(bool combate, bool menu = true)
        {
            float x = _ui.VW - 12f;
            if (menu)
            {
                if (_ui.Boton(x - 70f, 10f, 70f, 34f, _confirmaSalir > 0f ? "¿Seguro?" : "Menú", _confirmaSalir > 0f ? "rojo" : "gris", true, 13))
                {
                    if (_confirmaSalir > 0f) { _run = null; _menu = Menu.Principal; _confirmaSalir = 0f; }
                    else _confirmaSalir = 3f;
                }
                _ui.PonerTip(x - 70f, 10f, 70f, 34f, _run != null && _run.EsTutorial ? "Volver al título (el tutorial no se guarda)" : "Volver al título. La run se guarda sola al empezar cada nivel.");
                x -= 78f;
            }
            if (_ui.Boton(x - 70f, 10f, 70f, 34f, Sonido.Silencio ? "Sin son." : "Sonido", Sonido.Silencio ? "gris" : "verde", true, 13)) { Sonido.Silencio = !Sonido.Silencio; GuardarOpciones(); }
            x -= 78f;
            if (combate)
            {
                if (_ui.Boton(x - 54f, 10f, 54f, 34f, "x" + (_velIdx == 0 ? "1" : (_velIdx == 1 ? "2" : "3.5")), "azul", true, 14)) { _velIdx = (_velIdx + 1) % Velocidades.Length; GuardarOpciones(); }
                _ui.PonerTip(x - 54f, 10f, 54f, 34f, "Velocidad de las animaciones (tecla F)");
                x -= 62f;
                if (_ui.Boton(x - 78f, 10f, 78f, 34f, "Registro", _verRegistro ? "amarillo" : "gris", true, 13)) _verRegistro = !_verRegistro;
                _ui.PonerTip(x - 78f, 10f, 78f, 34f, "Muestra u oculta el registro de lo que pasa en el combate");
            }
        }

        private static string NombreMundo(int m) { MundoDef d = GameData.Mundos[Math.Min(m, 7) - 1]; return d.Nombre + " - " + d.Ambiente; }

        private string StatTxt(Stats s)
        {
            return "Def " + s.Defensa + "  Maná " + s.Mana + "  Vel " + s.Velocidad;
        }

        private static float VH() { return Interfaz.VH; }

        // ================================================================ PREPARACIÓN
        /// <summary>Moneda + total de la run.</summary>
        private void DineroTxt(float x, float y)
        {
            _ui.Img(x, y, 26f, 26f, Arte.Icono("moneda"));
            _ui.Texto(x + 28f, y, 60f, 26f, _run.Dinero.ToString(), 18, Interfaz.Oro, TextAnchor.MiddleLeft, true);
            _ui.PonerTip(x, y, 80f, 26f, "Monedas de la run. Sirven en la tienda y para repetir las 3 opciones de habilidad. Las recoges pasando por encima o rompiendo cajas.");
        }

        private void CabeceraRun(string extra)
        {
            float w = 560f, x = (_ui.VW - w) / 2f;
            _ui.PanelOscuro(x, 8f, w, 56f);
            string t = _run.EsTutorial ? "Tutorial" : NombreMundo(_run.Mundo);
            _ui.Texto(x + 18f, 10f, w - 150f, 28f, t, 19, Interfaz.Crema, TextAnchor.MiddleLeft, false);
            _ui.Texto(x + 18f, 34f, w - 150f, 24f, "Nivel " + _run.Nivel + "/" + Config.NivelesPorMundo + (_run.EsNivelDeJefe ? "  -  ¡JEFE!" : "") + "   Intento " + _run.Intento + extra, 14, _run.EsNivelDeJefe ? new Color(1f, 0.6f, 0.5f) : new Color(0.85f, 0.8f, 0.95f), TextAnchor.MiddleLeft, false);
            _ui.Corazones(x + w - 118f, 16f, 30f, Math.Max(0, _run.Vidas), Config.VidasPorRun);
            _ui.PonerTip(x + w - 118f, 16f, 100f, 30f, "Vidas de la run: si caen los 4 gatos pierdes una");
            DineroTxt(x + w - 214f, 18f);
        }

        private void PantallaPreparacion()
        {
            CabeceraRun("");
            BarraSuperiorDerecha(false);
            if (!_run.EsTutorial && _ui.Boton(268f, 16f, 84f, 36f, "Mapa", "azul", true, 15)) { _enMapa = true; _mapaT = 5f; _mapaAnim = false; _mapaMundoVista = _run.Mundo; _mapaNivelSel = _run.Nivel; }
            if (!_run.EsTutorial) _ui.PonerTip(268f, 16f, 84f, 36f, "Volver al mapa del mundo");
            if (!string.IsNullOrEmpty(_run.Mensaje))
            {
                float w = Mathf.Min(_ui.VW - 500f, 760f);
                _ui.PanelRosa((_ui.VW - w) / 2f, 70f, w, 40f);
                _ui.Texto((_ui.VW - w) / 2f + 14f, 70f, w - 28f, 40f, _run.Mensaje, 13, Interfaz.Tinta, TextAnchor.MiddleCenter);
            }
            float cardTop = Interfaz.VH - 226f;

            // ---- columna izquierda: reserva y equipo
            float lx = 12f, lw = 244f, ly = 120f;
            int nr = _run.Reserva.Count;
            float rh = 40f + Mathf.Max(1, nr) * 52f;
            _ui.PanelCrema(lx, ly, lw, rh);
            _ui.Texto(lx + 14f, ly + 8f, lw - 28f, 24f, "Reserva", 18, Interfaz.Tinta, TextAnchor.UpperLeft);
            if (nr == 0) _ui.Texto(lx + 14f, ly + 38f, lw - 28f, 40f, "Vacía. Los jefes sueltan gatos.", 12, Suave, TextAnchor.UpperLeft);
            for (int i = 0; i < nr; i++)
            {
                Gato g = _run.Reserva[i];
                float yy = ly + 36f + i * 52f;
                _ui.Retrato(lx + 12f, yy, 44f, Arte.TexGato(g.ClaseId), true, false);
                _ui.Texto(lx + 62f, yy - 2f, 100f, 20f, g.Nombre, 14, Interfaz.Tinta, TextAnchor.UpperLeft);
                _ui.Barra(lx + 62f, yy + 20f, 90f, 12f, (float)g.Vida / Mathf.Max(1, g.VidaMax), new Color(0.45f, 0.85f, 0.4f));
                _ui.Texto(lx + 62f, yy + 18f, 90f, 16f, g.Vida + "/" + g.VidaMax, 10, Color.white, TextAnchor.MiddleCenter, true);
                if (_ui.Boton(lx + lw - 80f, yy + 4f, 70f, 32f, "Cambiar", "azul", _selExp != null, 12))
                {
                    _run.Intercambiar(_selExp, g);
                    Tut(TutAccion.Intercambiar);
                    _selExp = null;
                }
                _ui.PonerTip(lx + 62f, yy - 2f, 90f, 40f, FichaLarga(g));
                _ui.PonerTip(lx + 12f, yy, 44f, 44f, FichaLarga(g));
                if (_selExp == null) _ui.PonerTip(lx + lw - 80f, yy + 4f, 70f, 32f, "Elige antes un gato de tu equipo (abajo) para cambiarlo por este.");
            }

            float ey = ly + rh + 8f;
            int cols = 5;
            float ih = 42f;
            int filasMax = Mathf.Max(1, (int)Mathf.Floor((cardTop - 6f - ey - 34f - 38f) / (ih + 4f)));
            int n = _run.EquipoSinAsignar.Count;
            int filas = Mathf.Max(1, Mathf.Min(filasMax, (n + cols - 1) / cols));
            float eh = 34f + filas * (ih + 4f) + 38f;
            _ui.PanelCrema(lx, ey, lw, eh);
            _ui.Texto(lx + 14f, ey + 6f, lw - 28f, 24f, "Equipo sin asignar", 17, Interfaz.Tinta, TextAnchor.UpperLeft);
            if (n == 0) _ui.Texto(lx + 14f, ey + 34f, lw - 28f, 30f, "Nada por ahora. Sale de los desafíos y la tienda.", 12, Suave, TextAnchor.UpperLeft);
            else _ui.Texto(lx + 120f, ey + 8f, lw - 130f, 18f, "verde: cuerpo, naranja: arma", 9, Suave, TextAnchor.MiddleRight);
            for (int i = 0; i < n && i < cols * filas; i++)
            {
                EquipoTipo q = _run.EquipoSinAsignar[i];
                float ix = lx + 12f + (i % cols) * (ih + 2f), iy = ey + 32f + (i / cols) * (ih + 4f);
                bool s = _selEquipo == i;
                _ui.Panel9(ix, iy, ih, ih, s ? Arte.Boton("amarillo", false) : Arte.PanelOscuro, 12f, s ? Color.white : TinteSlot(q));
                _ui.Img(ix + 4f, iy + 4f, ih - 8f, ih - 8f, Arte.IconoEquipo(q));
                if (_ui.Zona(ix, iy, ih, ih, true)) _selEquipo = s ? -1 : i;
                if (_ayudas) _ui.PonerTip(ix, iy, ih, ih, TextoObjeto(q) + "\nElígelo y luego pulsa un gato para equiparlo (reemplaza al que lleve en ese espacio).");
            }
            if (n > cols * filas) _ui.Texto(lx + 14f, ey + eh - 62f, lw - 28f, 16f, "+" + (n - cols * filas) + " más", 11, Suave, TextAnchor.MiddleLeft);
            if (n > 0 && _ui.Boton(lx + 12f, ey + eh - 36f, lw - 24f, 28f, "Equipar solo", "verde", true, 12))
                BotJugador.Equipar(_run);

            // ---- columna derecha: el nivel (los enemigos reales de este nivel)
            float rw = 244f, rx = _ui.VW - rw - 12f, ry = 120f;
            MundoDef m = GameData.Mundos[Math.Min(_run.Mundo, 7) - 1];
            int tw, th;
            var enemNivel = EnemigosDe(_run.Mundo, _run.Nivel, out tw, out th);
            int filasEn = Mathf.Max(1, (enemNivel.Count + 4) / 5);
            float phEn = 66f + filasEn * 46f + 22f;
            _ui.PanelCrema(rx, ry, rw, phEn);
            _ui.Texto(rx + 14f, ry + 8f, rw - 28f, 24f, _run.EsNivelDeJefe ? "¡Nivel de jefe!" : "Enemigos del nivel", 18, _run.EsNivelDeJefe ? new Color(0.75f, 0.2f, 0.3f) : Interfaz.Tinta, TextAnchor.UpperLeft);
            _ui.Texto(rx + 14f, ry + 30f, rw - 28f, 18f, "Tablero " + tw + "x" + th + (_run.EsTutorial ? "" : ". Mira el mapa para ver cómo empiezan."), 10, Suave, TextAnchor.UpperLeft);
            ListaEnemigos(rx + 12f, ry + 54f, rw - 24f, enemNivel, 40f);
            _ui.Texto(rx + 14f, ry + phEn - 22f, rw - 28f, 18f, ResumenDebilidades(enemNivel), 10, Morado, TextAnchor.UpperLeft);
            EnemigoDef jefe = GameData.Enemigo(m.JefeId);
            if (_run.EsNivelDeJefe)
            {
                float jy = ry + phEn + 6f;
                _ui.PanelCrema(rx, jy, rw, 84f);
                _ui.Retrato(rx + 14f, jy + 8f, 66f, Arte.TexEnemigo(jefe.Id), false, true);
                _ui.Texto(rx + 90f, jy + 8f, rw - 100f, 22f, jefe.Nombre, 14, Interfaz.Tinta, TextAnchor.UpperLeft);
                float jx = rx + 90f;
                jx += _ui.Dato(jx, jy + 32f, "corazon", jefe.Vida.ToString(), 12, Interfaz.Tinta, "Vida del jefe");
                jx += _ui.Dato(jx, jy + 32f, "defensa", jefe.Defensa.ToString(), 12, Interfaz.Tinta, "Defensa del jefe");
                if (jefe.Debil != Elemento.Fisico) { _ui.Texto(rx + 90f, jy + 54f, 40f, 18f, "Débil", 11, Morado, TextAnchor.MiddleLeft); _ui.Img(rx + 130f, jy + 53f, 18f, 18f, Arte.IconoClave(Textos.IconoElemento(jefe.Debil))); }
                if (_ayudas) _ui.PonerTip(rx + 14f, jy, rw - 28f, 84f, TextoEnemigo(jefe));
                phEn += 90f;
            }
            float iy2 = ry + phEn + 8f;
            _ui.PanelCrema(rx, iy2, rw, 104f);
            _ui.Texto(rx + 14f, iy2 + 6f, rw - 28f, 24f, "Inventario", 17, Interfaz.Tinta, TextAnchor.UpperLeft);
            int ci = 0;
            foreach (Consumible cs in Enum.GetValues(typeof(Consumible)))
            {
                int cn; _run.Inventario.TryGetValue(cs, out cn);
                float cx2 = rx + 12f + (ci % 4) * 54f, cy2 = iy2 + 32f + (ci / 4) * 34f;
                _ui.Img(cx2, cy2, 28f, 28f, Arte.Icono(IconoCons(cs)), cn > 0 ? Color.white : new Color(1f, 1f, 1f, 0.35f));
                _ui.Texto(cx2 + 28f, cy2 + 3f, 24f, 24f, "x" + cn, 12, Interfaz.Tinta, TextAnchor.MiddleLeft);
                if (_ayudas) _ui.PonerTip(cx2, cy2, 52f, 28f, Textos.Nombre(cs) + " (x" + cn + "): " + Textos.Descripcion(cs));
                ci++;
            }
            if (_run.Efectos.Count > 0)
            {
                float by = iy2 + 112f;
                _ui.PanelCrema(rx, by, rw, 34f + _run.Efectos.Count * 26f);
                _ui.Texto(rx + 14f, by + 5f, rw - 28f, 22f, "Bendiciones y maldiciones", 13, Interfaz.Tinta, TextAnchor.UpperLeft);
                for (int i = 0; i < _run.Efectos.Count; i++)
                {
                    EfectoActivo ef = _run.Efectos[i];
                    float yy = by + 30f + i * 26f;
                    _ui.Chip(rx + 12f, yy, (ef.EsBendicion ? "+ " : "- ") + Textos.Nombre(ef.Tipo) + " (" + ef.Turnos + ")", ef.EsBendicion ? new Color(0.7f, 1f, 0.7f) : new Color(1f, 0.7f, 0.7f), 12);
                    if (_ayudas) _ui.PonerTip(rx + 12f, yy, rw - 24f, 22f, Textos.Nombre(ef.Tipo) + ": " + Textos.Descripcion(ef.Tipo) + "\nDura " + ef.Turnos + " rondas y sigue entre niveles.");
                }
            }

            // ---- tarjetas de los 4 gatos
            float cw = (_ui.VW - 24f - 250f - 3f * 8f) / 4f;
            for (int i = 0; i < _run.Expedicion.Count; i++) TarjetaGato(_run.Expedicion[i], 12f + i * (cw + 8f), cardTop, cw, 216f);
            if (_ui.Boton(_ui.VW - 250f, Interfaz.VH - 118f, 238f, 106f, "¡EMPEZAR\nNIVEL!", "amarillo", true, 26))
            {
                _run.Empezar();
                _modo = Modo.Mover;
            }

            // clic en un gato de la escena (se comprueba al final, cuando ya se conocen las zonas de la interfaz)
            Event e = Event.current;
            if (_run.Fase == FaseRun.Preparacion && !ModalAbierto && e.type == EventType.MouseDown && e.button == 0 && !_ui.SobreInterfaz(_ui.Raton))
            {
                VU v = _esc.UnidadBajoPantalla(new Vector2(_ui.Raton.x * _ui.K, Screen.height - _ui.Raton.y * _ui.K));
                if (v != null && v.U.Gato != null) { _selExp = _selExp == v.U.Gato ? null : v.U.Gato; Sonido.Tocar("clic"); e.Use(); }
            }
        }

        private static Color TinteSlot(EquipoTipo e) { return Objetos.Slot(e) == SlotObjeto.Cuerpo ? new Color(0.65f, 1f, 0.85f) : new Color(1f, 0.82f, 0.6f); }

        private static string TextoObjeto(EquipoTipo e)
        {
            return Textos.Nombre(e) + " [" + Objetos.NombreSlot(Objetos.Slot(e)) + "]: " + Textos.Descripcion(e);
        }

        /// <summary>Globo de un enemigo: datos, ataque en claves y debilidad/resistencia.</summary>
        private string TextoEnemigo(EnemigoDef d)
        {
            var sb = new StringBuilder();
            sb.Append(d.Nombre).Append("\nVida ").Append(d.Vida).Append("  Def ").Append(d.Defensa).Append("  Mov ").Append(d.Movimiento);
            sb.Append("\n").Append(d.Ataque.Nombre).Append(": ");
            var ks = Textos.Claves(d.Ataque);
            for (int i = 0; i < ks.Count; i++) sb.Append(i > 0 ? ", " : "").Append(ks[i].Texto);
            if (d.Debil != Elemento.Fisico) sb.Append("\nDébil: ").Append(Textos.Nombre(d.Debil));
            if (d.Resiste != Elemento.Fisico) sb.Append("   Resiste: ").Append(Textos.Nombre(d.Resiste));
            if (!string.IsNullOrEmpty(d.Nota)) sb.Append("\n").Append(d.Nota);
            return sb.ToString();
        }

        private static string IconoCons(Consumible c)
        {
            switch (c)
            {
                case Consumible.PocionVida: return "pocionVida";
                case Consumible.PocionMana: return "pocionMana";
                case Consumible.PocionVelocidad: return "pocionVel";
                case Consumible.Bomba: return "bomba";
                case Consumible.BombaFuego: return "bombaFuego";
                case Consumible.BombaHielo: return "bombaHielo";
                case Consumible.FrascoVeneno: return "frascoVeneno";
                default: return "antidoto";
            }
        }

        private static string ListaHabilidades(List<HabilidadDef> hs, bool largo)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < hs.Count; i++)
            {
                if (i > 0) sb.Append(largo ? "\n" : " / ");
                sb.Append(hs[i].Nombre).Append(" (").Append(hs[i].CosteMana).Append(")");
                if (largo) sb.Append(": ").Append(hs[i].Descripcion);
            }
            return sb.ToString();
        }

        private string FichaLarga(Gato g)
        {
            Stats s = g.Stats;
            var sb = new StringBuilder();
            sb.Append(g.Nombre).Append("  Vida ").Append(g.Vida).Append('/').Append(g.VidaMax).Append("  Mov ").Append(g.Movimiento);
            sb.Append("\nDef ").Append(s.Defensa).Append("  Maná ").Append(s.Mana).Append("  Vel ").Append(s.Velocidad);
            sb.Append("\nFue ").Append(s.Fuerza).Append("  Int ").Append(s.Inteligencia).Append("  Des ").Append(s.Destreza).Append("  Sue ").Append(s.Suerte);
            if (g.Basico != null) sb.Append("\n").Append(g.Basico.Nombre).Append(" (básico, gratis)");
            foreach (HabilidadDef h in g.Habilidades) sb.Append("\n").Append(h.Nombre).Append(" (").Append(h.CosteMana).Append(")");
            sb.Append("\n").Append(Textos.PasivaNombre(g.Clase)).Append(": ").Append(g.Clase.PasivaTexto);
            return sb.ToString();
        }

        // ================================================================ COMBATE
        private void PantallaCombate()
        {
            Combate c = _run.Combate;
            if (c == null) return;
            Unidad act = c.Activa;
            bool enCurso = c.Estado == EstadoCombate.EnCurso;
            bool ocupado = _esc.Ocupado;
            bool modal = ModalAbierto;
            Event e = Event.current;

            DibujarTextosFlotantes();
            DibujarEtiquetasUnidades(c);

            // ---------- turnos (arriba a la izquierda)
            List<Unidad> orden = c.LineaDeTurnos(9);
            float tw = 20f + orden.Count * 52f;
            _ui.PanelOscuro(10f, 8f, tw, 72f);
            Unidad visual = _esc.ActivoVisual ?? act;
            for (int i = 0; i < orden.Count; i++)
            {
                Unidad u = orden[i];
                bool a = u == visual;
                float sz = a ? 54f : 44f;
                float px = 20f + i * 52f, py = a ? 12f : 20f;
                _ui.Retrato(px, py, sz, Arte.TexUnidad(u), u.EsGato, a);
                if (_ayudas) _ui.PonerTip(px, py, sz, sz, u.Nombre + (a ? "\nJuega ahora" : ""));
            }

            // ---------- info del nivel (arriba a la derecha)
            BarraSuperiorDerecha(true);
            float iw = 330f, ix = _ui.VW - iw - 12f;
            _ui.PanelOscuro(ix, 50f, iw, 90f);
            string titulo = _run.EsTutorial ? "Tutorial  Nivel " + c.NumNivel : "Mundo " + c.NumMundo + "  Nivel " + c.NumNivel + (_run.EsNivelDeJefe ? "  JEFE" : "");
            _ui.Texto(ix + 16f, 54f, iw - 130f, 26f, titulo, 17, Interfaz.Crema, TextAnchor.MiddleLeft, false);
            _ui.Texto(ix + 16f, 80f, iw - 130f, 26f, "Ronda " + c.Ronda + "   Intento " + _run.Intento, 13, new Color(0.85f, 0.8f, 0.95f), TextAnchor.MiddleLeft, false);
            _ui.Corazones(ix + iw - 104f, 64f, 28f, Math.Max(0, _run.Vidas), Config.VidasPorRun);
            if (_ayudas) _ui.PonerTip(ix + iw - 104f, 64f, 94f, 28f, "Vidas de la run");
            _ui.Img(ix + iw - 104f, 96f, 24f, 24f, Arte.Icono("moneda"));
            _ui.Texto(ix + iw - 78f, 96f, 70f, 24f, (_run.Dinero + c.Dinero) + (c.Dinero > 0 ? " (+" + c.Dinero + ")" : ""), 14, Interfaz.Oro, TextAnchor.MiddleLeft, true);
            if (_ayudas) _ui.PonerTip(ix + iw - 104f, 96f, 100f, 24f, "Monedas de la run" + (c.Dinero > 0 ? " + " + c.Dinero + " recogidas en este nivel" : "") + ". Se recogen pasando por encima.");
            if (c.Efectos.Count > 0)
            {
                float ey = 146f;
                foreach (EfectoActivo ef in c.Efectos)
                {
                    string et = (ef.EsBendicion ? "+ " : "- ") + Textos.Nombre(ef.Tipo) + " " + ef.Turnos;
                    float cwid = _ui.Chip(ix + iw - _ui.AnchoTexto(et, 12) - 14f, ey, et, ef.EsBendicion ? new Color(0.7f, 1f, 0.7f) : new Color(1f, 0.7f, 0.7f), 12);
                    if (_ayudas) _ui.PonerTip(ix + iw - cwid, ey, cwid, 20f, Textos.Nombre(ef.Tipo) + ": " + Textos.Descripcion(ef.Tipo));
                    ey += 26f;
                }
            }

            // ---------- registro (opcional, se abre con el botón de arriba)
            if (_verRegistro)
            {
                float rw = 300f, rx = 10f, ry = 88f;
                _ui.PanelCrema(rx, ry, rw, 150f);
                int desde = Math.Max(0, c.Log.Count - 7);
                var sb = new StringBuilder();
                for (int i = desde; i < c.Log.Count; i++) sb.AppendLine(c.Log[i]);
                _ui.Texto(rx + 12f, ry + 8f, rw - 24f, 136f, sb.ToString(), 11, Interfaz.Tinta, TextAnchor.UpperLeft);
            }

            // ---------- tarjeta de unidad (abajo a la izquierda): lo que señalas, lo que inspeccionas o el que juega
            Unidad foco = _hoverU != null ? _hoverU : (_insp != null ? _insp : (act != null && act.Vivo ? act : null));
            if (foco != null && (_hoverU != null || _insp != null || !_casillaInfo.HasValue)) TarjetaUnidad(c, foco, 10f, Interfaz.VH - 210f, 336f, 200f);
            if (_casillaInfo.HasValue && _hoverU == null && _insp == null) TarjetaCasilla(c, _casillaInfo.Value, 10f, Interfaz.VH - 210f, 336f, 200f);

            // ---------- acciones (abajo al centro)
            float ax = 352f;
            if (act != null && act.EsGato && enCurso) BarraAcciones(c, act, ax, Interfaz.VH - 98f, ocupado);
            else if (enCurso)
            {
                _ui.PanelOscuro(ax, Interfaz.VH - 70f, 300f, 54f);
                _ui.Texto(ax, Interfaz.VH - 70f, 300f, 54f, ocupado ? "Turno de los enemigos..." : "Esperando...", 17, Interfaz.Crema, TextAnchor.MiddleCenter, false);
            }

            // ---------- pista de lo que hay que hacer
            if (enCurso && _turnoJugador) DibujarPista(c, act, ax);

            // ---------- final del combate
            if (!enCurso && !ocupado) PanelFin(c);

            // ---------- entrada del jugador
            if (modal) return;
            if (e.type == EventType.KeyDown) Teclas(c, act, e);
            if (e.type == EventType.MouseDown && !_ui.SobreInterfaz(_ui.Raton))
            {
                if (e.button == 1)
                {
                    if (_hoverU != null) { _casillaInfo = null; Inspeccionar(_hoverU); e.Use(); }
                    else if (_modo != Modo.Mover) { _modo = Modo.Mover; e.Use(); }
                    else if (_insp != null) { _insp = null; e.Use(); }
                    else if (_hover.HasValue) { _casillaInfo = _casillaInfo.HasValue && _casillaInfo.Value == _hover.Value ? (Pos?)null : _hover; Sonido.Tocar("clic", 0.7f, 1.1f); e.Use(); }
                    else if (_casillaInfo.HasValue) { _casillaInfo = null; e.Use(); }
                }
                else if (e.button == 0 && _turnoJugador && _hover.HasValue) { e.Use(); ClicTablero(c, act, _hover.Value); }
            }
        }

        private void Inspeccionar(Unidad u)
        {
            _insp = _insp == u ? null : u;
            Sonido.Tocar("clic", 0.7f, 1.1f);
            Tut(TutAccion.Inspeccionar);
        }

        private void DibujarPista(Combate c, Unidad act, float ax)
        {
            string t;
            if (_modo == Modo.Mover) t = c.MovimientoRestante > 0 ? "Elige una casilla azul para moverte  (te quedan " + c.MovimientoRestante + ")" : "Sin movimiento. Ataca o termina el turno";
            else if (_modo == Modo.Habilidad) t = HabSel(act).Nombre + ": elige un objetivo en rojo  (clic derecho: cancelar)";
            else t = Textos.Nombre(_cons) + ": elige una casilla marcada  (clic derecho: cancelar)";
            float w = _ui.AnchoTexto(t, 14) + 40f, x = ax + 300f - w / 2f;
            float pulso = 1f + 0.015f * Mathf.Sin(_tPantalla * 5f);
            _ui.Panel9(x, Interfaz.VH - 192f, w, 32f, Arte.PanelCrema, 14f, new Color(1f, 1f, 1f, 0.95f));
            _ui.Texto(x, Interfaz.VH - 192f, w, 32f, t, Mathf.RoundToInt(14 * pulso), Interfaz.Tinta, TextAnchor.MiddleCenter, false);
        }

        private static bool EsEstadoMalo(EstadoTipo t)
        {
            return t == EstadoTipo.Provocado || t == EstadoTipo.Inmovilizado || t == EstadoTipo.Envenenado || t == EstadoTipo.Ralentizado;
        }

        private static string IconoEstado(EstadoTipo t)
        {
            switch (t)
            {
                case EstadoTipo.Provocado: return "calavera";
                case EstadoTipo.Inmovilizado: return "gota_vacia";
                case EstadoTipo.DefensaMas: return "escudo";
                case EstadoTipo.Envenenado: return "veneno";
                case EstadoTipo.Ralentizado: return "hielo";
                case EstadoTipo.DanoMas: return "fuerza";
                default: return "bota";
            }
        }

        private static string Abreviar(string n, int max) { return n.Length > max ? n.Substring(0, max - 1) + "." : n; }

        /// <summary>Globo corto de una habilidad de la barra de acciones y clic derecho = ficha completa.</summary>
        private void HabilidadExtras(float bx, float by, float bw, float bh, HabilidadDef h, string clase, string porque)
        {
            if (_ayudas) _ui.PonerTipHab(bx, by, bw, bh, h, clase, string.IsNullOrEmpty(porque) ? "Clic derecho: descripción completa" : porque);
            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 1 && !_ui.Congelado && _ui.Sobre(bx, by, bw, bh)) { AbrirFicha(h, clase); e.Use(); }
        }

        private string PorQueNo(Combate c, Unidad act, HabilidadDef h)
        {
            if (c.PuedeUsar(act, h)) return "";
            if (c.ActuoEsteTurno && !h.Movilidad) return "Ya actuaste este turno";
            return act.Mana < h.CosteMana ? "Te falta maná" : "No puedes usarla ahora";
        }

        private void BarraAcciones(Combate c, Unidad act, float x, float y, bool ocupado)
        {
            bool libre = _turnoJugador;
            int nh = act.Habilidades.Count;
            float w = nh >= 4 ? 690f : (nh == 3 ? 640f : 600f);
            _ui.PanelOscuro(x - 6f, y - 8f, w + 12f, 100f);
            float bx = x + 6f, bh = 80f;
            string clase = act.Gato != null ? act.Gato.ClaseId : "luchador";
            int mov = c.MovimientoRestante;
            if (_ui.BotonAccion(bx, y, 78f, bh, Arte.Icono("bota"), "Mover", "Q", "azul", libre, _modo == Modo.Mover, mov > 0 ? mov.ToString() : ""))
                _modo = Modo.Mover;
            if (_ayudas) _ui.PonerTip(bx, y, 78f, bh, "Mover: casillas libres. Barro cuesta 2 y agua 3. Hierba alta: 25% de esquivar. Las monedas se recogen al pisarlas.");
            bx += 86f;

            // ataque básico (A): no gasta maná
            HabilidadDef b = act.Basico;
            if (b != null)
            {
                bool puede = c.PuedeUsar(act, b);
                if (_ui.BotonAccion(bx, y, 78f, bh, Arte.IconoHab(b, clase), Abreviar(b.Nombre, 9), "A", "azul", libre && puede, _modo == Modo.Habilidad && _habIdx < 0, "0", "verde"))
                { _modo = Modo.Habilidad; _habIdx = -1; }
                HabilidadExtras(bx, y, 78f, bh, b, clase, PorQueNo(c, act, b));
                bx += 86f;
            }

            // las habilidades del gato (W, E, y R y S cuando sube de nivel)
            List<HabilidadDef> hs = act.Habilidades;
            string[] teclas = { "W", "E", "R", "S" };
            float hw = hs.Count >= 4 ? 86f : (hs.Count == 3 ? 104f : 124f);
            for (int i = 0; i < hs.Count && i < 4; i++)
            {
                HabilidadDef h = hs[i];
                bool puede = c.PuedeUsar(act, h);
                if (_ui.BotonAccion(bx, y, hw, bh, Arte.IconoHab(h, clase), Abreviar(h.Nombre, hw > 100f ? 15 : 10), teclas[i], "azul", libre && puede, _modo == Modo.Habilidad && _habIdx == i, c.CosteDe(act, h).ToString(), h.Movilidad ? "verde" : "azul"))
                { _modo = Modo.Habilidad; _habIdx = i; }
                HabilidadExtras(bx, y, hw, bh, h, clase, PorQueNo(c, act, h));
                bx += hw + 6f;
            }
            if (hs.Count < 2) bx += (hw + 6f) * (2 - hs.Count);
            if (_ui.Boton(bx, y + 4f, w - (bx - x) - 4f, bh - 8f, "TERMINAR\nTURNO", "amarillo", libre, 15))
                Terminar(c);
            if (_ayudas) _ui.PonerTip(bx, y, w - (bx - x), bh, "Termina el turno de este gato (Espacio)");

            // objetos: siempre las 8 casillas, en el orden de las teclas 1-8
            float cx = x;
            float cy = Interfaz.VH - 152f;
            _ui.PanelOscuro(x - 6f, cy - 6f, 8 * 50f + 12f, 50f);
            int n = 1;
            foreach (Consumible cs in Enum.GetValues(typeof(Consumible)))
            {
                int cant = c.Cantidad(cs);
                bool ok = libre && c.PuedeUsarConsumible(act, cs);
                if (_ui.BotonAccion(cx, cy, 46f, 38f, Arte.Icono(IconoCons(cs)), "", n.ToString(), "azul", ok, _modo == Modo.Consumible && _cons == cs, "x" + cant))
                { _modo = Modo.Consumible; _cons = cs; }
                if (_ayudas) _ui.PonerTip(cx, cy, 46f, 38f, Textos.Nombre(cs) + " (tienes " + cant + ")\n" + Textos.Descripcion(cs) + (Combate.EsLanzable(cs) ? "\nSe lanza a distancia." : ""));
                cx += 50f;
                n++;
            }

            // botones pequeños de ayuda
            float hx = x + 8 * 50f + 14f;
            if (_ui.Boton(hx, cy + 4f, 92f, 30f, "Auto (T)", "gris", libre, 11)) { BotJugador.JugarTurno(c); Aplicar(c); _modo = Modo.Mover; }
            if (_ayudas) _ui.PonerTip(hx, cy + 4f, 92f, 30f, "Un bot juega el turno de este gato (tecla T)");
            if (_ui.Boton(hx + 98f, cy + 4f, 92f, 30f, "Simular nivel", "gris", enCursoSimulable(c), 11)) { BotJugador.JugarCombate(c); c.TomarEventos(); _esc.Saltar(); _modo = Modo.Mover; }
            if (_ayudas) _ui.PonerTip(hx + 98f, cy + 4f, 92f, 30f, "Un bot juega el resto del nivel sin animaciones");
        }

        private static bool enCursoSimulable(Combate c) { return c.Estado == EstadoCombate.EnCurso; }

        private void Terminar(Combate c)
        {
            c.TerminarTurnoJugador();
            Tut(TutAccion.TerminarTurno);
            Aplicar(c);
            _modo = Modo.Mover;
        }

        private void Aplicar(Combate c)
        {
            List<Evento> evs = c.TomarEventos();
            TutEventos(evs);
            _esc.Encolar(evs);
        }

        private void ClicTablero(Combate c, Unidad act, Pos p)
        {
            bool ok;
            Modo antes = _modo;
            if (_modo == Modo.Mover) { ok = c.Mover(p); if (ok) Tut(TutAccion.Mover); }
            else if (_modo == Modo.Habilidad) { ok = c.UsarHabilidad(HabSel(act), p); if (ok) _modo = Modo.Mover; }
            else { ok = c.UsarConsumible(_cons, p); if (ok) _modo = Modo.Mover; }
            if (ok) Aplicar(c);
            else
            {
                Sonido.Tocar("error", 0.7f, 1f);
                _esc.Fx.Texto(antes == Modo.Mover ? "No puedes ir ahí" : "Objetivo no válido", _esc.AMundo(p) + new Vector2(0f, 0.3f), new Color(1f, 0.6f, 0.6f), 18f, 0.8f);
            }
        }

        private void Teclas(Combate c, Unidad act, Event e)
        {
            if (e.keyCode == KeyCode.F) { _velIdx = (_velIdx + 1) % Velocidades.Length; GuardarOpciones(); e.Use(); return; }
            if (!_turnoJugador || act == null) return;
            switch (e.keyCode)
            {
                case KeyCode.Q: _modo = Modo.Mover; e.Use(); break;
                case KeyCode.A:
                    if (act.Basico != null && c.PuedeUsar(act, act.Basico)) { _habIdx = -1; _modo = Modo.Habilidad; }
                    else Sonido.Tocar("error");
                    e.Use();
                    break;
                case KeyCode.W:
                case KeyCode.E:
                case KeyCode.R:
                case KeyCode.S:
                    {
                        int hi = e.keyCode == KeyCode.W ? 0 : (e.keyCode == KeyCode.E ? 1 : (e.keyCode == KeyCode.R ? 2 : 3));
                        if (hi < act.Habilidades.Count && c.PuedeUsar(act, act.Habilidades[hi])) { _habIdx = hi; _modo = Modo.Habilidad; }
                        else Sonido.Tocar("error");
                        e.Use();
                        break;
                    }
                case KeyCode.Escape: _modo = Modo.Mover; _insp = null; _casillaInfo = null; e.Use(); break;
                case KeyCode.Space:
                case KeyCode.Return: Terminar(c); e.Use(); break;
                case KeyCode.T: BotJugador.JugarTurno(c); Aplicar(c); _modo = Modo.Mover; e.Use(); break;
                case KeyCode.Alpha1: case KeyCode.Alpha2: case KeyCode.Alpha3: case KeyCode.Alpha4: case KeyCode.Alpha5: case KeyCode.Alpha6: case KeyCode.Alpha7: case KeyCode.Alpha8:
                    {
                        int i = (int)e.keyCode - (int)KeyCode.Alpha1;
                        var todos = (Consumible[])Enum.GetValues(typeof(Consumible));
                        if (i >= 0 && i < todos.Length && c.PuedeUsarConsumible(act, todos[i])) { _modo = Modo.Consumible; _cons = todos[i]; }
                        else Sonido.Tocar("error");
                        e.Use();
                        break;
                    }
            }
        }

        private void PanelFin(Combate c)
        {
            bool v = c.Estado == EstadoCombate.Victoria;
            float w = 560f, h = 260f, x = (_ui.VW - w) / 2f, y = 190f;
            _ui.PanelCrema(x, y, w, h);
            _ui.Contorno(x, y + 14f, w, 70f, v ? "¡VICTORIA!" : "DERROTA", 56, v ? new Color(0.5f, 0.95f, 0.5f) : new Color(1f, 0.5f, 0.5f), TextAnchor.MiddleCenter);
            string t = v ? "Superaste el nivel en " + c.Ronda + " rondas." : "Cayeron los 4 gatos. Pierdes una vida (te quedarán " + Math.Max(0, _run.Vidas - 1) + ") y el nivel se reinicia.";
            if (v && c.Dinero > 0) t += "\nMonedas recogidas: +" + c.Dinero;
            if (v && _run.EsNivelDeJefe) t += "\n¡Jefe derrotado!";
            _ui.Texto(x + 30f, y + 96f, w - 60f, 70f, t, 16, Interfaz.Tinta, TextAnchor.UpperCenter);
            if (_ui.Boton(x + w / 2f - 130f, y + h - 82f, 260f, 56f, "CONTINUAR", "amarillo", true, 24)) _run.ResolverFinDeCombate();
        }

        // ---------------------------------------------------------------- textos y etiquetas sobre el tablero
        private Vector2 AVirtual(Vector2 mundo)
        {
            Vector2 p = _esc.MundoAPantalla(mundo);
            return new Vector2(p.x / _ui.K, (Screen.height - p.y) / _ui.K);
        }

        private void DibujarTextosFlotantes()
        {
            foreach (TextoFlotante t in _esc.Fx.Textos)
            {
                Vector2 p = AVirtual(t.Mundo);
                float pop = 1f + 0.6f * Mathf.Max(0f, 1f - t.T * 7f);
                float a = Mathf.Clamp01((t.Dur - t.T) / (t.Dur * 0.35f));
                Color c = t.Col; c.a = a;
                _ui.Contorno(p.x - 130f, p.y - 24f, 260f, 48f, t.Txt, Mathf.RoundToInt(t.Tam * pop), c, TextAnchor.MiddleCenter);
            }
        }

        private void DibujarEtiquetasUnidades(Combate c)
        {
            // iconos de estado y vista previa de daño sobre las unidades
            foreach (VU v in _esc.Unidades)
            {
                if (!v.Visible || v.Muerto) continue;
                Unidad u = v.U;
                Vector2 p = AVirtual(_esc.Cabeza(v));
                float x = p.x - u.Estados.Count * 11f + 11f;
                foreach (EstadoActivo es in u.Estados)
                {
                    string ic = IconoEstado(es.Tipo);
                    _ui.Img(x - 11f, p.y - 40f, 22f, 22f, Arte.Icono(ic));
                    x += 22f;
                }
            }
            if (_turnoJugador && c.Activa != null && _hover.HasValue && _objs.Contains(_hover.Value) && _modo == Modo.Habilidad)
            {
                Unidad act = c.Activa;
                HabilidadDef hs = HabSel(act);
                foreach (Pos q in _area)
                {
                    Unidad o = c.UnidadEn(q);
                    if (o == null) continue;
                    VU v = _esc.Vis(o);
                    if (v == null) continue;
                    Vector2 p = AVirtual(_esc.Cabeza(v));
                    if ((hs.Efecto == EfectoHab.Curar || hs.Efecto == EfectoHab.CuraArea) && o.Bando == act.Bando)
                    {
                        _ui.Contorno(p.x - 60f, p.y - 70f, 120f, 30f, "+" + (hs.Poder + act.StatValor(hs.Stat)), 22, new Color(0.5f, 1f, 0.6f), TextAnchor.MiddleCenter);
                    }
                    else if (hs.Poder > 0 && hs.EsAtaque && o.Bando != act.Bando)
                    {
                        int d = c.PrevisualizarDano(act, hs, o);
                        bool mata = d >= o.Vida;
                        bool espalda = c.PorLaEspalda(act, o);
                        Elemento elA = c.ElementoDe(act, hs);
                        bool debil = o.Def != null && elA != Elemento.Fisico && o.Def.Debil == elA;
                        bool resiste = o.Def != null && elA != Elemento.Fisico && o.Def.Resiste == elA;
                        _ui.Contorno(p.x - 70f, p.y - 70f, 140f, 30f, "-" + d + (mata ? " ¡K.O.!" : ""), 22, mata ? new Color(1f, 0.35f, 0.35f) : new Color(1f, 0.85f, 0.4f), TextAnchor.MiddleCenter);
                        string nota = (espalda ? "¡por la espalda!  " : "") + (debil ? "¡débil!  " : "") + (resiste ? "resiste  " : "");
                        if (nota.Length > 0) _ui.Contorno(p.x - 90f, p.y - 94f, 180f, 24f, nota.Trim(), 15, resiste ? new Color(0.7f, 0.8f, 1f) : new Color(1f, 0.9f, 0.5f), TextAnchor.MiddleCenter);
                    }
                }
            }
        }

        // ================================================================ RECOMPENSA
        private void PantallaRecompensa()
        {
            CabeceraRun("");
            BarraSuperiorDerecha(false);
            _ui.Contorno(0, 66f, _ui.VW, 44f, "¡Nivel superado!  Un desafío te espera", 30, Interfaz.Oro, TextAnchor.MiddleCenter);
            if (!string.IsNullOrEmpty(_run.Mensaje)) _ui.Texto(0, 108f, _ui.VW, 26f, _run.Mensaje, 14, Color.white, TextAnchor.MiddleCenter, true);

            // un solo desafío por nivel; cada gato muestra solo si le irá bien (+ verde), mal (- rojo) o regular (nada)
            float cw = Mathf.Min(520f, _ui.VW - 48f);
            float x0 = (_ui.VW - cw) / 2f, y = 142f;
            float h = 214f + _run.Expedicion.Count * 84f + 10f;
            for (int i = 0; i < _run.Ofertas.Count; i++)
            {
                Oferta o = _run.Ofertas[i];
                float x = x0;
                _ui.PanelCrema(x, y, cw, h);
                _ui.Panel9(x + 14f, y + 12f, 64f, 64f, Arte.PanelOscuro, 16f, Color.white);
                _ui.Texto(x + 14f, y + 10f, 64f, 64f, "?", 40, Interfaz.Crema, TextAnchor.MiddleCenter);
                _ui.Texto(x + 88f, y + 12f, cw - 100f, 56f, o.Def.Nombre, 24, Interfaz.Tinta, TextAnchor.UpperLeft);
                _ui.Texto(x + 88f, y + 52f, cw - 100f, 22f, "Un desafío misterioso", 13, Suave, TextAnchor.UpperLeft);
                _ui.Texto(x + 14f, y + 92f, cw - 28f, 44f, "Elige quién lo intenta. + = probablemente sale bien, - = probablemente sale mal, nada = es una apuesta.", 13, new Color(0.45f, 0.3f, 0.55f), TextAnchor.UpperLeft);
                _ui.Texto(x + 14f, y + 150f, cw - 28f, 22f, "¿Quién lo intenta?", 15, new Color(0.45f, 0.3f, 0.55f), TextAnchor.UpperLeft);
                for (int j = 0; j < _run.Expedicion.Count; j++)
                {
                    Gato g = _run.Expedicion[j];
                    double p = Desafios.Probabilidad(g, o), pr = Desafios.ProbRepetir(g);
                    float by = y + 176f + j * 84f;
                    bool click = _ui.Boton(x + 12f, by, cw - 24f, 76f, "", "gris", true, 12);
                    _ui.Retrato(x + 22f, by + 8f, 58f, Arte.TexGato(g.ClaseId), true, false);
                    _ui.Texto(x + 90f, by + 8f, cw - 190f, 24f, g.Nombre, 19, new Color(0.2f, 0.1f, 0.2f), TextAnchor.UpperLeft);
                    _ui.Texto(x + 90f, by + 34f, cw - 190f, 34f, pr > 0 ? "Suerte: puede repetir la tirada" : "", 12, new Color(0.2f, 0.1f, 0.2f), TextAnchor.UpperLeft);
                    if (p >= 0.66) _ui.Contorno(x + cw - 100f, by + 12f, 80f, 50f, "+", 44, new Color(0.25f, 0.85f, 0.35f), TextAnchor.MiddleCenter);
                    else if (p < 0.4) _ui.Contorno(x + cw - 100f, by + 12f, 80f, 50f, "-", 44, new Color(0.95f, 0.3f, 0.3f), TextAnchor.MiddleCenter);
                    if (click)
                    {
                        ResultadoDesafio r = _run.ElegirDesafio(i, g);
                        if (r != null) { _resultadoBien = r.Exito; _resultadoPendiente = true; Sonido.Tocar(r.Exito ? "cura" : "error"); }
                        return;
                    }
                }
            }
            if (_ui.Boton(12f, Interfaz.VH - 44f, 240f, 32f, "Elegir automáticamente (bot)", "gris", true, 12)) BotJugador.ElegirDesafio(_run);
        }

        private static string IconoDesafio(DesafioTipo t)
        {
            switch (t)
            {
                case DesafioTipo.Roca: return "escudo";
                case DesafioTipo.Trampa: return "calavera";
                case DesafioTipo.Glifo: return "estrella";
                case DesafioTipo.Cerradura: return "espada";
                case DesafioTipo.Altar: return "corazon";
                default: return "pocionVida";
            }
        }

        // ================================================================ ELEGIR HABILIDAD (propuesto, por confirmar)
        private static Color ColorCategoria(string cat)
        {
            if (cat == "Ataque") return new Color(1f, 0.72f, 0.65f);
            if (cat == "Movilidad") return new Color(0.7f, 0.85f, 1f);
            return new Color(0.75f, 1f, 0.75f);
        }

        private static string RangoTxt(HabilidadDef h)
        {
            if (h.RangoMax <= 0) return "Sobre sí mismo";
            if (h.RangoMin == h.RangoMax) return "Rango " + h.RangoMax;
            return "Rango " + h.RangoMin + "-" + h.RangoMax;
        }

        private void PantallaElegirHabilidad()
        {
            Gato g = _run.GatoEligiendo;
            if (g == null) return;
            BarraSuperiorDerecha(false);
            PendienteHab pend = _run.Pendientes.Count > 0 ? _run.Pendientes[0] : null;
            int numero = pend != null ? pend.Numero : 1;
            _ui.Contorno(0, 14f, _ui.VW, 56f, "Elige una habilidad", 44, Interfaz.Oro, TextAnchor.MiddleCenter);
            _ui.Texto(0, 70f, _ui.VW, 28f, g.Nombre + " (Nv " + g.Nivel + ")  -  habilidad " + numero + " de " + g.HabilidadesTotales + "   -   elecciones que quedan: " + _run.Pendientes.Count, 17, Color.white, TextAnchor.MiddleCenter, true);

            // ---- panel del gato (izquierda)
            float lx = 12f, ly = 112f, lw = 252f, lh = 440f;
            _ui.PanelCrema(lx, ly, lw, lh);
            _ui.Retrato(lx + 14f, ly + 14f, 96f, Arte.TexGato(g.ClaseId), true, true);
            _ui.Texto(lx + 118f, ly + 14f, lw - 128f, 30f, g.Nombre, 20, Interfaz.Tinta, TextAnchor.UpperLeft);
            Stats s = g.Stats;
            float dx = lx + 118f;
            dx += _ui.Dato(dx, ly + 46f, "corazon", g.VidaMax.ToString(), 12, Interfaz.Tinta, "Vida");
            dx += _ui.Dato(dx, ly + 46f, "defensa", s.Defensa.ToString(), 12, Interfaz.Tinta, "Defensa");
            dx = lx + 118f;
            dx += _ui.Dato(dx, ly + 70f, "gota", s.Mana.ToString(), 12, Interfaz.Tinta, "Maná");
            dx += _ui.Dato(dx, ly + 70f, "bota", g.Movimiento.ToString(), 12, Interfaz.Tinta, "Movimiento por turno");
            _ui.Dato(dx, ly + 70f, "st_vel", s.Velocidad.ToString(), 12, Interfaz.Tinta, "Velocidad: quién juega antes");
            dx = lx + 118f;
            dx += _ui.Dato(dx, ly + 94f, "st_fue", s.Fuerza.ToString(), 12, Interfaz.Tinta, "Fuerza");
            dx += _ui.Dato(dx, ly + 94f, "st_int", s.Inteligencia.ToString(), 12, Interfaz.Tinta, "Inteligencia");
            _ui.Dato(dx, ly + 94f, "st_des", s.Destreza.ToString(), 12, Interfaz.Tinta, "Destreza");
            _ui.Texto(lx + 14f, ly + 122f, lw - 28f, 20f, "Pasiva", 14, Morado, TextAnchor.UpperLeft);
            _ui.Texto(lx + 14f, ly + 142f, lw - 28f, 20f, Textos.PasivaNombre(g.Clase), 15, Interfaz.Tinta, TextAnchor.UpperLeft);
            ClavesEnv(lx + 14f, ly + 166f, lw - 28f, Textos.ClavesPasiva(g.Clase.Pasiva), 12, Interfaz.Tinta, _ayudas, true);
            if (_ayudas) _ui.PonerTip(lx + 14f, ly + 142f, lw - 28f, 50f, g.Clase.PasivaTexto);
            _ui.Texto(lx + 14f, ly + 230f, lw - 28f, 20f, "Ya tiene", 14, Morado, TextAnchor.UpperLeft);
            float hx = lx + 14f;
            if (g.Basico != null) { IconoHabilidadBoton(hx, ly + 256f, 44f, g.Basico, g.ClaseId); hx += 52f; }
            foreach (string id in g.HabilidadIds)
            {
                HabilidadDef hh = g.Clase.Hab(id);
                if (hh == null) continue;
                IconoHabilidadBoton(hx, ly + 256f, 44f, hh, g.ClaseId);
                hx += 52f;
            }
            _ui.Texto(lx + 14f, ly + 308f, lw - 28f, 40f, "Pasa el ratón por una habilidad para ver lo esencial; haz clic para la descripción completa.", 11, Suave, TextAnchor.UpperLeft);

            // ---- las 3 opciones
            float cx0 = lx + lw + 14f, cwTot = _ui.VW - cx0 - 12f, gap = 14f;
            int no = Math.Max(1, _run.OpcionesHab.Count);
            float cw = Mathf.Min(340f, (cwTot - gap * (no - 1)) / no), ch = 340f;
            float xs = cx0 + (cwTot - (cw * no + gap * (no - 1))) / 2f;
            for (int i = 0; i < _run.OpcionesHab.Count; i++)
            {
                HabilidadDef h = _run.OpcionesHab[i];
                float x = xs + i * (cw + gap), y = ly;
                bool sobre = _ui.Sobre(x, y, cw, ch) && !_ui.Congelado;
                _ui.Panel9(x, y, cw, ch, Arte.PanelCrema, 22f, sobre ? new Color(1f, 0.98f, 0.8f) : Color.white);
                _ui.Bloquear(x, y, cw, ch);
                _ui.Img(x + 14f, y + 14f, 76f, 76f, Arte.IconoHab(h, g.ClaseId));
                _ui.Texto(x + 100f, y + 14f, cw - 112f, 56f, h.Nombre, 22, Interfaz.Tinta, TextAnchor.UpperLeft);
                float cxp = x + 100f, cyp = y + 66f;
                cxp += _ui.Chip(cxp, cyp, h.Categoria, ColorCategoria(h.Categoria), 12) + 4f;
                if (h.Elemento != Elemento.Fisico) _ui.Chip(cxp, cyp, Textos.Nombre(h.Elemento), EscenaIso.ColorElem(h.Elemento, Color.white), 12);
                // coste con icono
                _ui.Img(x + 14f, y + 104f, 24f, 24f, Arte.IconoClave(h.CosteMana == 0 ? "gratis" : "mana"));
                _ui.Texto(x + 42f, y + 104f, 120f, 24f, h.CosteMana == 0 ? "Gratis" : h.CosteMana + " de maná", 15, Interfaz.Tinta, TextAnchor.MiddleLeft);
                ClavesEnv(x + 14f, y + 138f, cw - 28f, Textos.Claves(h), 15, Interfaz.Tinta, _ayudas, true);
                if (_ui.Boton(x + 20f, y + ch - 132f, cw - 40f, 38f, "Descripción completa", "azul", true, 14)) AbrirFicha(h, g.ClaseId);
                if (_ui.Boton(x + 20f, y + ch - 86f, cw - 40f, 60f, "ELEGIR", "amarillo", true, 26))
                {
                    _run.ElegirHabilidad(i);
                    Sonido.Tocar("carta", 0.8f, 1.1f);
                    return;
                }
            }

            // ---- repetir las opciones con monedas, y bot
            float by = ly + lh + 12f;
            bool puedeRep = _run.Dinero >= Config.CosteRepetirOpciones;
            if (_ui.Boton(cx0, by, 330f, 50f, "Repetir opciones  (" + Config.CosteRepetirOpciones + " monedas)", puedeRep ? "azul" : "gris", puedeRep, 16))
            {
                _run.RepetirOpciones();
                Sonido.Tocar("moneda", 0.7f, 1f);
                return;
            }
            if (_ayudas) _ui.PonerTip(cx0, by, 330f, 50f, "Vuelve a sortear las 3 opciones a cambio de monedas. Las monedas las recoges en los niveles.");
            _ui.Img(cx0 + 340f, by + 12f, 28f, 28f, Arte.Icono("moneda"));
            _ui.Texto(cx0 + 372f, by + 10f, 100f, 32f, _run.Dinero.ToString(), 22, Interfaz.Oro, TextAnchor.MiddleLeft, true);
            if (_ui.Boton(_ui.VW - 330f, by, 318f, 50f, "Elegir todas automáticamente (bot)", "gris", true, 13)) BotJugador.ElegirHabilidades(_run);
        }

        // ================================================================ TIENDA (propuesto, por confirmar)
        private static string IconoOferta(OfertaTienda o)
        {
            if (o.EsConsumible) return IconoCons(o.Cons);
            if (o.EsBanquete) return "corazon";
            if (o.EsDescanso) return "ob_tienda";
            return "estrella";
        }

        private static Texture IconoOfertaTex(OfertaTienda o)
        {
            if (o.EsEquipo) return Arte.IconoEquipo(o.Equipo);
            return Arte.Icono(IconoOferta(o));
        }

        private void PantallaTienda()
        {
            CabeceraRun("");
            BarraSuperiorDerecha(false);
            _ui.Contorno(0, 66f, _ui.VW, 44f, "Tienda entre niveles", 32, Interfaz.Oro, TextAnchor.MiddleCenter);
            _ui.Texto(0, 110f, _ui.VW, 26f, "Gasta tus monedas antes del siguiente nivel. Lo que no compres se pierde cuando salgas.", 14, Color.white, TextAnchor.MiddleCenter, true);

            int n = Math.Max(1, _run.Tienda.Count);
            float gap = 12f, cw = Mathf.Min(230f, (_ui.VW - 24f - gap * (n - 1)) / n), ch = 330f;
            float x0 = (_ui.VW - (cw * n + gap * (n - 1))) / 2f, y = 150f;
            for (int i = 0; i < _run.Tienda.Count; i++)
            {
                OfertaTienda o = _run.Tienda[i];
                float x = x0 + i * (cw + gap);
                _ui.Panel9(x, y, cw, ch, Arte.PanelCrema, 22f, o.Vendido ? new Color(0.75f, 0.75f, 0.75f) : Color.white);
                _ui.Bloquear(x, y, cw, ch);
                _ui.Img(x + cw / 2f - 36f, y + 14f, 72f, 72f, IconoOfertaTex(o), o.Vendido ? new Color(1f, 1f, 1f, 0.4f) : Color.white);
                _ui.Texto(x + 10f, y + 92f, cw - 20f, 52f, o.Nombre, 18, Interfaz.Tinta, TextAnchor.UpperCenter);
                _ui.Texto(x + 14f, y + 142f, cw - 28f, 96f, o.EsEquipo ? "[" + Objetos.NombreSlot(Objetos.Slot(o.Equipo)) + "] " + o.Descripcion : o.Descripcion, 13, Interfaz.Tinta, TextAnchor.UpperCenter);
                _ui.Img(x + cw / 2f - 46f, y + 242f, 26f, 26f, Arte.Icono("moneda"));
                _ui.Texto(x + cw / 2f - 16f, y + 240f, 80f, 30f, o.Precio.ToString(), 22, Interfaz.Tinta, TextAnchor.MiddleLeft);
                bool puede = !o.Vendido && _run.Dinero >= o.Precio;
                if (_ui.Boton(x + 16f, y + ch - 56f, cw - 32f, 44f, o.Vendido ? "VENDIDO" : "COMPRAR", puede ? "verde" : "gris", puede, 18))
                {
                    if (_run.Comprar(i)) Sonido.Tocar("moneda", 0.8f, 1f);
                    return;
                }
            }

            // ---- el equipo: vida y objetos
            float py = y + ch + 16f;
            _ui.PanelCrema(12f, py, _ui.VW - 24f - 260f, 150f);
            _ui.Texto(26f, py + 8f, 300f, 24f, "Tu equipo", 17, Interfaz.Tinta, TextAnchor.UpperLeft);
            for (int i = 0; i < _run.Expedicion.Count; i++)
            {
                Gato g = _run.Expedicion[i];
                float gx = 26f + i * 168f;
                _ui.Retrato(gx, py + 36f, 56f, Arte.TexGato(g.ClaseId), true, false);
                _ui.Texto(gx + 62f, py + 36f, 100f, 22f, g.Nombre, 14, Interfaz.Tinta, TextAnchor.UpperLeft);
                _ui.Barra(gx + 62f, py + 60f, 92f, 14f, (float)g.Vida / Mathf.Max(1, g.VidaMax), new Color(0.45f, 0.85f, 0.4f));
                _ui.Texto(gx + 62f, py + 59f, 92f, 16f, g.Vida + "/" + g.VidaMax, 11, Color.white, TextAnchor.MiddleCenter, true);
                _ui.PonerTip(gx, py + 36f, 156f, 56f, FichaLarga(g));
            }
            int ci = 0;
            foreach (Consumible cs in Enum.GetValues(typeof(Consumible)))
            {
                int cant; _run.Inventario.TryGetValue(cs, out cant);
                float ix = 26f + ci * 66f;
                _ui.Img(ix, py + 104f, 30f, 30f, Arte.Icono(IconoCons(cs)), cant > 0 ? Color.white : new Color(1f, 1f, 1f, 0.35f));
                _ui.Texto(ix + 30f, py + 108f, 34f, 24f, "x" + cant, 13, Interfaz.Tinta, TextAnchor.MiddleLeft);
                _ui.PonerTip(ix, py + 104f, 62f, 30f, Textos.Nombre(cs) + ": " + Textos.Descripcion(cs));
                ci++;
            }

            if (_ui.Boton(_ui.VW - 260f, py, 248f, 150f, "SEGUIR\nAL NIVEL", "amarillo", true, 28))
            {
                _run.SalirDeTienda();
                Sonido.Tocar("clic");
            }
            if (_ui.Boton(12f, Interfaz.VH - 44f, 240f, 32f, "Comprar automáticamente (bot)", "gris", true, 12)) BotJugador.Comprar(_run);
        }


        private void PantallaFinal()
        {
            bool gano = _run.Fase == FaseRun.CampanaCompletada;
            bool tut = _run.EsTutorial;
            BarraSuperiorDerecha(false);
            string titulo = tut ? "¡TUTORIAL COMPLETADO!" : (gano ? "¡CAMPAÑA COMPLETADA!" : "FIN DE LA RUN");
            _ui.Contorno(0, 20f, _ui.VW, 90f, titulo, 62, gano ? new Color(0.6f, 1f, 0.6f) : new Color(1f, 0.6f, 0.6f), TextAnchor.MiddleCenter);
            _ui.Texto(0, 112f, _ui.VW, 28f, _run.Mensaje, 17, Color.white, TextAnchor.UpperCenter, true);

            float w = Mathf.Min(900f, _ui.VW - 40f), x = (_ui.VW - w) / 2f, y = 160f;
            if (tut)
            {
                _ui.PanelCrema(x, y, w, 200f);
                _ui.Texto(x + 30f, y + 24f, w - 60f, 150f, "Ya conoces los gatos, las habilidades, los elementos, los objetos, la tienda y a los jefes. En una run de verdad empiezas con 4 gatos al azar, tienes 3 vidas y recorres 7 mundos.", 17, Interfaz.Tinta, TextAnchor.UpperCenter);
                if (_ui.Boton(_ui.VW / 2f - 330f, y + 240f, 200f, 56f, "Repetir tutorial", "azul", true, 18)) { EmpezarTutorial(); return; }
                if (_ui.Boton(_ui.VW / 2f - 110f, y + 240f, 220f, 56f, "NUEVA RUN", "amarillo", true, 22)) { _run = null; _menu = Menu.NuevaRun; return; }
                if (_ui.Boton(_ui.VW / 2f + 130f, y + 240f, 200f, 56f, "Título", "gris", true, 18)) { _run = null; _menu = Menu.Principal; }
                return;
            }
            _ui.PanelCrema(x, y, w, 440f);
            _ui.Texto(x + 20f, y + 12f, w - 40f, 24f, "Registro de niveles", 18, Interfaz.Tinta, TextAnchor.UpperLeft);
            var sb = new StringBuilder();
            int ini = Math.Max(0, _run.Historial.Count - 16);
            for (int i = ini; i < _run.Historial.Count; i++)
            {
                RegistroNivel r = _run.Historial[i];
                sb.AppendLine("Mundo " + r.Mundo + "-" + r.Nivel + "   intento " + r.Intento + "   " + r.Resultado + "   rondas " + r.Rondas + "   vidas " + r.VidasRestantes + (string.IsNullOrEmpty(r.Desafio) ? "" : "   " + r.Desafio));
            }
            _ui.Texto(x + 20f, y + 42f, w - 40f, 380f, sb.ToString(), 13, Interfaz.Tinta, TextAnchor.UpperLeft);
            _ui.Texto(x + 20f, y + 410f, w - 40f, 24f, "Semilla de la run: " + _run.Semilla + "  (la misma semilla da los mismos niveles)", 12, Suave, TextAnchor.UpperLeft);
            if (_ui.Boton(_ui.VW / 2f - 230f, Interfaz.VH - 96f, 300f, 66f, "NUEVA RUN", "amarillo", true, 28)) { _run = null; _menu = Menu.Nuevo; return; }
            if (_ui.Boton(_ui.VW / 2f + 90f, Interfaz.VH - 90f, 140f, 54f, "Título", "gris", true, 18)) { _run = null; _menu = Menu.Principal; }
        }
    }
}
