// Gatomate - núcleo del juego (sin dependencias de Unity).
// Desafíos de recompensa: tras cada nivel se ofrecen 3 al azar; el jugador elige uno y qué gato lo enfrenta.
// Éxito = 50% + 10% por punto de estadística sobre la dificultad (mín 5%, máx 95%), con ajuste de +1/-1
// si la estadística es la más alta/baja del gato, y repetición por suerte (10% por punto sobre 5).
using System;
using System.Collections.Generic;

namespace Gatomate.Core
{
    public sealed class DesafioDef
    {
        public DesafioTipo Tipo;
        public string Nombre;
        public StatTipo Stat;
        public string SiSaleBien;
        public string SiSaleMal;
        public bool Penaliza = true;     // false = fallar no cuesta nada (se muestra '+' en vez de '+ / −')
    }

    public sealed class Oferta
    {
        public DesafioDef Def;
        public int Dificultad;
    }

    public sealed class ResultadoDesafio
    {
        public bool Exito;
        public bool Repetida;
        public double Probabilidad;
        public string Texto;
    }

    public static class Desafios
    {
        public static readonly List<DesafioDef> Todos = new List<DesafioDef>
        {
            new DesafioDef { Tipo = DesafioTipo.Roca, Nombre = "Roca en el camino", Stat = StatTipo.Fuerza, SiSaleBien = "Un equipamiento", SiSaleMal = "3 de daño al gato" },
            new DesafioDef { Tipo = DesafioTipo.Trampa, Nombre = "Trampa de púas", Stat = StatTipo.Velocidad, SiSaleBien = "Una poción de vida", SiSaleMal = "3 de daño al gato" },
            new DesafioDef { Tipo = DesafioTipo.Glifo, Nombre = "Glifo antiguo", Stat = StatTipo.Inteligencia, SiSaleBien = "Una bendición", SiSaleMal = "Una maldición" },
            new DesafioDef { Tipo = DesafioTipo.Cerradura, Nombre = "Cerradura de cofre", Stat = StatTipo.Destreza, SiSaleBien = "Un consumible", SiSaleMal = "Sin premio y 2 de daño al gato" },
            new DesafioDef { Tipo = DesafioTipo.Altar, Nombre = "Altar misterioso", Stat = StatTipo.Suerte, SiSaleBien = "Una bendición", SiSaleMal = "Una maldición" },
            new DesafioDef { Tipo = DesafioTipo.Cofre, Nombre = "Cofre sin cerradura", Stat = StatTipo.Ninguna, SiSaleBien = "Un consumible", SiSaleMal = "Nada (opción segura y de poco valor)", Penaliza = false }
        };

        private static readonly StatTipo[] StatsDeDesafio = { StatTipo.Fuerza, StatTipo.Inteligencia, StatTipo.Destreza, StatTipo.Velocidad, StatTipo.Suerte };

        public static int Dificultad(int mundo)
        {
            int i = Math.Max(0, Math.Min(Config.DificultadPorMundo.Length - 1, mundo - 1));
            return Config.DificultadPorMundo[i];
        }

        /// <summary>Tres desafíos distintos al azar.</summary>
        public static List<Oferta> Ofrecer(Azar az, int mundo)
        {
            var pool = new List<DesafioDef>(Todos);
            az.Mezclar(pool);
            var l = new List<Oferta>();
            for (int i = 0; i < Config.OpcionesDeRecompensa && i < pool.Count; i++)
                l.Add(new Oferta { Def = pool[i], Dificultad = Dificultad(mundo) });
            return l;
        }

        /// <summary>Estadística usada, con +1 si es la más alta del gato y -1 si es la más baja.</summary>
        public static int StatConAjuste(Gato g, StatTipo st)
        {
            Stats s = g.Stats;
            int v = s.Get(st);
            int max = int.MinValue, min = int.MaxValue;
            foreach (StatTipo t in StatsDeDesafio)
            {
                int x = s.Get(t);
                if (x > max) max = x;
                if (x < min) min = x;
            }
            if (max == min) return v;
            if (v == max) return v + 1;
            if (v == min) return v - 1;
            return v;
        }

