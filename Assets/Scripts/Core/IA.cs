// Gatomate - núcleo del juego (sin dependencias de Unity).
// IA de enemigos y jefes. Regla del documento: "cada enemigo se mueve hacia el gato más cercano y ataca
// al de menos vida a su alcance; el sanador cura al aliado con menos vida". Además (propuesto): los invocadores
// llaman a sus crías, el gato bomba explota junto a un gato y el ladrón va a por las monedas del suelo.
using System;
using System.Collections.Generic;

namespace Gatomate.Core
{
    public static class IA
    {
        public static void JugarTurnoEnemigo(Combate c, Unidad u)
        {
            if (!u.Vivo || c.Estado != EstadoCombate.EnCurso) return;
            InvocarSiToca(c, u);
            if (u.Def != null && u.Def.Kamikaze) { JugarKamikaze(c, u); return; }
            if (u.Def != null && u.Def.Ladron && JugarLadron(c, u)) return;
            if (u.Habilidad.Efecto == EfectoHab.Curar) { JugarSanador(c, u); return; }
            if (u.Def != null && u.Def.EsJefe) { JugarJefe(c, u); return; }
            if (u.Habilidad.Efecto == EfectoHab.Ninguno) return;       // el nido solo invoca
            MoverYAtacar(c, u, false);
        }

        // ------------------------------------------------------------------ enemigos nuevos
        private static void InvocarSiToca(Combate c, Unidad u)
        {
            EnemigoDef d = u.Def;
            if (d == null || string.IsNullOrEmpty(d.Invoca) || d.InvocaCada <= 0) return;
            if (u.TurnosPropios % d.InvocaCada != 0) return;
            int vivos = 0;
            foreach (Unidad e in c.Vivos(Bando.Enemigos)) if (e.Dueno == u) vivos++;
            if (vivos >= d.InvocaMax) return;
            Pos? p = c.LibreCerca(u.Pos, 2);
            if (p == null) return;
            c.Registrar(u.Nombre + " llama a una cría.");
            c.Invocar(GameData.Enemigo(d.Invoca), p.Value, u);
        }

        /// <summary>Gato bomba: corre hacia el gato más cercano y, si queda pegado a uno, explota.</summary>
        private static void JugarKamikaze(Combate c, Unidad u)
        {
            var gatos = c.Vivos(Bando.Gatos);
            if (gatos.Count == 0) return;
            ElegirYMover(c, u, MasCercano(u, gatos));
            foreach (Pos q in c.Tab.Vecinos(u.Pos))
            {
                Unidad o = c.UnidadEn(q);
                if (o != null && o.Bando == Bando.Gatos)
                {
                    c.Registrar(u.Nombre + " explota.");
                    c.Matar(u);
                    return;
                }
            }
        }

        /// <summary>Ladrón: va hacia las monedas del suelo; con monedas encima huye de los gatos. Devuelve false si no hay nada que robar.</summary>
        private static bool JugarLadron(Combate c, Unidad u)
        {
            bool huir = u.Monedas > 0;
            var monedas = new List<Pos>();
            foreach (var kv in c.Tab.Monedas) monedas.Add(kv.Key);
            if (!huir && monedas.Count == 0) return false;
            var gatos = c.Vivos(Bando.Gatos);
            var rest = c.Alcance(u, c.MovimientoRestante, null);
            Pos mejor = u.Pos; double mejorPt = double.NegativeInfinity;
            foreach (var kv in rest)
            {
                Pos p = kv.Key;
                if (p != u.Pos && c.UnidadEn(p) != null) continue;
                double pt;
                if (huir)
                {
                    int dmin = 99;
                    foreach (Unidad g in gatos) dmin = Math.Min(dmin, p.Dist(g.Pos));
                    pt = dmin * 5;
                }
                else
                {
                    int dm = 99;
                    foreach (Pos q in monedas) dm = Math.Min(dm, p.Dist(q));
                    pt = c.Tab.MonedasEn(p) > 0 ? 1000 : -dm * 10;
                }
                Zona z = c.Tab.ZonaEn(p);
                if (z != null && (z.Tipo == ZonaTipo.Fuego || z.Dueno == Bando.Gatos)) pt -= 80;
                if (p == u.Pos) pt += 0.5;
                if (pt > mejorPt) { mejorPt = pt; mejor = p; }
            }
            if (mejor != u.Pos) c.Mover(mejor);
            Atacar(c, u, null, false);      // si le queda un gato al lado, lo muerde
            return true;
        }

