// Gatomate - núcleo del juego (sin dependencias de Unity).
// La run de tutorial: 3 niveles fijos. Todas las cifras son propuestas, por confirmar.
using System;
using System.Collections.Generic;

namespace Gatomate.Core
{
    public sealed partial class Run
    {
        public const int NivelesTutorial = 3;
        public const int MonedasMinimasTiendaTutorial = 8;

        /// <summary>Crea la run de tutorial: 8 gatos (4 en la expedición y 4 en la reserva), solo el Luchador elige sus habilidades.</summary>
        public static Run NuevoTutorial()
        {
            Run r = new Run(7, true);
            r.EsTutorial = true;
            r.Dinero = Config.CosteRepetirOpciones;
            string[] exp = { "luchador", "mago", "tanque", "cazador" };
            string[] res = { "picaro", "clerigo", "druida", "alquimista" };
            var habs = new Dictionary<string, string[]>
            {
                { "mago", new[] { "bola", "rayo" } }, { "tanque", new[] { "provocar", "embestida_escudo" } },
                { "cazador", new[] { "disparo", "voltereta" } }, { "picaro", new[] { "punalada", "paso_sombrio" } },
                { "clerigo", new[] { "plegaria", "paso_divino" } }, { "druida", new[] { "enredadera", "viaje_raiz" } },
                { "alquimista", new[] { "frasco", "burbuja" } }
            };
            foreach (string c in exp) r.Expedicion.Add(r.NuevoGato(c));
            foreach (string c in res) r.Reserva.Add(r.NuevoGato(c));
            foreach (Gato g in r.TodosLosGatos)
                if (habs.ContainsKey(g.ClaseId)) g.HabilidadIds.AddRange(habs[g.ClaseId]);
            r.Inventario[Consumible.PocionVida] = 1;
            r.Inventario[Consumible.PocionMana] = 1;
            r.Inventario[Consumible.BombaFuego] = 1;
            r.EquipoSinAsignar.Add(EquipoTipo.CollarFuerza);
            r.PrepararNivel(false);
            r.EncolarHabilidades(r.Expedicion[0]);
            r.IniciarEleccionDeHabilidades(FaseRun.Preparacion);
            return r;
        }

        private void GenerarTiendaTutorial()
        {
            Tienda = new List<OfertaTienda>();
            Dinero = Math.Max(Dinero, MonedasMinimasTiendaTutorial);
            Consumible[] cons = { Consumible.PocionVida, Consumible.BombaFuego, Consumible.PocionMana };
            foreach (Consumible c in cons)
                Tienda.Add(new OfertaTienda { EsConsumible = true, Cons = c, Nombre = Textos.Nombre(c), Descripcion = Textos.Descripcion(c), Precio = Precio(c) });
            EquipoTipo eq = Nivel <= 2 ? EquipoTipo.AnilloFuego : EquipoTipo.GemaFuego;
            Tienda.Add(new OfertaTienda { EsEquipo = true, Equipo = eq, Nombre = Textos.Nombre(eq), Descripcion = Textos.Descripcion(eq), Precio = PrecioEquipo });
            Tienda.Add(new OfertaTienda { EsBanquete = true, Nombre = "Banquete de pescado", Descripcion = "Cura el 50% de la vida máxima a todos los gatos", Precio = PrecioBanquete });
            Tienda.Add(OfertaDescanso());
        }
    }
}
