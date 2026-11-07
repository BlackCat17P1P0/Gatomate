// Gatomate - núcleo del juego (sin dependencias de Unity).
// Los 3 tableros fijos del tutorial (propuesto, por confirmar). Cada uno está pensado para enseñar cosas distintas.
using System.Collections.Generic;

namespace Gatomate.Core
{
    public static partial class GeneradorNivel
    {
        private static void P(Tablero t, Terreno ter, params int[] xy)
        {
            for (int i = 0; i + 1 < xy.Length; i += 2) t.PonerTerreno(new Pos(xy[i], xy[i + 1]), ter);
        }

        private static void M(Tablero t, int x, int y, int n) { t.PonerMonedas(new Pos(x, y), n); }

        /// <summary>Tablero y enemigos del nivel (1 a 3) del tutorial.</summary>
        public static Combate CrearTutorial(int nivel, List<Gato> expedicion, List<EfectoActivo> efectos, Dictionary<Consumible, int> inventario)
        {
            MundoDef m = GameData.Mundos[0];
            var tab = new Tablero(Config.TableroAncho, Config.TableroAlto);
            var enemigos = new List<KeyValuePair<string, Pos>>();
            if (nivel <= 1)
            {
                // Movimiento, ataque básico, habilidades, hierba, flores, cajas y monedas.
                P(tab, Terreno.Roca, 4, 0, 4, 1, 4, 8, 4, 9, 6, 2);
                P(tab, Terreno.Caja, 4, 4, 4, 5, 6, 6);
                P(tab, Terreno.HierbaAlta, 2, 6, 2, 7, 3, 6, 3, 7, 2, 2, 3, 2, 7, 6, 7, 7, 8, 7);
                P(tab, Terreno.Flores, 2, 4, 2, 5);
                M(tab, 5, 3, 2); M(tab, 5, 6, 1); M(tab, 3, 1, 1); M(tab, 6, 5, 1);
                enemigos.Add(new KeyValuePair<string, Pos>("maton", new Pos(7, 3)));
                enemigos.Add(new KeyValuePair<string, Pos>("maton", new Pos(7, 5)));
                enemigos.Add(new KeyValuePair<string, Pos>("arquero", new Pos(8, 4)));
            }
            else if (nivel == 2)
            {
                // Elementos: un lago, un sapo dentro, un slime de hielo contra una roca, barril, hierba y un nido.
                P(tab, Terreno.Agua, 4, 3, 5, 3, 4, 4, 5, 4, 4, 5, 5, 5);
                P(tab, Terreno.Barro, 3, 7, 4, 7, 5, 7, 3, 8);
                P(tab, Terreno.HierbaAlta, 2, 2, 2, 3, 3, 2, 3, 3, 6, 7, 7, 7, 6, 8, 7, 8);
                P(tab, Terreno.Roca, 8, 3, 3, 0, 3, 9);
                P(tab, Terreno.Barril, 7, 2);
                P(tab, Terreno.Caja, 8, 5);
                P(tab, Terreno.RocaFragil, 6, 4);
                M(tab, 3, 5, 2); M(tab, 6, 6, 2); M(tab, 2, 8, 1);
                enemigos.Add(new KeyValuePair<string, Pos>("slime_hielo", new Pos(7, 3)));
                enemigos.Add(new KeyValuePair<string, Pos>("sapo", new Pos(5, 4)));
                enemigos.Add(new KeyValuePair<string, Pos>("nido", new Pos(8, 6)));
            }
            else
            {
                // Jefe: el Gato callejero gigante con un bombardero y un ladrón, barriles y monedas.
                P(tab, Terreno.Roca, 5, 2, 5, 7, 3, 0, 3, 9);
                P(tab, Terreno.Caja, 3, 4, 3, 5);
                P(tab, Terreno.Barril, 6, 3, 6, 6);
                P(tab, Terreno.HierbaAlta, 2, 1, 3, 1, 2, 8, 3, 8, 7, 4, 7, 5);
                P(tab, Terreno.Flores, 2, 4, 2, 5);
                M(tab, 4, 4, 2); M(tab, 6, 5, 1); M(tab, 4, 7, 1);
                enemigos.Add(new KeyValuePair<string, Pos>("jefe1", new Pos(8, 4)));
                enemigos.Add(new KeyValuePair<string, Pos>("bombardero", new Pos(7, 2)));
                enemigos.Add(new KeyValuePair<string, Pos>("ladron", new Pos(7, 7)));
            }

            var unidades = new List<Unidad>();
            Pos[] salida = { new Pos(0, 3), new Pos(1, 4), new Pos(0, 5), new Pos(1, 6) };
            for (int i = 0; i < expedicion.Count && i < salida.Length; i++)
            {
                Unidad u = CrearGato(expedicion[i], i + 1);
                u.Pos = salida[i];
                unidades.Add(u);
            }
            int sig = 101;
            foreach (var par in enemigos)
            {
                Unidad e = CrearEnemigo(GameData.Enemigo(par.Key), m, sig++);
                e.Pos = par.Value;
                unidades.Add(e);
            }
            return new Combate(tab, unidades, efectos, inventario, new Azar(Azar.Derivar(7, nivel, 77)), 1, nivel);
        }
    }
}