        // ------------------------------------------------------------------ enemigos comunes
        /// <summary>Mueve hacia el gato más cercano y ataca. Devuelve la vida total que perdieron los gatos.</summary>
        private static int MoverYAtacar(Combate c, Unidad u, bool objetivoMasVida)
        {
            var gatos = c.Vivos(Bando.Gatos);
            if (gatos.Count == 0) return 0;
            Unidad forzado = Provocador(u);
            Unidad meta = forzado != null ? forzado : (objetivoMasVida ? MasVida(gatos) : MasCercano(u, gatos));
            ElegirYMover(c, u, meta);
            int antes = VidaTotal(c);
            Atacar(c, u, forzado, objetivoMasVida);
            return antes - VidaTotal(c);
        }

        private static Unidad Provocador(Unidad u)
        {
            EstadoActivo e = u.Estado(EstadoTipo.Provocado);
            if (e != null && e.Fuente != null && e.Fuente.Vivo) return e.Fuente;
            return null;
        }

        private static int VidaTotal(Combate c)
        {
            int s = 0;
            foreach (Unidad g in c.Vivos(Bando.Gatos)) s += Math.Max(0, g.Vida);
            return s;
        }

        private static Unidad MasCercano(Unidad u, List<Unidad> gatos)
        {
            Unidad mejor = null; int md = int.MaxValue;
            foreach (Unidad g in gatos)
            {
                int d = u.Pos.Dist(g.Pos);
                if (d < md) { md = d; mejor = g; }
            }
            return mejor;
        }

        private static Unidad MasVida(List<Unidad> gatos)
        {
            Unidad mejor = null;
            foreach (Unidad g in gatos)
                if (mejor == null || g.Vida > mejor.Vida) mejor = g;
            return mejor;
        }

        private static bool Alcanza(HabilidadDef h, Pos desde, Pos objetivo)
        {
            int d = desde.Dist(objetivo);
            if (h.Efecto == EfectoHab.AreaCruz) return d >= 0 && d <= h.RangoMax + 1;
            return d >= h.RangoMin && d <= h.RangoMax;
        }

        private static void ElegirYMover(Combate c, Unidad u, Unidad meta)
        {
            HabilidadDef h = u.Habilidad;
            var rest = c.Alcance(u, c.MovimientoRestante, null);
            Pos mejor = u.Pos;
            double mejorPt = double.NegativeInfinity;
            foreach (var kv in rest)
            {
                Pos p = kv.Key;
                if (p != u.Pos && c.UnidadEn(p) != null) continue;
                double pt = 0;
                int d = p.Dist(meta.Pos);
                if (Alcanza(h, p, meta.Pos))
                {
                    pt += 1000;
                    if (h.RangoMax > 1) pt += d * 3;        // los de rango se quedan lejos
                }
                else pt -= d * 10;
                Zona z = c.Tab.ZonaEn(p);
                if (z != null)
                {
                    if (z.Tipo == ZonaTipo.Fuego) pt -= 80;
                    else if (z.Dueno == Bando.Gatos) pt -= 60;
                }
                Terreno t = c.Tab.TerrenoEn(p);
                if (t == Terreno.HierbaAlta) pt += 4;
                else if (t == Terreno.Flores) pt += 3;
                else if (t == Terreno.Barro) pt -= 2;
                else if (t == Terreno.Agua) pt -= 3;
                if (p == u.Pos) pt += 0.5;                  // a igualdad, no se mueve
                if (pt > mejorPt) { mejorPt = pt; mejor = p; }
            }
            if (mejor != u.Pos) c.Mover(mejor);
        }

        private static void Atacar(Combate c, Unidad u, Unidad forzado, bool objetivoMasVida)
        {
            HabilidadDef h = u.Habilidad;
            if (h.Efecto == EfectoHab.AreaCruz) { AtacarArea(c, u, h, forzado); return; }

            var candidatos = new List<Unidad>();
            foreach (Pos p in c.CasillasObjetivo(u, h))
            {
                Unidad o = c.UnidadEn(p);
                if (o != null && o.Bando == Bando.Gatos) candidatos.Add(o);
            }
            if (candidatos.Count == 0) return;

            Unidad elegido = null;
            if (forzado != null && candidatos.Contains(forzado)) elegido = forzado;
            else
            {
                foreach (Unidad o in candidatos)
                {
                    if (elegido == null) { elegido = o; continue; }
                    if (objetivoMasVida ? o.Vida > elegido.Vida : o.Vida < elegido.Vida) elegido = o;
                }
            }
            c.EjecutarHabilidad(u, h, elegido.Pos, false);
        }

