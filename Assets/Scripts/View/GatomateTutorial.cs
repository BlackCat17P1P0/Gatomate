// Gatomate - vista. El tutorial en pantalla: una tarjeta (o una ventana, fuera del combate) con los pasos de Core/Tutorial.cs.
// Algunos pasos esperan a que el jugador haga algo (moverse, atacar, clic derecho...). Siempre se puede saltar el paso o todo el tutorial.
using System;
using System.Collections.Generic;
using UnityEngine;
using Gatomate.Core;

namespace Gatomate.View
{
    public sealed partial class GatomateJuego
    {
        private string _tutClave = "";
        private int _tutIdx;
        private readonly HashSet<TutAccion> _tutHechas = new HashSet<TutAccion>();
        private bool _tutDesactivado = true;     // true = sin tutorial (las runs normales y las partidas cargadas)
        private bool _tutOculto;

        /// <summary>Anota que el jugador hizo algo que el tutorial puede estar esperando.</summary>
        private void Tut(TutAccion a) { if (_run != null && _run.EsTutorial) _tutHechas.Add(a); }

        /// <summary>Mira los eventos del combate y anota lo que el tutorial quiere ver (romper cajas, recoger monedas, empujar...).</summary>
        private void TutEventos(List<Evento> evs)
        {
            if (_run == null || !_run.EsTutorial || evs == null) return;
            foreach (Evento e in evs)
            {
                switch (e.Tipo)
                {
                    case EvTipo.Romper: Tut(TutAccion.Romper); break;
                    case EvTipo.Recoger: Tut(TutAccion.Moneda); break;
                    case EvTipo.Empuje: case EvTipo.Choque: Tut(TutAccion.Empujar); break;
                    case EvTipo.Consumible: Tut(TutAccion.Consumible); break;
                    case EvTipo.Dano:
                        if (e.Elem != Elemento.Fisico && e.U != null && e.U.EsGato) Tut(TutAccion.Elemental);
                        break;
                    case EvTipo.Habilidad:
                        if (e.U != null && e.U.EsGato) Tut(e.Hab != null && e.Hab.Basico ? TutAccion.Basico : TutAccion.Habilidad);
                        break;
                }
            }
        }

        private string PantallaTut()
        {
            switch (Actual)
            {
                case Pantalla.ElegirHabilidad: return "hab";
                case Pantalla.Preparacion: return "prep";
                case Pantalla.Combate: return "combate";
                case Pantalla.Recompensa: return "recompensa";
                case Pantalla.Tienda: return "tienda";
                case Pantalla.Final: return "fin";
                default: return "";
            }
        }

        /// <summary>El paso actual, o null si el tutorial no está activo o ya se acabaron los pasos de este momento.</summary>
        private PasoTut PasoActual()
        {
            if (_run == null || !_run.EsTutorial || _tutDesactivado) return null;
            string pan = PantallaTut();
            if (pan.Length == 0) return null;
            string clave = pan == "fin" ? "fin" : Tutorial.Clave(_run, pan);
            if (clave != _tutClave) { _tutClave = clave; _tutIdx = 0; _tutHechas.Clear(); _tutOculto = false; }
            List<PasoTut> pasos = Tutorial.Pasos(clave);
            while (_tutIdx < pasos.Count && pasos[_tutIdx].Espera != TutAccion.Ninguna && _tutHechas.Contains(pasos[_tutIdx].Espera)) _tutIdx++;
            return _tutIdx < pasos.Count ? pasos[_tutIdx] : null;
        }

        /// <summary>true si el paso actual es una ventana que tapa la pantalla (explicaciones sin acción, fuera del combate).</summary>
        private bool TutorialEsVentana()
        {
            PasoTut p = PasoActual();
            if (p == null) return false;
            return Actual != Pantalla.Combate && p.Espera == TutAccion.Ninguna && !(_tutOculto);
        }

