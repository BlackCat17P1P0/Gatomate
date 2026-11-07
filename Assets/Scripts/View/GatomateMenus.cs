// Gatomate - vista. Pantalla de título con su menú (Juego nuevo, Cargar juego, Opciones, Salir), el submenú de juego nuevo
// (Tutorial o Nueva run), las opciones y el guardado automático de la partida (PlayerPrefs).
using System;
using System.Collections.Generic;
using UnityEngine;
using Gatomate.Core;

namespace Gatomate.View
{
    public sealed partial class GatomateJuego
    {
        private enum Menu { Principal, Nuevo, NuevaRun, Opciones }

        private Menu _menu = Menu.Principal;
        private string _avisoMenu = "";
        private float _avisoT;
        private bool _ayudas = true;                 // globos de ayuda al pasar el ratón
        private bool _confirmaBorrar;

        private const string ClaveGuardado = "gatomate_guardado";
        private const string ClaveResumen = "gatomate_resumen";

        // ---------------------------------------------------------------- opciones y guardado
        private void CargarOpciones()
        {
            try
            {
                Sonido.Silencio = PlayerPrefs.GetInt("gatomate_silencio", 0) == 1;
                _velIdx = Math.Max(0, Math.Min(Velocidades.Length - 1, PlayerPrefs.GetInt("gatomate_vel", 0)));
                _ayudas = PlayerPrefs.GetInt("gatomate_ayudas", 1) == 1;
            }
            catch (Exception) { }
        }

        private void GuardarOpciones()
        {
            try
            {
                PlayerPrefs.SetInt("gatomate_silencio", Sonido.Silencio ? 1 : 0);
                PlayerPrefs.SetInt("gatomate_vel", _velIdx);
                PlayerPrefs.SetInt("gatomate_ayudas", _ayudas ? 1 : 0);
                PlayerPrefs.Save();
            }
            catch (Exception) { }
        }

        private bool HayGuardado()
        {
            try { return PlayerPrefs.HasKey(ClaveGuardado) && !string.IsNullOrEmpty(PlayerPrefs.GetString(ClaveGuardado, "")); }
            catch (Exception) { return false; }
        }

        /// <summary>Guarda la run en la preparación de cada nivel (y borra el guardado si la run terminó).</summary>
        private void AutoGuardar()
        {
            if (_run == null || _run.EsTutorial) return;
            try
            {
                if (_run.Fase == FaseRun.RunPerdida || _run.Fase == FaseRun.CampanaCompletada)
                {
                    PlayerPrefs.DeleteKey(ClaveGuardado); PlayerPrefs.DeleteKey(ClaveResumen); PlayerPrefs.Save();
                    return;
                }
                string txt = _run.Serializar();
                if (txt == null) return;
                PlayerPrefs.SetString(ClaveGuardado, txt);
                PlayerPrefs.SetString(ClaveResumen, "Mundo " + _run.Mundo + "  Nivel " + _run.Nivel + "  -  " + _run.Vidas + " vidas  -  " + _run.Dinero + " monedas");
                PlayerPrefs.Save();
            }
            catch (Exception) { }
        }

        private void CargarPartida()
        {
            Run r = null;
            try { r = Run.Cargar(PlayerPrefs.GetString(ClaveGuardado, "")); } catch (Exception) { }
            if (r == null) { Aviso("El guardado está dañado o es de otra versión."); return; }
            _run = r;
            _tutDesactivado = true;
            _selExp = null; _selEquipo = -1; _modo = Modo.Mover; _insp = null;
            Sonido.Tocar("victoria", 0.5f, 1.2f);
        }

        private void Aviso(string t) { _avisoMenu = t; _avisoT = 4f; }

        private void EmpezarTutorial()
        {
            Config.MundosJugables = 7;
            _run = Run.NuevoTutorial();
            _tutDesactivado = false; _tutClave = ""; _tutIdx = 0; _tutHechas.Clear();
            _selExp = null; _selEquipo = -1; _modo = Modo.Mover; _insp = null;
            Sonido.Tocar("victoria", 0.5f, 1.2f);
        }

