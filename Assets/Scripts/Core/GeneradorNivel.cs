// Gatomate - núcleo del juego (sin dependencias de Unity).
// Genera el tablero procedural y coloca gatos y enemigos a partir de una semilla.
using System;
using System.Collections.Generic;

namespace Gatomate.Core
{
    public static partial class GeneradorNivel
    {
        public static Unidad CrearGato(Gato g, int id)
        {
            Stats s = g.Stats;
            var u = new Unidad
            {
                Id = id,
                Nombre = g.Nombre,
                Bando = Bando.Gatos,
                VidaMax = s.Vida,
                Vida = Math.Max(1, Math.Min(g.Vida, s.Vida)),
                Defensa = s.Defensa,
                ManaMax = s.Mana,
                Mana = s.Mana,
                Velocidad = s.Velocidad,
                Fuerza = s.Fuerza,
                Inteligencia = s.Inteligencia,
                Destreza = s.Destreza,
                Suerte = s.Suerte,
                Pasiva = g.Clase.Pasiva,
                Gato = g,
                ColorHex = g.Clase.ColorHex,
                Dir = new Pos(1, 0)
            };
            u.MovimientoBase = g.Movimiento;
            u.Basico = g.Basico;
            u.Equipo.AddRange(g.Equipo);
            u.Habilidades.AddRange(g.Habilidades);
            u.Habilidad = u.Habilidades.Count > 0 ? u.Habilidades[0] : null;
            return u;
        }

        /// <summary>El escalado por mundo solo afecta a los enemigos comunes (no a jefes ni a los invocados: lodos, ratas).</summary>
        public static Unidad CrearEnemigo(EnemigoDef def, MundoDef mundo, int id)
        {
            bool escala = !def.EsJefe && !def.Invocado;
            int bv = escala ? mundo.BonusVida : 0;
            int bp = escala ? mundo.BonusPoder : 0;
            HabilidadDef h = def.Ataque.Copia();
            if (h.Poder > 0) h.Poder += bp;
            var u = new Unidad
            {
                Id = id,
                Nombre = def.Nombre,
                Bando = Bando.Enemigos,
                VidaMax = def.Vida + bv,
                Vida = def.Vida + bv,
                Defensa = def.Defensa,
                MovimientoBase = def.Movimiento,
                Velocidad = def.Velocidad,
                Habilidad = h,
                Pasiva = Pasiva.Ninguna,
                Def = def,
                ColorHex = def.ColorHex,
                Dir = new Pos(-1, 0)
            };
            return u;
        }

        /// <summary>Todo lo que decide la semilla de un nivel: tamaño del tablero, terreno, dónde empiezan los gatos y qué enemigos hay.
        /// El mapa del mundo lo usa para enseñar la composición enemiga antes de entrar.</summary>
        public sealed class PlanNivel
        {
            public Tablero Tab;
            public int Disposicion;                      // 0..7: de qué lado/esquina sale cada bando
            public Pos[] PosGatos;                       // 4 sitios de salida, agrupados
            public Pos DirGatos, DirEnemigos;            // hacia dónde miran al empezar
            public List<EnemigoDef> Enemigos = new List<EnemigoDef>();
            public List<Pos> PosEnemigos = new List<Pos>();
            public bool HayJefe;
        }

        /// <summary>Tamaño del tablero (propuesto): los niveles normales miden 8-11 x 7-10 y el del jefe 10-12 x 9-11.</summary>
        private static void Tamano(Azar az, int nivel, out int w, out int h)
        {
            bool jefe = nivel >= Config.NivelesPorMundo;
            w = jefe ? az.Entre(10, 13) : az.Entre(8, 12);
            h = jefe ? az.Entre(9, 12) : az.Entre(7, 11);
        }

        /// <summary>Casillas de salida de (gatos, enemigos) según la disposición: izquierda/derecha, arriba/abajo o esquinas opuestas.</summary>
        private static void Anclas(int disp, int w, int h, out Pos gatos, out Pos enemigos)
        {
            Pos a, b;
            switch (disp / 2)
            {
                case 0: a = new Pos(0, h / 2); b = new Pos(w - 1, h / 2); break;
                case 1: a = new Pos(w / 2, 0); b = new Pos(w / 2, h - 1); break;
                case 2: a = new Pos(0, 0); b = new Pos(w - 1, h - 1); break;
                default: a = new Pos(0, h - 1); b = new Pos(w - 1, 0); break;
            }
            if (disp % 2 == 0) { gatos = a; enemigos = b; } else { gatos = b; enemigos = a; }
        }