        public static double Probabilidad(Gato g, Oferta o)
        {
            if (o.Def.Stat == StatTipo.Ninguna) return 1.0;
            int v = StatConAjuste(g, o.Def.Stat);
            double p = Config.ProbBase + Config.ProbPorPunto * (v - o.Dificultad);
            return Math.Max(Config.ProbMin, Math.Min(Config.ProbMax, p));
        }

        /// <summary>Probabilidad de repetir la tirada por suerte (10% por punto sobre 5).</summary>
        public static double ProbRepetir(Gato g)
        {
            int s = g.Stats.Suerte;
            return s > Config.SuerteNeutra ? Math.Min(1.0, Config.RepetirPorPuntoDeSuerte * (s - Config.SuerteNeutra)) : 0.0;
        }

        public static ResultadoDesafio Resolver(Run run, Oferta o, Gato g)
        {
            Azar az = run.Azar;
            var r = new ResultadoDesafio { Probabilidad = Probabilidad(g, o) };
            if (o.Def.Stat == StatTipo.Ninguna) r.Exito = true;
            else
            {
                r.Exito = az.Prob(r.Probabilidad);
                if (!r.Exito)
                {
                    double pr = ProbRepetir(g);
                    if (pr > 0 && az.Prob(pr))
                    {
                        r.Repetida = true;
                        r.Exito = az.Prob(r.Probabilidad);
                    }
                }
            }

            string cabecera = o.Def.Nombre + " (" + g.Nombre + "): " + (r.Exito ? "¡sale bien!" : "sale mal.") + (r.Repetida ? " (repitió la tirada por suerte)" : "") + " ";
            string detalle;
            switch (o.Def.Tipo)
            {
                case DesafioTipo.Roca:
                    detalle = r.Exito ? DarEquipo(run) : Herir(g, 3);
                    break;
                case DesafioTipo.Trampa:
                    if (r.Exito) { run.AgregarConsumible(Consumible.PocionVida); detalle = "Gana una " + Textos.Nombre(Consumible.PocionVida).ToLower() + "."; }
                    else detalle = Herir(g, 3);
                    break;
                case DesafioTipo.Glifo:
                case DesafioTipo.Altar:
                    detalle = r.Exito ? DarEfecto(run, true) : DarEfecto(run, false);
                    break;
                case DesafioTipo.Cerradura:
                    detalle = r.Exito ? DarConsumible(run) : Herir(g, 2) + " Sin premio.";
                    break;
                default:
                    detalle = DarConsumible(run);
                    break;
            }
            r.Texto = cabecera + detalle;
            return r;
        }

        private static string Herir(Gato g, int dano)
        {
            int antes = g.Vida;
            g.Vida = Math.Max(1, g.Vida - dano);   // un fallo nunca deja a un gato con menos de 1 de vida
            return g.Nombre + " pierde " + (antes - g.Vida) + " de vida.";
        }

        private static string DarEquipo(Run run)
        {
            EquipoTipo e = (EquipoTipo)run.Azar.Entre(0, Enum.GetValues(typeof(EquipoTipo)).Length);
            run.EquipoSinAsignar.Add(e);
            return "Gana: " + Textos.Nombre(e) + " (" + Textos.Descripcion(e) + ").";
        }

        private static string DarConsumible(Run run)
        {
            Consumible c = (Consumible)run.Azar.Entre(0, Enum.GetValues(typeof(Consumible)).Length);
            run.AgregarConsumible(c);
            return "Gana: " + Textos.Nombre(c) + ".";
        }

        private static readonly EfectoParty[] Bendiciones = { EfectoParty.Furia, EfectoParty.PielDePiedra, EfectoParty.PiesLigeros, EfectoParty.MenteClara };
        private static readonly EfectoParty[] Maldiciones = { EfectoParty.Debilidad, EfectoParty.Fragilidad, EfectoParty.Lentitud, EfectoParty.Sed };

        private static string DarEfecto(Run run, bool bendicion)
        {
            EfectoParty tipo = run.Azar.Elegir(bendicion ? Bendiciones : Maldiciones);
            int turnos = run.Azar.Entre(Config.TurnosBendicionMin, Config.TurnosBendicionMax + 1);
            run.AgregarEfecto(tipo, turnos);
            return (bendicion ? "Bendición: " : "Maldición: ") + Textos.Nombre(tipo) + " (" + Textos.Descripcion(tipo) + ") durante " + turnos + " turnos.";
        }
    }
}