        private void Salir()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ---------------------------------------------------------------- pantalla de título
        private void PantallaTitulo()
        {
            if (_avisoT > 0f) _avisoT -= Time.deltaTime;
            float cx = _ui.VW / 2f;
            float pulso = 1f + 0.03f * Mathf.Sin(_tPantalla * 2.5f);
            _ui.Contorno(0, 36f + 4f * Mathf.Sin(_tPantalla * 2f), _ui.VW, 110f, "GATOMATE", Mathf.RoundToInt(96f * pulso), Interfaz.Oro, TextAnchor.MiddleCenter);
            _ui.Contorno(0, 140f, _ui.VW, 36f, "roguelike táctico de gatos  -  prototipo", 22, new Color(1f, 0.95f, 0.85f), TextAnchor.MiddleCenter);

            switch (_menu)
            {
                case Menu.Principal: MenuPrincipal(cx); break;
                case Menu.Nuevo: MenuNuevo(cx); break;
                case Menu.NuevaRun: MenuNuevaRun(cx); break;
                default: MenuOpciones(cx); break;
            }
            if (_avisoT > 0f)
            {
                float w = _ui.AnchoTexto(_avisoMenu, 16) + 40f;
                _ui.PanelRosa(cx - w / 2f, VH() - 90f, w, 40f);
                _ui.Texto(cx - w / 2f, VH() - 90f, w, 40f, _avisoMenu, 16, Interfaz.Tinta, TextAnchor.MiddleCenter);
            }
            _ui.Texto(0, VH() - 28f, _ui.VW, 22f, "Todo el arte y el sonido se generan por código  -  M: sonido", 12, new Color(1f, 1f, 1f, 0.7f), TextAnchor.MiddleCenter, true);
        }

        private void MenuPrincipal(float cx)
        {
            float w = 340f, h = 66f, x = cx - w / 2f, y = 300f, gap = 14f;
            bool hay = HayGuardado();
            if (_ui.Boton(x, y, w, h, "JUEGO NUEVO", "amarillo", true, 28)) { _menu = Menu.Nuevo; }
            if (_ui.Boton(x, y + (h + gap), w, h, "Cargar juego", hay ? "verde" : "gris", hay, 24))
                CargarPartida();
            if (hay)
            {
                string r = "";
                try { r = PlayerPrefs.GetString(ClaveResumen, ""); } catch (Exception) { }
                _ui.PonerTip(x, y + (h + gap), w, h, r.Length > 0 ? "Partida guardada: " + r : "Partida guardada");
            }
            else _ui.PonerTip(x, y + (h + gap), w, h, "Todavía no hay ninguna partida guardada. Se guarda sola al empezar cada nivel de una run.");
            if (_ui.Boton(x, y + 2f * (h + gap), w, h, "Opciones", "azul", true, 24)) _menu = Menu.Opciones;
            if (_ui.Boton(x, y + 3f * (h + gap), w, h, "Salir", "rojo", true, 24)) Salir();
            BarraSuperiorDerecha(false, false);
        }

        private void MenuNuevo(float cx)
        {
            float w = 560f, x = cx - w / 2f, y = 250f;
            _ui.PanelCrema(x, y, w, 330f);
            _ui.Contorno(x, y + 6f, w, 44f, "Juego nuevo", 32, Interfaz.Oro, TextAnchor.MiddleCenter);
            if (_ui.Boton(x + 30f, y + 62f, w - 60f, 84f, "TUTORIAL", "verde", true, 30)) EmpezarTutorial();
            _ui.Texto(x + 30f, y + 150f, w - 60f, 40f, "3 niveles guiados que explican gatos, habilidades, elementos, objetos y tienda. Recomendado la primera vez.", 13, Interfaz.Tinta, TextAnchor.UpperCenter);
            if (_ui.Boton(x + 30f, y + 196f, w - 60f, 84f, "NUEVA RUN", "amarillo", true, 30)) _menu = Menu.NuevaRun;
            _ui.Texto(x + 30f, y + 284f, w - 60f, 40f, "La campaña de verdad: 4 gatos al azar, 7 mundos, 3 vidas.", 13, Interfaz.Tinta, TextAnchor.UpperCenter);
            if (_ui.Boton(cx - 90f, y + 342f, 180f, 46f, "Volver", "gris", true, 18)) _menu = Menu.Principal;
        }