        private static void AtacarArea(Combate c, Unidad u, HabilidadDef h, Unidad forzado)
        {
            Pos? mejor = null; int mejorPt = 0;
            foreach (Pos p in c.CasillasObjetivo(u, h))
            {
                int pt = 0;
                foreach (Pos q in c.Tab.Cruz(p))
                {
                    Unidad o = c.UnidadEn(q);
                    if (o == null || o.Bando != Bando.Gatos) continue;
                    pt += 100 + (o.VidaMax - o.Vida);
                    if (o == forzado) pt += 500;
                }
                if (pt > mejorPt) { mejorPt = pt; mejor = p; }
            }
            if (mejor != null) c.EjecutarHabilidad(u, h, mejor.Value, false);
        }

        // ------------------------------------------------------------------ sanador
        private static void JugarSanador(Combate c, Unidad u)
        {
            HabilidadDef h = u.Habilidad;
            var gatos = c.Vivos(Bando.Gatos);
            Unidad herido = null;
            foreach (Unidad a in c.Vivos(Bando.Enemigos))
                if (a.Vida < a.VidaMax && (herido == null || a.Vida < herido.Vida)) herido = a;

            var rest = c.Alcance(u, c.MovimientoRestante, null);
            Pos mejor = u.Pos; double mejorPt = double.NegativeInfinity;
            foreach (var kv in rest)
            {
                Pos p = kv.Key;
                if (p != u.Pos && c.UnidadEn(p) != null) continue;
                double pt = 0;
                int dCerca = int.MaxValue;
                foreach (Unidad g in gatos) dCerca = Math.Min(dCerca, p.Dist(g.Pos));
                if (gatos.Count == 0) dCerca = 0;
                if (herido != null)
                {
                    int dh = p.Dist(herido.Pos);
                    if (dh <= h.RangoMax) pt += 500; else pt -= dh * 10;
                }
                pt += Math.Min(dCerca, 6) * 4;              // se queda atrás
                Zona z = c.Tab.ZonaEn(p);
                if (z != null && (z.Tipo == ZonaTipo.Fuego || z.Dueno == Bando.Gatos)) pt -= 80;
                if (p == u.Pos) pt += 0.5;
                if (pt > mejorPt) { mejorPt = pt; mejor = p; }
            }
            if (mejor != u.Pos) c.Mover(mejor);
            if (herido != null && c.CasillasObjetivo(u, h).Contains(herido.Pos))
                c.EjecutarHabilidad(u, h, herido.Pos, false);
        }

        // ------------------------------------------------------------------ jefes
        private static void JugarJefe(Combate c, Unidad u)
        {
            string id = u.Def.Id;
            int t = u.TurnosPropios;

            // Efecto de "mitad de vida" del jefe 3 (invocar 2 lodos pequeños)
            if (id == "jefe3" && !u.MitadUsada && u.Vida * 2 <= u.VidaMax)
            {
                u.MitadUsada = true;
                Invocar(c, u, "lodo", 2);
            }

            switch (id)
            {
                case "jefe1":
                    if (t % 3 == 0 && Salto(c, u)) return;
                    MoverYAtacar(c, u, false);
                    break;
                case "jefe2":
                    if (t % 2 == 0) Raices(c, u);
                    MoverYAtacar(c, u, false);
                    break;
                case "jefe3":
                    if (t % 3 == 0) Barro(c, u);
                    MoverYAtacar(c, u, false);
                    break;
                case "jefe4":
                    if (t % 3 == 0) Invocar(c, u, "acosador", 2);
                    {
                        int dano = MoverYAtacar(c, u, false);
                        if (dano > 0) c.Curar(u, dano, "Mordisco");
                    }
                    break;
                case "jefe5":
                    if (t % 4 == 0) Muros(c, u, 5);
                    MoverYAtacar(c, u, false);
                    break;
                case "jefe6":
                    if (t % 3 == 0) Tormenta(c, u);
                    MoverYAtacar(c, u, true);
                    break;
                default: // jefe7: rota las habilidades de los jefes 1 a 6
                    {
                        bool mitad = u.Vida * 2 <= u.VidaMax;
                        if (mitad || t % 2 == 0)
                        {
                            int k = u.RotacionJefe % 6;
                            u.RotacionJefe++;
                            c.Registrar(u.Nombre + " usa la habilidad del jefe " + (k + 1) + ".");
                            switch (k)
                            {
                                case 0: if (Salto(c, u)) return; break;
                                case 1: Raices(c, u); break;
                                case 2: Barro(c, u); break;
                                case 3: Invocar(c, u, "acosador", 2); break;
                                case 4: Muros(c, u, 5); break;
                                default: Tormenta(c, u); break;
                            }
                        }
                        MoverYAtacar(c, u, false);
                        break;
                    }
            }
        }

