// Gatomate - núcleo del juego (sin dependencias de Unity).
// Jugador automático (bot) sencillo: sirve para probar el balance sin jugar a mano (simulación)
// y como botón "Auto" en la vista. No es un jugador experto: elige la mejor jugada del turno con una puntuación simple.
using System;
using System.Collections.Generic;

namespace Gatomate.Core
{
    public static class BotJugador
    {
        private sealed class Plan
        {
            public Pos Casilla;
            public HabilidadDef Hab;          // null = no usa habilidad
            public bool UsaConsumible;
            public Consumible Cons;
            public Pos Objetivo;
            public double Valor;
            public double Total;              // valor * 10 + puntos de la casilla
        }

        // ------------------------------------------------------------------ combate
        public static void JugarCombate(Combate c)
        {
            int guardia = 0;
            while (c.Estado == EstadoCombate.EnCurso && c.Activa != null && c.Activa.EsGato && guardia++ < 5000)
                JugarTurno(c);
        }

        public static void JugarTurno(Combate c)
        {
            Unidad u = c.Activa;
            if (u == null || !u.EsGato || c.Estado != EstadoCombate.EnCurso) return;

            // Primero, si una habilidad de movilidad deja una jugada claramente mejor, se usa (no gasta la acción).
            for (int i = 0; i < 2; i++) if (!UsarMovilidad(c, u)) break;

            Plan mejor = MejorPlan(c, u);
            if (mejor != null)
            {
                if (mejor.Casilla != u.Pos) c.Mover(mejor.Casilla);
                if (mejor.Hab != null) c.UsarHabilidad(mejor.Hab, mejor.Objetivo);
                else if (mejor.UsaConsumible) c.UsarConsumible(mejor.Cons, mejor.Objetivo);
            }
            if (c.Estado == EstadoCombate.EnCurso && c.Activa == u) c.TerminarTurnoJugador();
        }

        private static bool EsMovilidad(HabilidadDef h) { return h.Movilidad; }

        /// <summary>Las 2 habilidades elegidas más el ataque básico gratis.</summary>
        private static List<HabilidadDef> Todas(Unidad u)
        {
            var l = new List<HabilidadDef>(u.Habilidades);
            if (u.Basico != null) l.Add(u.Basico);
            return l;
        }

        /// <summary>Mejor jugada (moverse y luego habilidad o consumible) desde la posición actual de la unidad.</summary>
        private static Plan MejorPlan(Combate c, Unidad u)
        {
            Pos orig = u.Pos;
            var casillas = new List<Pos>();
            casillas.Add(orig);
            casillas.AddRange(c.CasillasDeMovimiento());

            Plan mejor = null;
            double mejorTotal = double.NegativeInfinity;
            foreach (Pos t in casillas)
            {
                u.Pos = t;                                   // posición hipotética
                double pos = PuntosCasilla(c, u);
                Plan p = new Plan { Casilla = t };

                foreach (HabilidadDef h in Todas(u))
                {
                    if (EsMovilidad(h) || !c.PuedeUsar(u, h)) continue;
                    foreach (Pos obj in c.CasillasObjetivo(u, h))
                    {
                        double v = ValorHabilidad(c, u, h, obj, orig);
                        if (v > p.Valor) { p.Valor = v; p.Hab = h; p.UsaConsumible = false; p.Objetivo = obj; }
                    }
                }
                foreach (Consumible cons in Enum.GetValues(typeof(Consumible)))
                {
                    if (!c.PuedeUsarConsumible(u, cons)) continue;
                    foreach (Pos obj in c.CasillasConsumible(u, cons))
                    {
                        double v = ValorConsumible(c, u, cons, obj);
                        if (v > p.Valor) { p.Valor = v; p.Hab = null; p.UsaConsumible = true; p.Cons = cons; p.Objetivo = obj; }
                    }
                }

                p.Total = p.Valor * 10 + pos;
                if (p.Total > mejorTotal) { mejorTotal = p.Total; mejor = p; }
            }
            u.Pos = orig;
            return mejor;
        }

        /// <summary>A dónde terminaría la unidad con una habilidad de movilidad (null si no se mueve a una casilla propia).</summary>
        private static Pos? DestinoMovilidad(Combate c, Unidad u, HabilidadDef h, Pos obj)
        {
            switch (h.Efecto)
            {
                case EfectoHab.Teletransporte:
                case EfectoHab.Embestida:
                case EfectoHab.Intercambio:
                    return obj;
                case EfectoHab.TrasEnemigo:
                    return c.LugarTras(u, c.UnidadEn(obj));
                default:
                    return null;
            }
        }