        /// <summary>Las 'n' casillas pasables libres más cercanas al ancla (con desempate al azar): forman un grupo compacto.</summary>
        private static List<Pos> Grupo(Tablero t, Azar az, Pos ancla, int n, HashSet<Pos> ocupadas)
        {
            var cand = new List<Pos>();
            for (int x = 0; x < t.Ancho; x++)
                for (int y = 0; y < t.Alto; y++)
                {
                    Pos p = new Pos(x, y);
                    if (t.Pasable(p) && !ocupadas.Contains(p)) cand.Add(p);
                }
            az.Mezclar(cand);
            var claves = new List<KeyValuePair<int, Pos>>();
            for (int i = 0; i < cand.Count; i++) claves.Add(new KeyValuePair<int, Pos>(cand[i].Dist(ancla) * 1000 + i, cand[i]));
            claves.Sort(delegate (KeyValuePair<int, Pos> a, KeyValuePair<int, Pos> b) { return a.Key.CompareTo(b.Key); });
            var r = new List<Pos>();
            for (int i = 0; i < n && i < claves.Count; i++) { r.Add(claves[i].Value); ocupadas.Add(claves[i].Value); }
            return r;
        }

        private static Pos Direccion(Pos desde, Pos hacia)
        {
            int dx = hacia.X - desde.X, dy = hacia.Y - desde.Y;
            return Math.Abs(dx) >= Math.Abs(dy) ? new Pos(Math.Sign(dx), 0) : new Pos(0, Math.Sign(dy));
        }

        public static PlanNivel Planear(int mundo, int nivel, int semilla)
        {
            MundoDef m = GameData.Mundos[mundo - 1];
            var az = new Azar(semilla);
            var plan = new PlanNivel();
            int w, h;
            Tamano(az, nivel, out w, out h);
            plan.Disposicion = az.Entre(0, 8);
            Pos aG, aE;
            Anclas(plan.Disposicion, w, h, out aG, out aE);
            plan.Tab = GenerarTablero(az, m, w, h, aG, aE);
            Tablero tab = plan.Tab;

            var ocupadas = new HashSet<Pos>();
            List<Pos> gatos = Grupo(tab, az, aG, Config.GatosPorExpedicion, ocupadas);
            plan.PosGatos = gatos.ToArray();
            plan.DirGatos = Direccion(aG, aE);
            plan.DirEnemigos = Direccion(aE, aG);

            plan.HayJefe = nivel >= Config.NivelesPorMundo;
            int nEnem = plan.HayJefe ? 3 : (nivel == 1 ? 3 : 4);
            List<Pos> sitios = Grupo(tab, az, aE, nEnem, ocupadas);
            if (plan.HayJefe)
            {
                // el jefe se queda en el frente del grupo (la casilla más cercana al centro)
                Pos centro = new Pos(tab.Ancho / 2, tab.Alto / 2);
                int idx = 0;
                for (int i = 1; i < sitios.Count; i++) if (sitios[i].Dist(centro) < sitios[idx].Dist(centro)) idx = i;
                plan.Enemigos.Add(GameData.Enemigo(m.JefeId));
                plan.PosEnemigos.Add(sitios[idx]);
                sitios.RemoveAt(idx);
                for (int i = 0; i < sitios.Count; i++) { plan.Enemigos.Add(GameData.Enemigo(az.Elegir(m.TiposEnemigo))); plan.PosEnemigos.Add(sitios[i]); }
            }
            else
                for (int i = 0; i < sitios.Count; i++) { plan.Enemigos.Add(GameData.Enemigo(az.Elegir(m.TiposEnemigo))); plan.PosEnemigos.Add(sitios[i]); }
            return plan;
        }