        /// <summary>Salto: cae a hasta 4 casillas y daña la casilla y sus 4 vecinas. Reemplaza el turno. Devuelve false si no hay a quién saltar.</summary>
        private static bool Salto(Combate c, Unidad u)
        {
            Pos? mejor = null; int mejorPt = 0;
            for (int x = 0; x < c.Tab.Ancho; x++)
                for (int y = 0; y < c.Tab.Alto; y++)
                {
                    Pos p = new Pos(x, y);
                    if (p.Dist(u.Pos) > 4 || !c.CasillaLibre(p)) continue;
                    int pt = 0;
                    foreach (Pos q in c.Tab.Cruz(p))
                    {
                        Unidad o = c.UnidadEn(q);
                        if (o != null && o.Bando == Bando.Gatos) pt += 100 + (o.VidaMax - o.Vida);
                    }
                    if (pt > mejorPt) { mejorPt = pt; mejor = p; }
                }
            if (mejor == null) return false;
            Pos desde = u.Pos;
            u.Pos = mejor.Value;
            c.Registrar(u.Nombre + " salta y cae en " + u.Pos + ".");
            c.Emitir(new Evento { Tipo = EvTipo.Salto, U = u, A = desde, B = u.Pos, Area = c.Tab.Cruz(u.Pos) });
            foreach (Pos q in c.Tab.Cruz(u.Pos))
            {
                Unidad o = c.UnidadEn(q);
                if (o != null && o.Bando == Bando.Gatos) c.Danar(u, o, 4, true);
            }
            return true;
        }

        private static void Raices(Combate c, Unidad u)
        {
            var gatos = c.Vivos(Bando.Gatos);
            if (gatos.Count == 0) return;
            Unidad g = c.Azar.Elegir(gatos);
            g.PonerEstado(EstadoTipo.Inmovilizado, 1, 0, u);
            c.Registrar(u.Nombre + " inmoviliza a " + g.Nombre + " con raíces.");
            c.Emitir(new Evento { Tipo = EvTipo.Texto, Obj = g, Txt = "¡Raíces!" });
        }

        private static void Barro(Combate c, Unidad u)
        {
            int n = 0, guardia = 0;
            var tiles = new List<Pos>();
            while (n < 4 && guardia++ < 200)
            {
                Pos p = new Pos(c.Azar.Entre(0, c.Tab.Ancho), c.Azar.Entre(0, c.Tab.Alto));
                Terreno t = c.Tab.TerrenoEn(p);
                if (!c.Tab.Pasable(p) || t == Terreno.Barro || t == Terreno.Agua) continue;
                c.Tab.PonerTerreno(p, Terreno.Barro);
                tiles.Add(p);
                n++;
            }
            c.Registrar(u.Nombre + " convierte casillas en barro.");
            c.Emitir(new Evento { Tipo = EvTipo.Terreno, U = u, Area = tiles, Txt = "barro" });
        }

        private static void Muros(Combate c, Unidad u, int cantidad)
        {
            int n = 0, guardia = 0;
            var tiles = new List<Pos>();
            while (n < cantidad && guardia++ < 300)
            {
                Pos p = new Pos(c.Azar.Entre(0, c.Tab.Ancho), c.Azar.Entre(0, c.Tab.Alto));
                if (!c.CasillaLibre(p)) continue;
                Terreno viejo = c.Tab.TerrenoEn(p);
                c.Tab.PonerTerreno(p, Terreno.Muro);
                if (!c.Tab.EsConexo()) { c.Tab.PonerTerreno(p, viejo); continue; }   // nunca se bloquea el camino por completo
                c.Tab.QuitarZona(p);
                tiles.Add(p);
                n++;
            }
            c.Registrar(u.Nombre + " levanta " + n + " casillas de muro.");
            c.Emitir(new Evento { Tipo = EvTipo.Terreno, U = u, Area = tiles, Txt = "muro" });
        }

        private static void Tormenta(Combate c, Unidad u)
        {
            c.Registrar(u.Nombre + " desata la tormenta.");
            c.Emitir(new Evento { Tipo = EvTipo.Tormenta, U = u });
            foreach (Unidad g in c.Vivos(Bando.Gatos)) c.DanoDirecto(g, 2, "la tormenta");
        }

        private static void Invocar(Combate c, Unidad u, string enemigoId, int cantidad)
        {
            for (int i = 0; i < cantidad; i++)
            {
                Pos? p = c.LibreCerca(u.Pos, 4);
                if (p == null) break;
                c.Invocar(GameData.Enemigo(enemigoId), p.Value);
            }
        }
    }
}