        private void DibujarTutorial()
        {
            PasoTut p = PasoActual();
            if (p == null) return;
            List<PasoTut> pasos = Tutorial.Pasos(_tutClave);
            bool ventana = TutorialEsVentana();
            float w = Mathf.Min(580f, _ui.VW - 40f);
            string cuenta = (_tutIdx + 1) + "/" + pasos.Count;
            float alto = _ui.AltoTexto(p.Texto, 14, w - 40f) + 104f;

            if (ventana)
            {
                _ui.Rellenar(new Rect(0, 0, Screen.width, Screen.height), new Color(0.05f, 0.02f, 0.1f, 0.55f));
                float x = (_ui.VW - w) / 2f, y = (Interfaz.VH - alto) / 2f;
                _ui.PanelCrema(x, y, w, alto);
                _ui.Bloquear(0, 0, _ui.VW, Interfaz.VH);
                _ui.Texto(x + 20f, y + 12f, w - 120f, 30f, p.Titulo, 22, Interfaz.Tinta, TextAnchor.MiddleLeft);
                _ui.Texto(x + w - 90f, y + 12f, 70f, 30f, "Tutorial " + cuenta, 11, new Color(0.45f, 0.35f, 0.55f), TextAnchor.MiddleRight);
                _ui.Texto(x + 20f, y + 48f, w - 40f, alto - 100f, p.Texto, 14, Interfaz.Tinta, TextAnchor.UpperLeft);
                if (_ui.Boton(x + w - 190f, y + alto - 50f, 170f, 38f, _tutIdx + 1 >= pasos.Count ? "¡Entendido!" : "Siguiente  >", "amarillo", true, 16)) _tutIdx++;
                if (_ui.Boton(x + 20f, y + alto - 46f, 160f, 30f, "Saltar tutorial", "gris", true, 12)) _tutDesactivado = true;
                return;
            }

            // tarjeta que no tapa el juego
            float cy = Actual == Pantalla.Combate ? 94f : 118f;
            if (_tutOculto)
            {
                if (_ui.Boton((_ui.VW - 190f) / 2f, cy, 190f, 30f, "Tutorial " + cuenta + "  (ver)", "verde", true, 13)) _tutOculto = false;
                return;
            }
            float cx = (_ui.VW - w) / 2f;
            _ui.PanelCrema(cx, cy, w, alto - 6f);
            _ui.Texto(cx + 16f, cy + 8f, w - 130f, 28f, p.Titulo, 19, Interfaz.Tinta, TextAnchor.MiddleLeft);
            _ui.Texto(cx + w - 108f, cy + 8f, 92f, 28f, "Tutorial " + cuenta, 11, new Color(0.45f, 0.35f, 0.55f), TextAnchor.MiddleRight);
            _ui.Texto(cx + 16f, cy + 38f, w - 32f, alto - 90f, p.Texto, 14, Interfaz.Tinta, TextAnchor.UpperLeft);
            float by = cy + alto - 50f;
            if (p.Espera == TutAccion.Ninguna)
            {
                if (_ui.Boton(cx + w - 170f, by, 154f, 34f, _tutIdx + 1 >= pasos.Count ? "¡Entendido!" : "Siguiente  >", "amarillo", true, 15)) _tutIdx++;
            }
            else
            {
                _ui.Texto(cx + 16f, by, w - 260f, 34f, "Hazlo para continuar...", 13, new Color(0.7f, 0.25f, 0.4f), TextAnchor.MiddleLeft);
                if (_ui.Boton(cx + w - 170f, by, 154f, 34f, "Saltar paso", "gris", true, 14)) _tutIdx++;
            }
            if (_ui.Boton(cx + w - 280f, by, 100f, 34f, "Ocultar", "azul", true, 13)) _tutOculto = true;
            if (_ui.Boton(cx + 16f, by, 130f, 34f, "Salir del tutorial", "gris", p.Espera == TutAccion.Ninguna, 11)) _tutDesactivado = true;
        }
    }
}