        /// <summary>
        /// Crea el combate de un nivel. La semilla determina el tablero, las posiciones y los enemigos,
        /// así un nivel se puede reproducir.
        /// </summary>
        public static Combate Crear(List<Gato> expedicion, int mundo, int nivel, int semilla,
            List<EfectoActivo> efectos, Dictionary<Consumible, int> inventario)
        {
            MundoDef m = GameData.Mundos[mundo - 1];
            PlanNivel plan = Planear(mundo, nivel, semilla);
            var unidades = new List<Unidad>();
            for (int i = 0; i < expedicion.Count && i < plan.PosGatos.Length; i++)
            {
                Unidad u = CrearGato(expedicion[i], i + 1);
                u.Pos = plan.PosGatos[i];
                u.Dir = plan.DirGatos;
                unidades.Add(u);
            }
            int sig = 101;
            for (int i = 0; i < plan.Enemigos.Count; i++)
            {
                Unidad e = CrearEnemigo(plan.Enemigos[i], m, sig++);
                e.Pos = plan.PosEnemigos[i];
                e.Dir = plan.DirEnemigos;
                unidades.Add(e);
            }
            // El combate usa su propio generador para el azar de la batalla (esquivar, jefes), derivado de la semilla.
            return new Combate(plan.Tab, unidades, efectos, inventario, new Azar(Azar.Derivar(semilla, 77)), mundo, nivel);
        }

        // ------------------------------------------------------------------ terreno
        public static Tablero GenerarTablero(Azar az, MundoDef m, int w, int h, Pos anclaGatos, Pos anclaEnemigos)
        {
            for (int intento = 0; intento < Config.IntentosGenerarTablero; intento++)
            {
                Tablero t = new Tablero(w, h);
                bool[,] prot = Protegidas(w, h, anclaGatos, anclaEnemigos);
                bool agua = GenerarAgua(t, az, prot, m.PctAgua);
                GenerarBarro(t, az, prot, m.PctBarro, agua);
                Colocar(t, az, prot, Terreno.HierbaAlta, m.PctHierba, 5);
                Colocar(t, az, prot, Terreno.Flores, m.PctFlores, 3);
                Colocar(t, az, prot, Terreno.Roca, m.PctRoca, 2);
                // una de cada tres rocas es una roca grande (más vida)
                for (int x = 0; x < w; x++)
                    for (int y = 0; y < h; y++)
                        if (t.TerrenoEn(new Pos(x, y)) == Terreno.Roca && az.Prob(0.33)) t.PonerTerreno(new Pos(x, y), Terreno.Muro);
                Colocar(t, az, prot, Terreno.RocaFragil, m.PctRocaFragil, 2);
                Colocar(t, az, prot, Terreno.Caja, m.PctCaja, 1);
                Colocar(t, az, prot, Terreno.Barril, m.PctBarril, 1);
                if (t.EsConexo()) { ColocarMonedas(t, az, prot, m.Monedas); return t; }
            }
            // Si no sale un tablero conexo, se deja sin rocas ni agua.
            Tablero limpio = new Tablero(w, h);
            bool[,] p2 = Protegidas(w, h, anclaGatos, anclaEnemigos);
            Colocar(limpio, az, p2, Terreno.HierbaAlta, m.PctHierba, 5);
            Colocar(limpio, az, p2, Terreno.Flores, m.PctFlores, 3);
            ColocarMonedas(limpio, az, p2, m.Monedas);
            return limpio;
        }

        /// <summary>Casillas cerca de las salidas: se quedan de suelo para que nadie empiece atrapado.</summary>
        private static bool[,] Protegidas(int w, int h, Pos a, Pos b)
        {
            var prot = new bool[w, h];
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                {
                    Pos p = new Pos(x, y);
                    prot[x, y] = p.Dist(a) <= 2 || p.Dist(b) <= 2;
                }
            return prot;
        }

        private static bool Libre(Tablero t, bool[,] prot, Pos p)
        {
            return t.Dentro(p) && !prot[p.X, p.Y] && (t.TerrenoEn(p) == Terreno.Suelo);
        }

        /// <summary>Agua en charcos (mínimo 2x2) o ríos de una casilla de ancho, siempre continuos. Devuelve true si quedó agua.</summary>
        private static bool GenerarAgua(Tablero t, Azar az, bool[,] prot, int pct)
        {
            int objetivo = pct * t.Ancho * t.Alto / 100;
            if (objetivo <= 0) return false;
            int puestos = 0, guardia = 0;
            while (puestos < objetivo && guardia++ < 80)
            {
                if (az.Prob(0.55)) puestos += PonerCharco(t, az, prot);
                else puestos += PonerRio(t, az, prot);
            }
            LimpiarAgua(t);
            for (int x = 0; x < t.Ancho; x++)
                for (int y = 0; y < t.Alto; y++)
                    if (t.TerrenoEn(new Pos(x, y)) == Terreno.Agua) return true;
            return false;
        }