        private static bool UsarMovilidad(Combate c, Unidad u)
        {
            bool alguna = false;
            foreach (HabilidadDef h in u.Habilidades) if (EsMovilidad(h) && c.PuedeUsar(u, h)) alguna = true;
            if (!alguna) return false;

            Plan baseline = MejorPlan(c, u);
            double basePt = baseline != null ? baseline.Total : double.NegativeInfinity;
            double mejorPt = basePt + 5;                      // tiene que mejorar con claridad
            HabilidadDef mh = null; Pos mo = new Pos(0, 0);
            Pos orig = u.Pos;
            foreach (HabilidadDef h in u.Habilidades)
            {
                if (!EsMovilidad(h) || !c.PuedeUsar(u, h)) continue;
                foreach (Pos obj in c.CasillasObjetivo(u, h))
                {
                    Pos? dest = DestinoMovilidad(c, u, h, obj);
                    if (dest == null) continue;
                    u.Pos = dest.Value;
                    Plan p = MejorPlan(c, u);
                    u.Pos = orig;
                    if (p == null) continue;
                    double pt = p.Total - h.CosteMana * 2.0;     // el maná gastado también cuenta
                    if (p.Valor <= 0) continue;                   // moverse solo por moverse no vale la pena
                    if (pt > mejorPt) { mejorPt = pt; mh = h; mo = obj; }
                }
            }
            u.Pos = orig;
            if (mh == null) return false;
            return c.UsarHabilidad(mh, mo);
        }

        private static HabilidadDef Principal(Unidad u)
        {
            HabilidadDef m = null;
            foreach (HabilidadDef h in Todas(u))
                if (h.EsAtaque && !h.Movilidad && (m == null || h.RangoMax > m.RangoMax)) m = h;
            return m;
        }

        private static double PuntosCasilla(Combate c, Unidad u)
        {
            double pt = 0;
            var enemigos = c.Vivos(Bando.Enemigos);
            int cerca = 99;
            foreach (Unidad e in enemigos) cerca = Math.Min(cerca, u.Pos.Dist(e.Pos));
            HabilidadDef h = Principal(u);
            bool curandero = false;
            foreach (HabilidadDef x in u.Habilidades) if (x.Efecto == EfectoHab.Curar || x.Efecto == EfectoHab.CuraArea) curandero = true;

            if (curandero)
            {
                // se mantiene cerca de los aliados heridos
                Unidad herido = null;
                foreach (Unidad a in c.Vivos(Bando.Gatos))
                    if (a != u && (herido == null || a.Vida * 100 / a.VidaMax < herido.Vida * 100 / herido.VidaMax)) herido = a;
                if (herido != null) pt -= 0.4 * u.Pos.Dist(herido.Pos);
            }
            if (h == null)
            {
                pt += Math.Min(cerca, 4) * 0.5;            // sin ataques: lejos del frente
            }
            else if (c.RangoMaxEfectivo(u, h) >= 3)
            {
                int rMax = c.RangoMaxEfectivo(u, h);
                int ideal = Math.Max(c.RangoMinEfectivo(u, h), rMax - 1);
                pt -= Math.Abs(cerca - ideal) * 0.6;
                if (cerca <= 1) pt -= 2;
            }
            else
            {
                pt -= cerca * 0.8;
            }

            Zona z = c.Tab.ZonaEn(u.Pos);
            if (z != null)
            {
                if (z.Tipo == ZonaTipo.Fuego) pt -= 30;
                else if (z.Dueno != u.Bando) pt -= 20;
            }
            Terreno t = c.Tab.TerrenoEn(u.Pos);
            if (t == Terreno.HierbaAlta) pt += 1.5;
            else if (t == Terreno.Flores) pt += 1;
            else if (t == Terreno.Barro) pt -= 1;
            else if (t == Terreno.Agua) pt -= 2;
            pt += 0.8 * c.Tab.MonedasEn(u.Pos);               // las monedas valen la pena

            if (u.VidaMax <= 20)
            {
                int ady = 0;
                foreach (Pos q in c.Tab.Vecinos(u.Pos))
                {
                    Unidad o = c.UnidadEn(q);
                    if (o != null && o.Bando == Bando.Enemigos) ady++;
                }
                pt -= 0.5 * ady;
            }
            return pt;
        }

