// Gatomate - núcleo del juego (sin dependencias de Unity).
// Herramientas del menú de desarrollador (tecla |): saltar niveles, dar dinero, ir a la tienda... solo para probar el juego.
using System;
using System.Collections.Generic;

namespace Gatomate.Core
{
    public sealed partial class Run
    {
        /// <summary>Marca la run como "tramposa" en cuanto se usa una herramienta de desarrollador (no cambia las reglas, solo se muestra).</summary>
        public bool UsoDebug;

        private void LimpiarTransitorios()
        {
            Combate = null;
            Ofertas = new List<Oferta>();
            Tienda = new List<OfertaTienda>();
            OpcionesHab = new List<HabilidadDef>();
        }

        /// <summary>Termina el nivel actual como victoria (si hay combate, lo gana; si no, lo juega y lo gana al instante) y sigue el flujo normal (recompensa, tienda...).</summary>
        public void DebugTerminarNivel()
        {
            UsoDebug = true;
            if (Fase == FaseRun.Preparacion) Empezar();
            if (Fase != FaseRun.Combate || Combate == null) return;
            Combate.ForzarVictoria();
            ResolverFinDeCombate();
        }

        /// <summary>Pierde el nivel actual al instante (cuesta una vida, como una derrota normal).</summary>
        public void DebugPerderNivel()
        {
            UsoDebug = true;
            if (Fase == FaseRun.Preparacion) Empezar();
            if (Fase != FaseRun.Combate || Combate == null) return;
            foreach (Unidad u in new List<Unidad>(Combate.Vivos(Bando.Gatos))) Combate.Matar(u);
            ResolverFinDeCombate();
        }

        /// <summary>Salta directamente a un nivel de un mundo (queda en la preparación de ese nivel).</summary>
        public void DebugIrA(int mundo, int nivel)
        {
            UsoDebug = true;
            Mundo = Math.Max(1, Math.Min(GameData.Mundos.Count, mundo));
            Nivel = Math.Max(1, Math.Min(Config.NivelesPorMundo, nivel));
            Intento = 1;
            LimpiarTransitorios();
            Pendientes.Clear();
            PrepararNivel(false);
        }

        public void DebugDinero(int n) { UsoDebug = true; Dinero = Math.Max(0, Dinero + n); }

        public void DebugIrATienda()
        {
            UsoDebug = true;
            LimpiarTransitorios();
            Pendientes.Clear();
            GenerarTienda();
            Fase = FaseRun.Tienda;
        }

        public void DebugIrARecompensa()
        {
            UsoDebug = true;
            LimpiarTransitorios();
            Pendientes.Clear();
            Ofertas = Desafios.Ofrecer(Azar, Mundo);
            Fase = FaseRun.Recompensa;
        }

        public void DebugCurarTodo() { UsoDebug = true; foreach (Gato g in TodosLosGatos) g.Vida = g.VidaMax; }

        public void DebugVidas(int n) { UsoDebug = true; Vidas = Math.Max(1, Math.Min(9, Vidas + n)); }

        /// <summary>Sube un nivel a todos los gatos (con su habilidad extra pendiente en los niveles 3 y 6).</summary>
        public void DebugSubirNivel()
        {
            UsoDebug = true;
            foreach (Gato g in TodosLosGatos)
            {
                if (g.Nivel >= Config.NivelMaximo) continue;
                Progreso.GanarXp(g, Progreso.XpParaSubir(g.Nivel) - g.Xp);
                g.Vida = g.VidaMax;
                EncolarHabilidades(g);
            }
            if (Pendientes.Count > 0 && Fase == FaseRun.Preparacion) IniciarEleccionDeHabilidades(FaseRun.Preparacion);
        }

        public void DebugDarObjeto(EquipoTipo e) { UsoDebug = true; EquipoSinAsignar.Add(e); }

        /// <summary>Un objeto de cada tipo: sirve para probar todas las sinergias.</summary>
        public void DebugDarTodosLosObjetos()
        {
            UsoDebug = true;
            foreach (EquipoTipo e in Enum.GetValues(typeof(EquipoTipo))) if (!EquipoSinAsignar.Contains(e)) EquipoSinAsignar.Add(e);
        }

        public void DebugDarConsumibles() { UsoDebug = true; foreach (Consumible c in Enum.GetValues(typeof(Consumible))) AgregarConsumible(c); }

        /// <summary>Añade a la reserva el siguiente gato por conseguir (con el nivel medio de la party). Devuelve false si ya no quedan.</summary>
        public bool DebugNuevoGato()
        {
            UsoDebug = true;
            if (_clasesPendientes.Count == 0) return false;
            string id = _clasesPendientes[0];
            _clasesPendientes.RemoveAt(0);
            Gato g = NuevoGato(id);
            Progreso.FijarNivel(g, NivelNuevoGato());
            Reserva.Add(g);
            EncolarHabilidades(g);
            if (Pendientes.Count > 0 && Fase == FaseRun.Preparacion) IniciarEleccionDeHabilidades(FaseRun.Preparacion);
            return true;
        }
    }
}