        private static int PonerCharco(Tablero t, Azar az, bool[,] prot)
        {
            int cw = az.Entre(2, 4), ch = az.Entre(2, 4);
            int x0 = az.Entre(0, Math.Max(1, t.Ancho - cw + 1)), y0 = az.Entre(0, Math.Max(1, t.Alto - ch + 1));
            Pos quitar = new Pos(-1, -1);
            if (cw == 3 && ch == 3 && az.Prob(0.5))   // esquina redondeada
                quitar = new Pos(x0 + (az.Prob(0.5) ? 0 : 2), y0 + (az.Prob(0.5) ? 0 : 2));
            var celdas = new List<Pos>();
            for (int x = x0; x < x0 + cw; x++)
                for (int y = y0; y < y0 + ch; y++)
                {
                    Pos p = new Pos(x, y);
                    if (p == quitar) continue;
                    if (!t.Dentro(p) || prot[x, y]) return 0;
                    Terreno ter = t.TerrenoEn(p);
                    if (ter != Terreno.Suelo && ter != Terreno.Agua) return 0;
                    celdas.Add(p);
                }
            int nuevas = 0;
            foreach (Pos p in celdas) { if (t.TerrenoEn(p) != Terreno.Agua) { t.PonerTerreno(p, Terreno.Agua); nuevas++; } }
            return nuevas;
        }

        private static int PonerRio(Tablero t, Azar az, bool[,] prot)
        {
            // empieza en un borde y avanza hacia dentro, girando de vez en cuando
            Pos p; Pos dir;
            switch (az.Entre(0, 4))
            {
                case 0: p = new Pos(0, az.Entre(0, t.Alto)); dir = new Pos(1, 0); break;
                case 1: p = new Pos(t.Ancho - 1, az.Entre(0, t.Alto)); dir = new Pos(-1, 0); break;
                case 2: p = new Pos(az.Entre(0, t.Ancho), 0); dir = new Pos(0, 1); break;
                default: p = new Pos(az.Entre(0, t.Ancho), t.Alto - 1); dir = new Pos(0, -1); break;
            }
            int largo = az.Entre(5, Math.Max(t.Ancho, t.Alto) + 3);
            int nuevas = 0;
            for (int i = 0; i < largo; i++)
            {
                if (!t.Dentro(p) || prot[p.X, p.Y]) break;
                Terreno ter = t.TerrenoEn(p);
                if (ter != Terreno.Suelo && ter != Terreno.Agua) break;
                if (ter == Terreno.Suelo) { t.PonerTerreno(p, Terreno.Agua); nuevas++; }
                if (az.Prob(0.28))   // giro de 90 grados
                    dir = az.Prob(0.5) ? new Pos(-dir.Y, dir.X) : new Pos(dir.Y, -dir.X);
                p = new Pos(p.X + dir.X, p.Y + dir.Y);
            }
            return nuevas;
        }

        /// <summary>Quita el agua suelta: un grupo de agua solo vale si tiene un bloque 2x2 o es una línea continua de 3 o más casillas.</summary>
        private static void LimpiarAgua(Tablero t)
        {
            var vistos = new HashSet<Pos>();
            for (int x = 0; x < t.Ancho; x++)
                for (int y = 0; y < t.Alto; y++)
                {
                    Pos ini = new Pos(x, y);
                    if (t.TerrenoEn(ini) != Terreno.Agua || vistos.Contains(ini)) continue;
                    var grupo = new List<Pos>();
                    var cola = new Queue<Pos>();
                    cola.Enqueue(ini); vistos.Add(ini);
                    while (cola.Count > 0)
                    {
                        Pos c = cola.Dequeue(); grupo.Add(c);
                        foreach (Pos q in t.Vecinos(c))
                            if (t.TerrenoEn(q) == Terreno.Agua && vistos.Add(q)) cola.Enqueue(q);
                    }
                    if (grupo.Count >= 3 || TieneBloque2x2(t, grupo)) continue;
                    foreach (Pos c in grupo) t.PonerTerreno(c, Terreno.Suelo);
                }
        }

        private static bool TieneBloque2x2(Tablero t, List<Pos> grupo)
        {
            foreach (Pos c in grupo)
            {
                Pos a = new Pos(c.X + 1, c.Y), b = new Pos(c.X, c.Y + 1), d = new Pos(c.X + 1, c.Y + 1);
                if (t.Dentro(d) && t.TerrenoEn(a) == Terreno.Agua && t.TerrenoEn(b) == Terreno.Agua && t.TerrenoEn(d) == Terreno.Agua) return true;
            }
            return false;
        }