        private static double ValorDano(Combate c, Unidad objetivo, int bruto, Elemento elem, bool melee)
        {
            int d = c.CalcularDano(objetivo, bruto, elem, melee);
            double esq = c.Tab.TerrenoEn(objetivo.Pos) == Terreno.HierbaAlta ? 1 - Config.EsquivaHierbaAlta : 1.0;
            double v = d * esq + (d / (double)Math.Max(1, objetivo.Vida)) * 3;
            if (d >= objetivo.Vida) v += 12;
            if (objetivo.Def != null && elem != Elemento.Fisico)
            {
                if (objetivo.Def.Debil == elem) v += 1.5;
                if (objetivo.Def.Resiste == elem) v -= 1;
            }
            if (objetivo.Vida > d)
            {
                if (elem == Elemento.Veneno && objetivo.Def != null && objetivo.Def.Resiste != Elemento.Veneno) v += 3;
                if (elem == Elemento.Hielo && objetivo.Def != null && objetivo.Def.Resiste != Elemento.Hielo) v += 1.2;
            }
            return v;
        }

        /// <summary>Valor de empujar: choca = daño al objetivo (y al que esté detrás); si el que está detrás es un aliado, es malo.</summary>
        private static double ValorEmpuje(Combate c, Unidad u, Unidad o, int dist)
        {
            if (dist <= 0 || o.Pasiva == Pasiva.Inamovible) return 0;
            int dx = Math.Sign(o.Pos.X - u.Pos.X), dy = Math.Sign(o.Pos.Y - u.Pos.Y);
            if (dx != 0 && dy != 0) dy = 0;
            Pos cur = o.Pos;
            for (int i = 0; i < dist; i++)
            {
                Pos sig = new Pos(cur.X + dx, cur.Y + dy);
                bool bloqueado = !c.Tab.Dentro(sig) || !c.Tab.Pasable(sig);
                Unidad v = bloqueado ? null : c.UnidadEn(sig);
                if (bloqueado || v != null)
                {
                    double dmg = Config.DanoChoque + (dist - i - 1);
                    double r = dmg;
                    if (v != null) r += v.Bando == u.Bando ? -dmg * 1.5 : dmg;
                    return r;
                }
                cur = sig;
            }
            return 0.3 + (c.Tab.TerrenoEn(cur) == Terreno.Agua ? 0.3 : 0);
        }

        private static double ValorFuego(Combate c, Unidad u, List<Pos> casillas)
        {
            double v = 0;
            foreach (Pos q in casillas)
            {
                if (c.Tab.TerrenoEn(q) != Terreno.HierbaAlta) continue;
                Unidad x = c.UnidadEn(q);
                if (x != null && x.Bando == u.Bando) v -= 6;      // se quemaría un aliado
                else if (x != null) v += 2;
                else v += 0.3;
            }
            return v;
        }

