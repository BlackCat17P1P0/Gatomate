// Gatomate - vista. Menú de desarrollador: se abre con la tecla | (o F10). Sirve para probar el juego sin jugarlo entero:
// terminar o perder el nivel al instante, saltar a cualquier mundo y nivel, dar dinero, ir a la tienda, curar, subir de nivel, dar objetos...
using System;
using System.Collections.Generic;
using UnityEngine;
using Gatomate.Core;

namespace Gatomate.View
{
    public sealed partial class GatomateJuego
    {
        private bool _dbg;
        private int _dbgMundo = 1, _dbgNivel = 1;
        private string _dbgMsg = "";
        private float _dbgMsgT;

        private void TeclaDebug(Event e)
        {
            if (e.type != EventType.KeyDown) return;
            if (e.character == '|' || e.keyCode == KeyCode.F10)
            {
                _dbg = !_dbg;
                if (_dbg && _run != null) { _dbgMundo = _run.Mundo; _dbgNivel = _run.Nivel; }
                e.Use();
            }
        }

        private void DbgDi(string t) { _dbgMsg = t; _dbgMsgT = 3f; }

        private bool DbgBoton(float x, float y, float w, string txt, string tono, string tip)
        {
            bool r = _ui.Boton(x, y, w, 30f, txt, tono, true, 12);
            if (!string.IsNullOrEmpty(tip)) _ui.PonerTip(x, y, w, 30f, tip);
            return r;
        }