        /// <summary>true si en el tablero no queda agua suelta (usado por las pruebas).</summary>
        public static bool AguaValida(Tablero t)
        {
            var vistos = new HashSet<Pos>();
            for (int x = 0; x < t.Ancho; x++)
                for (int y = 0; y < t.Alto; y++)
                {
                    Pos ini = new Pos(x, y);
                    if (t.TerrenoEn(ini) != Terreno.Agua || vistos.Contains(ini)) continue;
                    var grupo = new List<Pos>(); var cola = new Queue<Pos>();
                    cola.Enqueue(ini); vistos.Add(ini);
                    while (cola.Count > 0)
                    {
                        Pos c = cola.Dequeue(); grupo.Add(c);
                        foreach (Pos q in t.Vecinos(c)) if (t.TerrenoEn(q) == Terreno.Agua && vistos.Add(q)) cola.Enqueue(q);
                    }
                    if (grupo.Count < 3 && !TieneBloque2x2(t, grupo)) return false;
                }
            return true;
        }

        /// <summary>El barro puede ser una casilla suelta, pero tiende a estar pegado al agua (orilla).</summary>
        private static void GenerarBarro(Tablero t, Azar az, bool[,] prot, int pct, bool hayAgua)
        {
            int objetivo = pct * t.Ancho * t.Alto / 100;
            if (objetivo <= 0) return;
            if (!hayAgua) { Colocar(t, az, prot, Terreno.Barro, pct, 5); return; }
            var orilla = new List<Pos>(); var cerca = new List<Pos>();
            for (int x = 0; x < t.Ancho; x++)
                for (int y = 0; y < t.Alto; y++)
                {
                    Pos p = new Pos(x, y);
                    if (!Libre(t, prot, p)) continue;
                    int d = int.MaxValue;
                    for (int dx = -3; dx <= 3; dx++)
                        for (int dy = -3; dy <= 3; dy++)
                        {
                            Pos q = new Pos(x + dx, y + dy);
                            if (t.Dentro(q) && t.TerrenoEn(q) == Terreno.Agua) d = Math.Min(d, Math.Max(Math.Abs(dx), Math.Abs(dy)));
                        }
                    if (d <= 1) orilla.Add(p); else if (d <= 3) cerca.Add(p);
                }
            az.Mezclar(orilla); az.Mezclar(cerca);
            int puestos = 0;
            for (int i = 0; i < orilla.Count && puestos < objetivo; i++)
                if (az.Prob(0.7)) { t.PonerTerreno(orilla[i], Terreno.Barro); puestos++; }
            for (int i = 0; i < cerca.Count && puestos < objetivo; i++)
                if (az.Prob(0.35)) { t.PonerTerreno(cerca[i], Terreno.Barro); puestos++; }
        }

        /// <summary>Monedas tiradas en el campo: montones de 1 o 2 en casillas libres lejos de las salidas.</summary>
        private static void ColocarMonedas(Tablero t, Azar az, bool[,] prot, int montones)
        {
            int puestos = 0, guardia = 0;
            while (puestos < montones && guardia++ < 400)
            {
                Pos p = new Pos(az.Entre(0, t.Ancho), az.Entre(0, t.Alto));
                if (!t.Pasable(p) || prot[p.X, p.Y] || t.MonedasEn(p) > 0) continue;
                t.PonerMonedas(p, az.Entre(1, 3));
                puestos++;
            }
        }

        /// <summary>Reparte manchas de un terreno por el tablero (las zonas de salida quedan libres).</summary>
        private static void Colocar(Tablero t, Azar az, bool[,] prot, Terreno tipo, int porcentaje, int tamMancha)
        {
            int objetivo = porcentaje * t.Ancho * t.Alto / 100;
            int puestos = 0;
            int guardia = 0;
            while (puestos < objetivo && guardia++ < 400)
            {
                Pos p = new Pos(az.Entre(0, t.Ancho), az.Entre(0, t.Alto));
                int tam = az.Entre(1, tamMancha + 1);
                for (int i = 0; i < tam && puestos < objetivo; i++)
                {
                    if (Libre(t, prot, p))
                    {
                        t.PonerTerreno(p, tipo);
                        puestos++;
                    }
                    Pos d = Tablero.Dirs4[az.Entre(0, 4)];
                    p = new Pos(p.X + d.X, p.Y + d.Y);
                    if (!t.Dentro(p)) break;
                }
            }
        }
    }
}