        private static double ValorHabilidad(Combate c, Unidad u, HabilidadDef h, Pos obj, Pos origen)
        {
            Unidad o = c.UnidadEn(obj);
            int movidas = c.CasillasMovidas + origen.Dist(u.Pos);
            int mod = c.ModInfligido(u);
            switch (h.Efecto)
            {
                case EfectoHab.Golpe:
                    {
                        if (o == null) return 0;                 // las cajas y rocas no se atacan por iniciativa
                        int bruto = c.BrutoGolpe(u, h, o, movidas) + mod;
                        double v = ValorDano(c, o, bruto, c.ElementoDe(u, h), u.Pos.Dist(o.Pos) == 1);
                        if (h.EmpujeDist > 0) v += ValorEmpuje(c, u, o, h.EmpujeDist);
                        if (c.ElementoDe(u, h) == Elemento.Fuego) v += ValorFuego(c, u, c.Tab.Cruz(obj));
                        return v;
                    }
                case EfectoHab.Carga:
                    {
                        // el gato acabaría pegado al enemigo: se cuenta lo que recorre
                        int rec = Math.Max(0, u.Pos.Dist(obj) - 1);
                        int bruto = c.BrutoGolpe(u, h, o, movidas + rec) + mod;
                        double v = ValorDano(c, o, bruto, c.ElementoDe(u, h), true);
                        if (h.EmpujeDist > 0) v += ValorEmpuje(c, u, o, h.EmpujeDist);
                        return v;
                    }
                case EfectoHab.AreaCruz:
                    {
                        double v = 0;
                        int bruto = c.BrutoGolpe(u, h, null, 0) + mod;
                        var cruz = c.Tab.Cruz(obj);
                        foreach (Pos q in cruz)
                        {
                            Unidad x = c.UnidadEn(q);
                            if (x != null && x.Bando != u.Bando) v += ValorDano(c, x, bruto, c.ElementoDe(u, h), false);
                        }
                        if (c.ElementoDe(u, h) == Elemento.Fuego) v += ValorFuego(c, u, cruz);
                        return v;
                    }
                case EfectoHab.AreaAlrededor:
                    {
                        double v = 0; int n = 0;
                        int bruto = c.BrutoGolpe(u, h, null, 0) + mod;
                        foreach (Pos q in c.TilesRadio(u.Pos, h.Radio))
                        {
                            Unidad x = c.UnidadEn(q);
                            if (x == null || x.Bando == u.Bando) continue;
                            v += ValorDano(c, x, bruto, c.ElementoDe(u, h), q.Dist(u.Pos) == 1) + ValorEmpuje(c, u, x, h.EmpujeDist);
                            n++;
                        }
                        return n >= 1 ? v : 0;
                    }
                case EfectoHab.Curar:
                    {
                        int falta = o.VidaMax - o.Vida;
                        int cura = Math.Min(falta, h.Poder + u.StatValor(h.Stat));
                        double v = cura;
                        if (cura < 3) v *= 0.3;
                        if (o.Vida * 2 <= o.VidaMax) v += 3;
                        if (h.QuitaEstado && o.TieneEstadoNegativo) v += 3;
                        return v;
                    }
                case EfectoHab.CuraArea:
                    {
                        double v = 0;
                        foreach (Unidad a in c.Vivos(u.Bando))
                        {
                            if (a.Pos.Dist(u.Pos) > h.Radio) continue;
                            int cura = Math.Min(a.VidaMax - a.Vida, h.Poder + u.StatValor(h.Stat));
                            v += cura * (cura < 2 ? 0.3 : 1.0);
                        }
                        return v >= 5 ? v : 0;
                    }
                case EfectoHab.Provocar:
                    {
                        EstadoActivo e = o.Estado(EstadoTipo.Provocado);
                        if (e != null && e.Fuente == u) return 0;
                        double v = o.Habilidad.RangoMax > 1 ? 2.5 : 4;
                        if (u.Vida * 2 < u.VidaMax) v -= 2;
                        return Math.Max(0, v);
                    }
                case EfectoHab.Escudo:
                    {
                        int ady = 0;
                        foreach (Unidad e in c.Vivos(Bando.Enemigos)) if (e.Pos.Dist(o.Pos) <= 2) ady++;
                        return ady > 0 && !o.Tiene(EstadoTipo.DefensaMas) ? 2 + ady : 0;
                    }
                case EfectoHab.Potenciar:
                    {
                        if (o.Tiene(EstadoTipo.DanoMas)) return 0;
                        bool ataca = false;
                        foreach (HabilidadDef x in o.Habilidades) if (x.EsAtaque) ataca = true;
                        int cerca = 99;
                        foreach (Unidad e in c.Vivos(Bando.Enemigos)) cerca = Math.Min(cerca, e.Pos.Dist(o.Pos));
                        return ataca && cerca <= 5 ? h.AplicaValor * 1.3 : 0;
                    }
                case EfectoHab.ZonaLinea:
                    {
                        double v = 0;
                        foreach (Pos q in c.Tab.Linea3(u.Pos, obj))
                        {
                            Unidad x = c.UnidadEn(q);
                            if (h.Zona == ZonaTipo.Enredadera)
                            {
                                if (x != null && x.Bando != u.Bando) v += 3;
                                foreach (Pos w in c.Tab.Vecinos(q))
                                {
                                    Unidad y = c.UnidadEn(w);
                                    if (y != null && y.Bando != u.Bando) v += 0.5;
                                }
                            }
                            else if (h.Zona == ZonaTipo.Frasco)
                            {
                                if (x != null && x.Bando != u.Bando) v += 3 * 0.8;
                                else if (x != null) v += Math.Min(Config.CuraFrascoAliado, x.VidaMax - x.Vida) * 0.8;
                            }
                            else
                            {
                                // fuego y veneno: bien para los enemigos, mal para los aliados
                                if (x != null && x.Bando != u.Bando) v += 3;
                                else if (x != null) v -= 4;
                            }
                        }
                        return v >= 3 ? v : 0;
                    }
            }
            return 0;
        }