        private void PantallaDebug()
        {
            if (!_dbg) return;
            _ui.Congelado = false;
            float w = 330f, x = _ui.VW - 340f, y = 50f;
            bool run = _run != null;
            bool combate = run && _run.Fase == FaseRun.Combate && _run.Combate != null && _run.Combate.Estado == EstadoCombate.EnCurso;
            float h = run ? 560f : 110f;
            _ui.Panel9(x, y, w, h, Arte.PanelOscuro, 22f, new Color(0.85f, 0.8f, 1f));
            _ui.Bloquear(x, y, w, h);
            _ui.Texto(x + 16f, y + 8f, w - 90f, 26f, "Menú de desarrollador", 17, Interfaz.Oro, TextAnchor.MiddleLeft);
            if (_ui.Boton(x + w - 74f, y + 8f, 60f, 26f, "Cerrar", "rojo", true, 11)) _dbg = false;
            _ui.Texto(x + 16f, y + 32f, w - 32f, 18f, "Tecla | (o F10) para abrir y cerrar. Solo para pruebas.", 10, new Color(0.8f, 0.75f, 0.9f), TextAnchor.MiddleLeft);
            if (!run)
            {
                _ui.Texto(x + 16f, y + 60f, w - 32f, 40f, "Empieza una partida (o el tutorial) para usar las herramientas.", 12, Interfaz.Crema, TextAnchor.UpperLeft);
                return;
            }
            float cy = y + 56f, c1 = x + 14f, c2 = x + 14f + 150f, bw = 146f;
            _ui.Texto(c1, cy, w - 28f, 18f, "Nivel actual", 12, Interfaz.Oro, TextAnchor.MiddleLeft); cy += 20f;
            if (DbgBoton(c1, cy, bw, "Ganar nivel", "verde", "Gana el nivel ahora mismo: te lleva a la recompensa, como si hubieras ganado (los gatos reciben experiencia)."))
            {
                _run.DebugTerminarNivel();
                if (_run.Combate != null) _run.Combate.TomarEventos();
                _esc.Saltar(); DbgDi("Nivel ganado.");
            }
            if (DbgBoton(c2, cy, bw, "Perder nivel", "rojo", "Pierde el nivel ahora mismo (cuesta una vida)."))
            {
                _run.DebugPerderNivel();
                if (_run.Combate != null) _run.Combate.TomarEventos();
                _esc.Saltar(); DbgDi("Nivel perdido.");
            }
            cy += 36f;

            _ui.Texto(c1, cy, w - 28f, 18f, "Ir a un nivel", 12, Interfaz.Oro, TextAnchor.MiddleLeft); cy += 20f;
            int maxM = GameData.Mundos.Count;
            if (_ui.Boton(c1, cy, 30f, 30f, "<", "gris", true, 14)) _dbgMundo = _dbgMundo <= 1 ? maxM : _dbgMundo - 1;
            _ui.Texto(c1 + 32f, cy, 64f, 30f, "Mundo " + _dbgMundo, 12, Interfaz.Crema, TextAnchor.MiddleCenter);
            if (_ui.Boton(c1 + 98f, cy, 30f, 30f, ">", "gris", true, 14)) _dbgMundo = _dbgMundo >= maxM ? 1 : _dbgMundo + 1;
            if (_ui.Boton(c2 + 6f, cy, 30f, 30f, "<", "gris", true, 14)) _dbgNivel = _dbgNivel <= 1 ? Config.NivelesPorMundo : _dbgNivel - 1;
            _ui.Texto(c2 + 38f, cy, 64f, 30f, "Nivel " + _dbgNivel, 12, Interfaz.Crema, TextAnchor.MiddleCenter);
            if (_ui.Boton(c2 + 104f, cy, 30f, 30f, ">", "gris", true, 14)) _dbgNivel = _dbgNivel >= Config.NivelesPorMundo ? 1 : _dbgNivel + 1;
            cy += 34f;
            if (DbgBoton(c1, cy, w - 28f, "Ir a Mundo " + _dbgMundo + " - Nivel " + _dbgNivel + (_dbgNivel == Config.NivelesPorMundo ? " (jefe)" : ""), "amarillo", "Salta directo a ese nivel (queda en el mapa/campamento)."))
            {
                if (_run.EsTutorial) DbgDi("En el tutorial no se puede saltar.");
                else { _run.DebugIrA(_dbgMundo, _dbgNivel); DbgDi("Ahora estás en " + _dbgMundo + "-" + _dbgNivel + "."); }
            }
            cy += 40f;

            _ui.Texto(c1, cy, w - 28f, 18f, "Pantallas", 12, Interfaz.Oro, TextAnchor.MiddleLeft); cy += 20f;
            if (DbgBoton(c1, cy, bw, "Ir a la tienda", "azul", "Abre la tienda de entre niveles (con todas sus ofertas).")) { _run.DebugIrATienda(); DbgDi("Tienda abierta."); }
            if (DbgBoton(c2, cy, bw, "Ir a recompensa", "azul", "Abre la pantalla de desafíos de recompensa.")) { _run.DebugIrARecompensa(); DbgDi("Recompensa abierta."); }
            cy += 36f;

            _ui.Texto(c1, cy, w - 28f, 18f, "Recursos", 12, Interfaz.Oro, TextAnchor.MiddleLeft); cy += 20f;
            if (DbgBoton(c1, cy, bw, "+10 monedas", "verde", null)) { _run.DebugDinero(10); DbgDi("+10 monedas."); }
            if (DbgBoton(c2, cy, bw, "+100 monedas", "verde", null)) { _run.DebugDinero(100); DbgDi("+100 monedas."); }
            cy += 34f;
            if (DbgBoton(c1, cy, bw, "Todos los consumibles", "verde", "Un consumible de cada tipo.")) { _run.DebugDarConsumibles(); DbgDi("Consumibles añadidos."); }
            if (DbgBoton(c2, cy, bw, "Los 50 objetos", "verde", "Añade un ejemplar de cada uno de los 50 objetos al equipo sin asignar.")) { _run.DebugDarTodosLosObjetos(); DbgDi("Objetos añadidos."); }
            cy += 34f;
            if (DbgBoton(c1, cy, bw, "Objeto al azar", "verde", null)) { var todos = (EquipoTipo[])Enum.GetValues(typeof(EquipoTipo)); EquipoTipo q = todos[UnityEngine.Random.Range(0, todos.Length)]; _run.DebugDarObjeto(q); DbgDi("Objeto: " + Textos.Nombre(q)); }
            if (DbgBoton(c2, cy, bw, "+1 vida de run", "verde", null)) { _run.DebugVidas(1); DbgDi("Vidas: " + _run.Vidas); }
            cy += 40f;

            _ui.Texto(c1, cy, w - 28f, 18f, "Gatos", 12, Interfaz.Oro, TextAnchor.MiddleLeft); cy += 20f;
            if (DbgBoton(c1, cy, bw, "Curar a todos", "verde", "Vida llena para todos los gatos.")) { _run.DebugCurarTodo(); DbgDi("Gatos curados."); }
            if (DbgBoton(c2, cy, bw, "+1 nivel a todos", "verde", "Sube un nivel a todos los gatos (con su habilidad extra en los niveles 3 y 6).")) { _run.DebugSubirNivel(); DbgDi("Nivel +1."); }
            cy += 34f;
            if (DbgBoton(c1, cy, bw, "Nuevo gato (reserva)", "verde", "Añade a la reserva el siguiente gato por conseguir.")) DbgDi(_run.DebugNuevoGato() ? "Gato nuevo en la reserva." : "Ya no quedan gatos por conseguir.");
            cy += 40f;

            if (combate)
            {
                Combate c = _run.Combate;
                _ui.Texto(c1, cy, w - 28f, 18f, "En combate", 12, Interfaz.Oro, TextAnchor.MiddleLeft); cy += 20f;
                if (DbgBoton(c1, cy, bw, "Curar y llenar maná", "verde", "Vida y maná al máximo para los gatos del combate."))
                {
                    foreach (Unidad u in c.Vivos(Bando.Gatos)) { u.Vida = u.VidaMax; u.Mana = u.ManaMax; }
                    DbgDi("Party restaurada.");
                }
                if (DbgBoton(c2, cy, bw, "Matar enemigos", "rojo", "Cae todo el bando enemigo."))
                {
                    c.ForzarVictoria(); _esc.Encolar(c.TomarEventos()); DbgDi("Enemigos derrotados.");
                }
            }
            if (_dbgMsgT > 0f)
            {
                _dbgMsgT -= Time.deltaTime;
                _ui.Texto(x + 14f, y + h - 28f, w - 28f, 22f, _dbgMsg, 12, new Color(0.7f, 1f, 0.7f), TextAnchor.MiddleLeft);
            }
        }
    }
}