        private void MenuNuevaRun(float cx)
        {
            float w = 540f, h = 196f, x = cx - w / 2f, y = 300f;
            _ui.PanelCrema(x, y, w, h);
            _ui.Texto(x + 24f, y + 18f, 130f, 28f, "Semilla", 17, Interfaz.Tinta, TextAnchor.MiddleLeft);
            _semillaTxt = _ui.Campo(x + 150f, y + 16f, 150f, 32f, _semillaTxt, 9, 17);
            if (_ui.Boton(x + 312f, y + 16f, 100f, 32f, "Al azar", "gris", true, 14)) _semillaTxt = (Environment.TickCount & 0xFFFFF).ToString();
            _ui.PonerTip(x + 24f, y + 16f, 390f, 32f, "La misma semilla da los mismos niveles.");
            _ui.Texto(x + 24f, y + 62f, 130f, 28f, "Mundos", 17, Interfaz.Tinta, TextAnchor.MiddleLeft);
            int[] opc = { 1, 3, 7 };
            for (int i = 0; i < opc.Length; i++)
                if (_ui.Boton(x + 150f + i * 124f, y + 58f, 116f, 34f, opc[i] == 1 ? "1 (corto)" : (opc[i] == 3 ? "3" : "7 (todo)"), _mundosElegidos == opc[i] ? "verde" : "gris", true, 14)) _mundosElegidos = opc[i];
            _ui.Texto(x + 24f, y + 106f, 130f, 28f, "Clases", 17, Interfaz.Tinta, TextAnchor.MiddleLeft);
            if (_ui.Boton(x + 150f, y + 102f, 170f, 34f, "Las 8 clases", !_cuatroClases ? "verde" : "gris", true, 14)) _cuatroClases = false;
            if (_ui.Boton(x + 328f, y + 102f, 190f, 34f, "4 clases (prueba)", _cuatroClases ? "verde" : "gris", true, 14)) _cuatroClases = true;
            _ui.Texto(x + 24f, y + 146f, w - 48f, 44f, "Cada gato elige 2 habilidades. Jefe al final de cada mundo y tienda entre niveles. Con 1 mundo pruebas el ciclo completo en pocos minutos.", 12, new Color(0.4f, 0.3f, 0.5f), TextAnchor.UpperLeft);
            if (_ui.Boton(cx - 150f, y + h + 14f, 300f, 62f, "¡EMPEZAR!", "amarillo", true, 28)) { _tutDesactivado = true; NuevaRun(); }
            if (_ui.Boton(cx - 90f, y + h + 88f, 180f, 42f, "Volver", "gris", true, 16)) _menu = Menu.Nuevo;
        }

        private void MenuOpciones(float cx)
        {
            float w = 520f, h = 330f, x = cx - w / 2f, y = 250f;
            _ui.PanelCrema(x, y, w, h);
            _ui.Contorno(x, y + 6f, w, 44f, "Opciones", 32, Interfaz.Oro, TextAnchor.MiddleCenter);
            _ui.Texto(x + 30f, y + 70f, 200f, 34f, "Sonido y música", 17, Interfaz.Tinta, TextAnchor.MiddleLeft);
            if (_ui.Boton(x + 280f, y + 66f, 200f, 40f, Sonido.Silencio ? "Apagado" : "Encendido", Sonido.Silencio ? "gris" : "verde", true, 16)) { Sonido.Silencio = !Sonido.Silencio; GuardarOpciones(); }
            _ui.Texto(x + 30f, y + 120f, 240f, 34f, "Velocidad de animaciones", 17, Interfaz.Tinta, TextAnchor.MiddleLeft);
            string[] vn = { "x1", "x2", "x3.5" };
            for (int i = 0; i < vn.Length; i++)
                if (_ui.Boton(x + 280f + i * 68f, y + 116f, 64f, 40f, vn[i], _velIdx == i ? "verde" : "gris", true, 16)) { _velIdx = i; GuardarOpciones(); }
            _ui.Texto(x + 30f, y + 170f, 240f, 34f, "Globos de ayuda", 17, Interfaz.Tinta, TextAnchor.MiddleLeft);
            if (_ui.Boton(x + 280f, y + 166f, 200f, 40f, _ayudas ? "Activados" : "Desactivados", _ayudas ? "verde" : "gris", true, 16)) { _ayudas = !_ayudas; GuardarOpciones(); }
            bool hay = HayGuardado();
            _ui.Texto(x + 30f, y + 220f, 240f, 34f, "Partida guardada", 17, Interfaz.Tinta, TextAnchor.MiddleLeft);
            if (_ui.Boton(x + 280f, y + 216f, 200f, 40f, _confirmaBorrar ? "¿Seguro? Borrar" : "Borrar guardado", _confirmaBorrar ? "rojo" : "gris", hay, 15))
            {
                if (_confirmaBorrar)
                {
                    try { PlayerPrefs.DeleteKey(ClaveGuardado); PlayerPrefs.DeleteKey(ClaveResumen); PlayerPrefs.Save(); } catch (Exception) { }
                    _confirmaBorrar = false; Aviso("Guardado borrado.");
                }
                else _confirmaBorrar = true;
            }
            if (_ui.Boton(cx - 90f, y + h - 56f, 180f, 44f, "Volver", "gris", true, 18)) { _menu = Menu.Principal; _confirmaBorrar = false; }
            _ui.Texto(x, y + h + 8f, w, 40f, "Teclas del combate: clic = actuar, clic derecho en un enemigo = inspeccionar, Q mover, A ataque básico, W/E habilidades, 1-8 objetos, Espacio terminar turno.", 12, Color.white, TextAnchor.UpperCenter, true);
        }
    }
}