        private static double ValorConsumible(Combate c, Unidad u, Consumible cons, Pos obj)
        {
            int m = u.Pasiva == Pasiva.Maestria ? 1 : 0;
            Unidad o = c.UnidadEn(obj);
            switch (cons)
            {
                case Consumible.PocionVida:
                    {
                        int falta = o.VidaMax - o.Vida;
                        if (falta < 6 || o.Vida * 2 > o.VidaMax) return 0;
                        return Math.Min(8 + m, falta) * 0.9;
                    }
                case Consumible.PocionMana:
                    {
                        int minimo = 99;
                        foreach (HabilidadDef h in u.Habilidades) minimo = Math.Min(minimo, h.CosteMana);
                        return (o == u && u.ManaMax >= 4 && u.Mana < minimo) ? 4 : 0;
                    }
                case Consumible.Bomba:
                case Consumible.BombaFuego:
                case Consumible.BombaHielo:
                case Consumible.FrascoVeneno:
                    {
                        Elemento e = cons == Consumible.BombaFuego ? Elemento.Fuego : (cons == Consumible.BombaHielo ? Elemento.Hielo : (cons == Consumible.FrascoVeneno ? Elemento.Veneno : Elemento.Fisico));
                        int bruto = (cons == Consumible.Bomba ? 5 : (cons == Consumible.FrascoVeneno ? 2 : 4)) + m;
                        double v = 0; int n = 0;
                        var cruz = c.Tab.Cruz(obj);
                        foreach (Pos q in cruz)
                        {
                            Unidad x = c.UnidadEn(q);
                            if (x != null && x.Bando != u.Bando) { v += ValorDano(c, x, bruto, e, false); n++; }
                        }
                        if (e == Elemento.Fuego) v += ValorFuego(c, u, cruz);
                        return n >= 2 ? v : 0;
                    }
                case Consumible.Antidoto:
                    return o.TieneEstadoNegativo ? 4 : 0;
                default:
                    return 0;
            }
        }

        // ------------------------------------------------------------------ habilidades y tienda de la campaña
        /// <summary>Elige una habilidad para el gato que toca: prefiere atacar con el primero y mezcla con apoyo o movilidad después.</summary>
        public static void ElegirHabilidades(Run r)
        {
            int guardia = 0;
            while (r.Fase == FaseRun.ElegirHabilidad && guardia++ < 100)
            {
                Gato g = r.GatoEligiendo;
                bool tieneAtaque = false;
                foreach (HabilidadDef h in g.Habilidades) if (g.HabilidadIds.Contains(h.Id) && h.EsAtaque) tieneAtaque = true;
                int mejor = 0; double mejorPt = double.NegativeInfinity;
                for (int i = 0; i < r.OpcionesHab.Count; i++)
                {
                    HabilidadDef h = r.OpcionesHab[i];
                    double pt = r.Azar.Doble() * 2.5;                     // algo de variedad
                    if (h.EsAtaque && !h.Movilidad) pt += tieneAtaque ? 1.5 : 4;
                    else if (!tieneAtaque) pt -= 1;
                    if (h.Movilidad && tieneAtaque) pt += 1;
                    if (pt > mejorPt) { mejorPt = pt; mejor = i; }
                }
                if (!r.ElegirHabilidad(mejor)) break;
            }
        }

        /// <summary>Gasta las monedas por orden de utilidad.</summary>
        public static void Comprar(Run r)
        {
            int guardia = 0;
            while (guardia++ < 20)
            {
                int mejor = -1; double mejorPt = double.NegativeInfinity;
                for (int i = 0; i < r.Tienda.Count; i++)
                {
                    OfertaTienda o = r.Tienda[i];
                    if (o.Vendido || o.Precio > r.Dinero) continue;
                    double pt;
                    if (o.EsDescanso)
                    {
                        double falta = 0, maxT = 0;
                        foreach (Gato g in r.TodosLosGatos) { falta += g.VidaMax - g.Vida; maxT += g.VidaMax; }
                        if (maxT <= 0 || falta / maxT < 0.4) continue;      // solo si la party está bastante herida
                        pt = 9.5;
                    }
                    else if (o.EsBanquete) pt = 9;
                    else if (o.EsEquipo) pt = 8;
                    else switch (o.Cons)
                    {
                        case Consumible.PocionVida: pt = 10; break;
                        case Consumible.Bomba: case Consumible.BombaFuego: case Consumible.BombaHielo: pt = 7; break;
                        case Consumible.FrascoVeneno: pt = 5; break;
                        case Consumible.Antidoto: pt = 4; break;
                        default: pt = 3; break;
                    }
                    if (pt > mejorPt) { mejorPt = pt; mejor = i; }
                }
                if (mejor < 0 || !r.Comprar(mejor)) break;
            }
        }

        // ------------------------------------------------------------------ campaña
        /// <summary>Lo bien que le sienta un objeto a un gato (heurística simple del bot).</summary>
        public static double ValorObjeto(Gato g, ObjetoDef d)
        {
            StatTipo principal = Progreso.StatPrincipal(g.Clase);
            double v = d.Vida * 0.5 + d.Def * 2 + d.Mana * 1.0 + d.Mov * 2 + d.Vel * 0.5 + d.Sue * 0.2;
            v += d.Fue * (principal == StatTipo.Fuerza ? 2.0 : 0.3);
            v += d.Int * (principal == StatTipo.Inteligencia ? 2.0 : 0.3);
            v += d.Des * (principal == StatTipo.Destreza ? 2.0 : 0.3);
            if (d.Imbuye != Elemento.Fisico || d.BonoElemVal > 0) v += 1.5;
            if (d.Esp != Especial.Ninguno) v += 1.5 + d.EspVal * 0.5;
            if (d.AfinClase != null && d.AfinClase == g.ClaseId) v += 3;
            return v;
        }

        /// <summary>Reparte el equipamiento sin asignar: cada objeto va al gato y al espacio donde más mejora (cambia el que lleva si el nuevo es mejor).</summary>
        public static void Equipar(Run r)
        {
            for (int vuelta = 0; vuelta < 30; vuelta++)
            {
                Gato mejorG = null; EquipoTipo mejorE = default(EquipoTipo); double mejorGan = 0.5;
                foreach (EquipoTipo e in r.EquipoSinAsignar)
                {
                    ObjetoDef d = Objetos.Def(e);
                    foreach (Gato g in r.Expedicion)
                    {
                        ObjetoDef actual = g.Objeto(d.Slot);
                        double gan = ValorObjeto(g, d) - (actual != null ? ValorObjeto(g, actual) : 0);
                        if (gan > mejorGan) { mejorGan = gan; mejorG = g; mejorE = e; }
                    }
                }
                if (mejorG == null) break;
                if (!r.Equipar(mejorG, mejorE)) break;
            }
        }

        /// <summary>Elige el desafío y el gato con mejor valor esperado.</summary>
        public static void ElegirDesafio(Run r)
        {
            int mi = 0; Gato mg = null; double mv = double.NegativeInfinity;
            for (int i = 0; i < r.Ofertas.Count; i++)
            {
                Oferta o = r.Ofertas[i];
                double premio, castigo;
                switch (o.Def.Tipo)
                {
                    case DesafioTipo.Roca: premio = 4; castigo = 1.5; break;
                    case DesafioTipo.Trampa: premio = 3; castigo = 1.5; break;
                    case DesafioTipo.Glifo: premio = 3; castigo = 3; break;
                    case DesafioTipo.Altar: premio = 3; castigo = 3; break;
                    case DesafioTipo.Cerradura: premio = 2.5; castigo = 1; break;
                    default: premio = 2; castigo = 0; break;
                }
                foreach (Gato g in r.Expedicion)
                {
                    double p = Desafios.Probabilidad(g, o);
                    double pr = Desafios.ProbRepetir(g);
                    double pe = p + (1 - p) * pr * p;
                    double v = pe * premio - (1 - pe) * castigo;
                    if (v > mv) { mv = v; mi = i; mg = g; }
                }
            }
            if (mg != null) r.ElegirDesafio(mi, mg);
        }

        /// <summary>Juega una run completa sin intervención. Devuelve cuando la run termina o se completa la campaña.</summary>
        public static void JugarRun(Run r)
        {
            int guardia = 0;
            while (r.Fase != FaseRun.RunPerdida && r.Fase != FaseRun.CampanaCompletada && guardia++ < 1000)
            {
                switch (r.Fase)
                {
                    case FaseRun.ElegirHabilidad:
                        ElegirHabilidades(r);
                        break;
                    case FaseRun.Tienda:
                        Comprar(r);
                        r.SalirDeTienda();
                        break;
                    case FaseRun.Preparacion:
                        Equipar(r);
                        r.Empezar();
                        break;
                    case FaseRun.Combate:
                        JugarCombate(r.Combate);
                        r.ResolverFinDeCombate();
                        break;
                    case FaseRun.Recompensa:
                        ElegirDesafio(r);
                        break;
                }
            }
        }
    }
}
