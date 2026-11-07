// Gatomate - núcleo del juego (sin dependencias de Unity).
// Combate por turnos: orden de turnos, movimiento, habilidades, terreno, bendiciones y fin del nivel.
using System;
using System.Collections.Generic;

namespace Gatomate.Core
{
    public enum EstadoCombate { EnCurso, Victoria, Derrota }

    public sealed class Combate
    {
        public readonly Tablero Tab;
        public readonly List<Unidad> Unidades;
        public readonly List<EfectoActivo> Efectos;                  // bendiciones y maldiciones activas (copia de la run)
        public readonly Dictionary<Consumible, int> Inventario;      // se comparte con la run
        public readonly Azar Azar;
        public readonly int NumMundo;                                // 1..7
        public readonly int NumNivel;                                // 1..3
        public readonly MundoDef Mundo;

        public EstadoCombate Estado = EstadoCombate.EnCurso;
        public int Ronda;
        public Unidad Activa;
        public int MovimientoRestante;
        public int CasillasMovidas;
        public bool ActuoEsteTurno;
        public int Dinero;                                           // monedas que los gatos han recogido en este nivel
        public readonly List<string> Log = new List<string>();

        /// <summary>Si es true se guardan eventos para animar (la vista lo activa; las simulaciones no).</summary>
        public bool GrabarEventos;
        public readonly List<Evento> Eventos = new List<Evento>();

        private List<Unidad> _orden;
        private int _idx = -1;
        private bool _rondaAbierta;
        private int _sigId = 1000;

        public Combate(Tablero tab, List<Unidad> unidades, List<EfectoActivo> efectos, Dictionary<Consumible, int> inventario, Azar azar, int mundo, int nivel)
        {
            Tab = tab; Unidades = unidades; Efectos = efectos; Inventario = inventario; Azar = azar;
            NumMundo = mundo; NumNivel = nivel;
            GrabarEventos = Config.GrabarEventos;
            Mundo = GameData.Mundos[mundo - 1];
            foreach (var u in unidades)
            {
                if (u.Id >= _sigId) _sigId = u.Id + 1;
                if (u.Dir == new Pos(0, 0)) u.Dir = u.EsGato ? new Pos(1, 0) : new Pos(-1, 0);
                if (u.EsGato && u.Habilidades.Count == 0 && u.Habilidad != null) u.Habilidades.Add(u.Habilidad);
            }
        }

        // ------------------------------------------------------------------ consultas
        public Unidad UnidadEn(Pos p)
        {
            for (int i = 0; i < Unidades.Count; i++)
                if (Unidades[i].Vivo && Unidades[i].Pos == p) return Unidades[i];
            return null;
        }

        public List<Unidad> Vivos(Bando b)
        {
            var l = new List<Unidad>();
            for (int i = 0; i < Unidades.Count; i++)
                if (Unidades[i].Vivo && Unidades[i].Bando == b) l.Add(Unidades[i]);
            return l;
        }

        public bool CasillaLibre(Pos p) { return Tab.Pasable(p) && UnidadEn(p) == null; }

        public bool Efecto(EfectoParty e)
        {
            for (int i = 0; i < Efectos.Count; i++) if (Efectos[i].Tipo == e && Efectos[i].Turnos > 0) return true;
            return false;
        }

        public int ModInfligido(Unidad u)
        {
            if (!u.EsGato) return 0;
            return (Efecto(EfectoParty.Furia) ? 2 : 0) - (Efecto(EfectoParty.Debilidad) ? 2 : 0);
        }

        public int ModRecibido(Unidad u)
        {
            if (!u.EsGato) return 0;
            return (Efecto(EfectoParty.Fragilidad) ? 2 : 0) - (Efecto(EfectoParty.PielDePiedra) ? 2 : 0);
        }

        public bool TieneAliadoVecino(Unidad objetivo)
        {
            foreach (Pos q in Tab.Vecinos(objetivo.Pos))
            {
                Unidad v = UnidadEn(q);
                if (v != null && v != objetivo && v.Bando == objetivo.Bando) return true;
            }
            return false;
        }

        public void Emitir(Evento e) { if (GrabarEventos) Eventos.Add(e); }

        /// <summary>Entrega y vacía los eventos pendientes.</summary>
        public readonly Dictionary<Gato, int> XpGanada = new Dictionary<Gato, int>();   // experiencia ganada en este combate (se aplica al ganar)

        public List<Evento> TomarEventos()
        {
            var l = new List<Evento>(Eventos);
            Eventos.Clear();
            return l;
        }

        /// <summary>Quién juega ahora y quiénes siguen (esta ronda y la siguiente), para mostrar el orden de turnos.</summary>
        public List<Unidad> LineaDeTurnos(int max)
        {
            var l = new List<Unidad>();
            if (_orden != null)
                for (int i = Math.Max(0, _idx); i < _orden.Count; i++)
                    if (_orden[i].Vivo) l.Add(_orden[i]);
            var sig = new List<Unidad>();
            foreach (Unidad u in Unidades) if (u.Vivo) sig.Add(u);
            sig.Sort(delegate (Unidad a, Unidad b)
            {
                int c = b.VelocidadEfectiva.CompareTo(a.VelocidadEfectiva);
                if (c != 0) return c;
                c = a.Bando.CompareTo(b.Bando);
                return c != 0 ? c : a.Id.CompareTo(b.Id);
            });
            l.AddRange(sig);
            if (l.Count > max) l.RemoveRange(max, l.Count - max);
            return l;
        }

        /// <summary>Hacia dónde mira la unidad pasa a ser la dirección (de 4) que apunta a 'hacia'.</summary>
        public void Mirar(Unidad u, Pos hacia)
        {
            int dx = hacia.X - u.Pos.X, dy = hacia.Y - u.Pos.Y;
            if (dx == 0 && dy == 0) return;
            u.Dir = Math.Abs(dx) >= Math.Abs(dy) ? new Pos(Math.Sign(dx), 0) : new Pos(0, Math.Sign(dy));
        }

        /// <summary>true si el atacante está en el lado contrario a hacia donde mira el objetivo (por la espalda).</summary>
        public bool PorLaEspalda(Unidad atacante, Unidad objetivo)
        {
            int vx = atacante.Pos.X - objetivo.Pos.X, vy = atacante.Pos.Y - objetivo.Pos.Y;
            return objetivo.Dir.X * vx + objetivo.Dir.Y * vy < 0;
        }

        public int BonusDanoEstado(Unidad u)
        {
            EstadoActivo e = u.Estado(EstadoTipo.DanoMas);
            return e != null ? e.Valor : 0;
        }

        /// <summary>Daño bruto de una habilidad (antes de defensa y de Furia/Debilidad). 'movidas' = casillas que se movió antes (Impulso).</summary>
        public int BrutoGolpe(Unidad u, HabilidadDef h, Unidad obj, int movidas)
        {
            int stat = u.StatValor(h.Stat);
            if (h.Basico) stat /= 2;   // el ataque básico es gratis: solo suma la mitad de la estadística
            int b = h.Poder + stat + BonusDanoEstado(u);
            if (h.Efecto == EfectoHab.Golpe || h.Efecto == EfectoHab.Carga)
            {
                if (u.Pasiva == Pasiva.Impulso && movidas >= 3) b += 2;
                if (obj != null)
                {
                    if (h.BonusSiAislado > 0 && !TieneAliadoVecino(obj)) b += h.BonusSiAislado;
                    if (PorLaEspalda(u, obj))
                    {
                        b += h.BonusEspalda + u.Esp(Especial.EspaldaMas);
                        if (u.Pasiva == Pasiva.Apunalar) b += 3;
                    }
                }
            }
            Elemento el = ElementoDe(u, h);
            if (el != Elemento.Fisico && u.Pasiva == Pasiva.Canalizar) b += 1;
            if (h.EsAtaque || h.Efecto == EfectoHab.ZonaLinea) b += u.BonusGema(el);
            if (h.EsAtaque)
            {
                if (el == Elemento.Fisico) b += u.Esp(Especial.FisicoMas);
                if (h.Basico) b += u.Esp(Especial.BasicoMas);
                if (h.Efecto == EfectoHab.AreaCruz || h.Efecto == EfectoHab.AreaAlrededor) b += u.Esp(Especial.AreaMas);
            }
            return b;
        }

        /// <summary>Elemento real de un ataque: el suyo, o el del anillo que lleva si el ataque es físico.</summary>
        public Elemento ElementoDe(Unidad u, HabilidadDef h)
        {
            if (h.Elemento != Elemento.Fisico) return h.Elemento;
            if (h.EsAtaque && u != null) return u.Imbuido;
            return Elemento.Fisico;
        }

        /// <summary>Daño previsto de la habilidad sobre un objetivo (sin esquiva), para mostrarlo antes de confirmar.</summary>
        public int PrevisualizarDano(Unidad u, HabilidadDef h, Unidad obj)
        {
            int bruto = BrutoGolpe(u, h, obj, u == Activa ? CasillasMovidas : 0) + ModInfligido(u);
            return CalcularDano(obj, bruto, ElementoDe(u, h), u.Pos.Dist(obj.Pos) == 1, u.Esp(Especial.Perfora));
        }

        /// <summary>Maná que cuesta de verdad una habilidad para esta unidad (algunos objetos abaratan las de movilidad).</summary>
        public int CosteDe(Unidad u, HabilidadDef h)
        {
            int c = h.CosteMana;
            if (h.Movilidad && u != null) c -= u.Esp(Especial.MovilidadBarata);
            return Math.Max(0, c);
        }

        public void Registrar(string s)
        {
            Log.Add(s);
            if (Log.Count > 400) Log.RemoveRange(0, 100);
        }

        // ------------------------------------------------------------------ flujo de turnos
        /// <summary>Empieza el combate y avanza hasta el primer turno de un gato.</summary>
        public void Iniciar()
        {
            Registrar("Empieza el combate: " + Mundo.Nombre + ", nivel " + NumNivel + ".");
            AvanzarHastaGato();
        }

        /// <summary>El jugador termina el turno del gato activo; juegan los enemigos hasta el próximo gato.</summary>
        public void TerminarTurnoJugador()
        {
            if (Config.Medidor == null) { TerminarTurnoJugadorInterno(); return; }
            long t0 = System.Diagnostics.Stopwatch.GetTimestamp();
            TerminarTurnoJugadorInterno();
            Config.Medidor(Config.MedTerminarTurno, System.Diagnostics.Stopwatch.GetTimestamp() - t0);
        }

        private void TerminarTurnoJugadorInterno()
        {
            if (Estado != EstadoCombate.EnCurso || Activa == null || !Activa.EsGato) return;
            TerminarTurno(Activa);
            ComprobarFin();
            AvanzarHastaGato();
        }

        private void AvanzarHastaGato()
        {
            int guardia = 0;
            while (Estado == EstadoCombate.EnCurso && guardia++ < 100000)
            {
                Unidad u = ProximaUnidad();
                if (u == null) return;
                ComenzarTurno(u);
                if (Estado != EstadoCombate.EnCurso) return;
                if (!u.Vivo) { Activa = null; continue; }
                if (u.EsGato) return;
                IA.JugarTurnoEnemigo(this, u);
                if (Estado != EstadoCombate.EnCurso) return;
                TerminarTurno(u);
                ComprobarFin();
            }
        }

        private Unidad ProximaUnidad()
        {
            while (true)
            {
                _idx++;
                if (_orden == null || _idx >= _orden.Count)
                {
                    if (_orden != null)
                    {
                        FinRonda();
                        if (Estado != EstadoCombate.EnCurso) return null;
                    }
                    if (Ronda >= Config.RondasMaximas)
                    {
                        Registrar("Se acabó el tiempo del nivel: cuenta como derrota.");
                        Terminar(EstadoCombate.Derrota);
                        return null;
                    }
                    NuevaRonda();
                    continue;
                }
                Unidad u = _orden[_idx];
                if (u.Vivo) return u;
            }
        }

        private void NuevaRonda()
        {
            Ronda++;
            _rondaAbierta = true;
            _orden = new List<Unidad>();
            foreach (Unidad u in Unidades) if (u.Vivo) _orden.Add(u);
            // Mayor velocidad primero; en empate, gatos antes que enemigos, luego por id.
            _orden.Sort(delegate (Unidad a, Unidad b)
            {
                int c = b.VelocidadEfectiva.CompareTo(a.VelocidadEfectiva);
                if (c != 0) return c;
                c = a.Bando.CompareTo(b.Bando);
                if (c != 0) return c;
                return a.Id.CompareTo(b.Id);
            });
            _idx = -1;
        }

        private void FinRonda()
        {
            // El fuego se propaga a la hierba alta vecina (propuesto) y después las zonas pierden una ronda.
            var nuevos = new List<Pos>();
            foreach (Zona z in Tab.Zonas)
            {
                if (z.Tipo != ZonaTipo.Fuego) continue;
                foreach (Pos q in Tab.Vecinos(z.Pos))
                    if (Tab.TerrenoEn(q) == Terreno.HierbaAlta && !nuevos.Contains(q)) nuevos.Add(q);
            }
            foreach (Pos q in nuevos)
            {
                Tab.PonerTerreno(q, Terreno.Suelo);
                Tab.PonerZona(ZonaTipo.Fuego, q, 2, Bando.Enemigos);
            }
            if (nuevos.Count > 0)
            {
                Registrar("El fuego se extiende por la hierba alta.");
                Emitir(new Evento { Tipo = EvTipo.Zona, Zona = ZonaTipo.Fuego, Area = nuevos });
            }
            Tab.AvanzarZonas();
            TickEfectos();
            _rondaAbierta = false;
        }

        private void TickEfectos()
        {
            for (int i = Efectos.Count - 1; i >= 0; i--)
            {
                Efectos[i].Turnos--;
                if (Efectos[i].Turnos <= 0)
                {
                    Registrar("Termina: " + Textos.Nombre(Efectos[i].Tipo) + ".");
                    Efectos.RemoveAt(i);
                }
            }
        }

        public int MovimientoTotal(Unidad u)
        {
            int m;
            if (u.EsGato)
            {
                // Movimiento propio de la clase (ágiles más, pesados menos). La velocidad solo manda en el orden de turnos,
                // salvo la poción de velocidad, que suma movimiento. Si una unidad de prueba no lo trae, se usa la regla antigua.
                m = u.MovimientoBase > 0 ? u.MovimientoBase : (u.Velocidad + 1) / 2 + 1;
                int extra = u.VelocidadEfectiva - u.Velocidad;
                if (extra > 0) m += (extra + 1) / 2;
                if (Efecto(EfectoParty.PiesLigeros)) m++;
                if (Efecto(EfectoParty.Lentitud)) m--;
            }
            else m = u.MovimientoBase;
            if (m > 0 && u.Tiene(EstadoTipo.Ralentizado)) m = Math.Max(1, m - Config.MovimientoRalentizado);
            if (u.Tiene(EstadoTipo.Inmovilizado)) m = 0;
            return Math.Max(0, m);
        }

        private bool EnredaderaHostil(Unidad u, Pos p)
        {
            Zona z = Tab.ZonaEn(p);
            return z != null && z.Tipo == ZonaTipo.Enredadera && z.Dueno != u.Bando;
        }

        private void ComenzarTurno(Unidad u)
        {
            Activa = u;
            Emitir(new Evento { Tipo = EvTipo.Turno, U = u });
            u.TurnosPropios++;
            CasillasMovidas = 0;
            ActuoEsteTurno = false;
            u.QuitarEstado(EstadoTipo.DefensaMas);   // "hasta su próximo turno"

            if (u.EsGato)
            {
                int dm = (Efecto(EfectoParty.MenteClara) ? 1 : 0) - (Efecto(EfectoParty.Sed) ? 1 : 0);
                u.Mana = Math.Max(0, Math.Min(u.ManaMax, u.Mana + Config.ManaPorTurno + dm + u.Esp(Especial.ManaTurno)));
            }

            // Efectos del terreno al empezar el turno
            Terreno t = Tab.TerrenoEn(u.Pos);
            if (t == Terreno.Flores) Curar(u, Config.CuraFlores, "flores");
            if (u.Pasiva == Pasiva.HijoDelBosque && (t == Terreno.HierbaAlta || t == Terreno.Flores)) Curar(u, 1, "Hijo del bosque");
            if (u.Esp(Especial.Regenera) > 0) Curar(u, u.Esp(Especial.Regenera), "regeneración");
            EstadoActivo veneno = u.Estado(EstadoTipo.Envenenado);
            if (veneno != null) DanoDirecto(u, Math.Max(1, veneno.Valor), "el veneno", Elemento.Veneno);
            Zona z = u.Vivo ? Tab.ZonaEn(u.Pos) : null;
            if (z != null)
            {
                if (z.Tipo == ZonaTipo.Fuego)
                {
                    if (!(u.Def != null && u.Def.Resiste == Elemento.Fuego)) DanoDirecto(u, Config.DanoFuego, "el fuego", Elemento.Fuego);
                }
                else if (z.Tipo == ZonaTipo.Veneno)
                {
                    if (z.Dueno != u.Bando && !(u.Def != null && u.Def.Resiste == Elemento.Veneno))
                    {
                        u.PonerEstado(EstadoTipo.Envenenado, Config.TurnosVeneno, Config.DanoVeneno, null);
                        Emitir(new Evento { Tipo = EvTipo.Texto, Obj = u, Txt = "¡Envenenado!" });
                    }
                }
                else if (z.Tipo == ZonaTipo.Frasco)
                {
                    if (z.Dueno != u.Bando) DanoDirecto(u, Config.DanoFrascoEnemigo, "el frasco");
                    else Curar(u, Config.CuraFrascoAliado, "el frasco");
                }
            }
            if (u.Def != null && u.Def.Id == "jefe2" && u.Vivo && HayHierbaAlta()) Curar(u, 2, "el bosque");

            MovimientoRestante = MovimientoTotal(u);
            if (EnredaderaHostil(u, u.Pos)) MovimientoRestante = Math.Min(MovimientoRestante, 1);
            ComprobarFin();
        }

        private bool HayHierbaAlta()
        {
            for (int x = 0; x < Tab.Ancho; x++)
                for (int y = 0; y < Tab.Alto; y++)
                    if (Tab.TerrenoEn(new Pos(x, y)) == Terreno.HierbaAlta) return true;
            return false;
        }

        private void TerminarTurno(Unidad u)
        {
            if (u.Vivo && u.Pasiva == Pasiva.Aura)
            {
                foreach (Pos q in Tab.Vecinos(u.Pos))
                {
                    Unidad v = UnidadEn(q);
                    if (v != null && v.Bando == u.Bando) Curar(v, 1, "Aura");
                }
            }
            for (int i = u.Estados.Count - 1; i >= 0; i--)
            {
                EstadoActivo e = u.Estados[i];
                if (e.Tipo == EstadoTipo.DefensaMas) continue;   // se quita al empezar su próximo turno
                e.Turnos--;
                if (e.Turnos <= 0) u.Estados.RemoveAt(i);
            }
            Activa = null;
        }

        private void ComprobarFin()
        {
            if (Estado != EstadoCombate.EnCurso) return;
            if (Vivos(Bando.Enemigos).Count == 0) Terminar(EstadoCombate.Victoria);
            else if (Vivos(Bando.Gatos).Count == 0) Terminar(EstadoCombate.Derrota);
        }

        private void Terminar(EstadoCombate e)
        {
            if (Estado != EstadoCombate.EnCurso) return;
            Estado = e;
            if (_rondaAbierta) { TickEfectos(); _rondaAbierta = false; }
            foreach (Unidad u in Unidades)
                if (u.Gato != null) u.Gato.Vida = Math.Max(0, u.Vida);
            Registrar(e == EstadoCombate.Victoria ? "¡Victoria!" : "Derrota: cayeron los 4 gatos.");
            Emitir(new Evento { Tipo = EvTipo.Fin, N = e == EstadoCombate.Victoria ? 1 : 0 });
        }

        // ------------------------------------------------------------------ daño y curación
        public int CalcularDano(Unidad objetivo, int bruto)
        {
            return CalcularDano(objetivo, bruto, Elemento.Fisico, false);
        }

        /// <summary>Daño final: elemento (débil +50%, resiste -50%, rayo en el agua), defensa y reducciones.</summary>
        public int CalcularDano(Unidad objetivo, int bruto, Elemento elem, bool cuerpoACuerpo, int perfora = 0)
        {
            bruto = ModificarElemento(objetivo, bruto, elem);
            int d = bruto - Math.Max(0, objetivo.DefensaEfectiva - perfora) + ModRecibido(objetivo);
            if (cuerpoACuerpo && objetivo.Pasiva == Pasiva.Inamovible) d -= 1;
            return Math.Max(Config.DanoMinimo, d);
        }

        public int ModificarElemento(Unidad objetivo, int bruto, Elemento elem)
        {
            if (elem == Elemento.Fisico) return bruto;
            if (objetivo.Def != null)
            {
                if (objetivo.Def.Debil == elem) bruto += Math.Max(1, bruto / 2);
                else if (objetivo.Def.Resiste == elem) bruto = bruto / 2;
            }
            if (elem == Elemento.Rayo && Tab.TerrenoEn(objetivo.Pos) == Terreno.Agua) bruto += Config.BonusRayoEnAgua;
            return bruto;
        }

        public bool Resiste(Unidad u, Elemento e) { return u.Def != null && u.Def.Resiste == e; }

        /// <summary>Daño de un ataque. La hierba alta da 25% de esquivar (si 'esquivable').</summary>
        public int Danar(Unidad atacante, Unidad objetivo, int bruto, bool esquivable, Elemento elem = Elemento.Fisico, bool fuegoAmigo = false)
        {
            if (!objetivo.Vivo) return 0;
            int pEsq = esquivable ? objetivo.Esp(Especial.Esquiva) : 0;
            bool esqHierba = esquivable && Tab.TerrenoEn(objetivo.Pos) == Terreno.HierbaAlta && Azar.Prob(Config.EsquivaHierbaAlta);
            if (esqHierba || (pEsq > 0 && Azar.Prob(pEsq / 100.0)))
            {
                Registrar(objetivo.Nombre + (esqHierba ? " esquiva el ataque entre la hierba alta." : " esquiva el ataque."));
                Emitir(new Evento { Tipo = EvTipo.Esquiva, U = atacante, Obj = objetivo });
                return 0;
            }
            bool melee = atacante != null && atacante.Pos.Dist(objetivo.Pos) == 1;
            int d = CalcularDano(objetivo, bruto + (atacante != null ? ModInfligido(atacante) : 0), elem, melee, atacante != null ? atacante.Esp(Especial.Perfora) : 0);
            if (fuegoAmigo) d = Math.Max(Config.DanoMinimo, d / 2);   // fuego amigo: la mitad del daño
            objetivo.Vida -= d;
            if (atacante != null && atacante.Bando != objetivo.Bando) objetivo.UltimoAtacante = atacante;
            Registrar((atacante != null ? atacante.Nombre : "?") + " hace " + d + " de daño a " + objetivo.Nombre + (fuegoAmigo ? " (fuego amigo)." : "."));
            Emitir(new Evento { Tipo = EvTipo.Dano, U = atacante, Obj = objetivo, N = d, Elem = elem });
            if (objetivo.Vida <= 0) Morir(objetivo);
            else
            {
                if (!fuegoAmigo) AplicarElemento(atacante, objetivo, elem);
                int ep = objetivo.Esp(Especial.Espinas);
                if (ep > 0 && melee && atacante != null && atacante.Bando != objetivo.Bando && atacante.Vivo)
                    DanoDirecto(atacante, ep, "las espinas de " + objetivo.Nombre);
            }
            return d;
        }

        /// <summary>Efecto secundario del elemento: el hielo ralentiza (o congela en el agua) y el veneno envenena.</summary>
        private void AplicarElemento(Unidad atacante, Unidad objetivo, Elemento elem)
        {
            if (elem == Elemento.Hielo && !Resiste(objetivo, Elemento.Hielo))
            {
                if (Tab.TerrenoEn(objetivo.Pos) == Terreno.Agua)
                {
                    objetivo.PonerEstado(EstadoTipo.Inmovilizado, 1, 0, atacante);
                    Emitir(new Evento { Tipo = EvTipo.Texto, Obj = objetivo, Txt = "¡Congelado!" });
                }
                else
                {
                    objetivo.PonerEstado(EstadoTipo.Ralentizado, Config.TurnosRalentizado, 0, atacante);
                    Emitir(new Evento { Tipo = EvTipo.Texto, Obj = objetivo, Txt = "¡Ralentizado!" });
                }
            }
            else if (elem == Elemento.Veneno && !Resiste(objetivo, Elemento.Veneno))
            {
                objetivo.PonerEstado(EstadoTipo.Envenenado, Config.TurnosVeneno, Config.DanoVeneno, atacante);
                Emitir(new Evento { Tipo = EvTipo.Texto, Obj = objetivo, Txt = "¡Envenenado!" });
            }
        }

        /// <summary>Daño que ignora la defensa (fuego, frasco, tormenta, veneno, choque...).</summary>
        public void DanoDirecto(Unidad objetivo, int d, string fuente, Elemento elem = Elemento.Fisico)
        {
            if (!objetivo.Vivo) return;
            objetivo.Vida -= d;
            Registrar(objetivo.Nombre + " recibe " + d + " de daño por " + fuente + ".");
            Emitir(new Evento { Tipo = EvTipo.Dano, Obj = objetivo, N = d, Txt = fuente, Elem = elem });
            if (objetivo.Vida <= 0) Morir(objetivo);
        }

        public int Curar(Unidad u, int n, string fuente)
        {
            if (!u.Vivo || n <= 0) return 0;
            int antes = u.Vida;
            u.Vida = Math.Min(u.VidaMax, u.Vida + n);
            int real = u.Vida - antes;
            if (real > 0)
            {
                Registrar(u.Nombre + " recupera " + real + " de vida (" + fuente + ").");
                Emitir(new Evento { Tipo = EvTipo.Cura, Obj = u, N = real, Txt = fuente });
            }
            return real;
        }

        private void Morir(Unidad u)
        {
            u.Vida = 0;
            Registrar(u.Nombre + " cae.");
            Emitir(new Evento { Tipo = EvTipo.Muerte, U = u });
            if (u.Gato != null) u.Gato.Vida = 0;
            if (u.EsGato) return;
            AlDerrotar(u);
            // Dinero botado: lo que llevaba un ladrón; los jefes sueltan 5; los demás a veces sueltan 1 o 2 (los invocados no).
            if (u.Monedas > 0) { SoltarMonedas(u.Pos, u.Monedas); u.Monedas = 0; }
            else if (u.Def != null && u.Def.EsJefe) SoltarMonedas(u.Pos, 5);
            else if (u.Dueno == null && Azar.Prob(Config.ProbMonedaEnemigo)) SoltarMonedas(u.Pos, Azar.Entre(1, 3));
            if (u.Def != null && u.Def.ExplotaAlMorir > 0) Explotar(u.Pos, u.Def.ExplotaAlMorir, Elemento.Fuego);
        }

        /// <summary>Premios por derrotar a un enemigo: experiencia para el gato que lo dejó caer, y vampirismo / maná de sus objetos.</summary>
        private void AlDerrotar(Unidad enemigo)
        {
            Unidad k = enemigo.UltimoAtacante;
            if (k == null || !k.EsGato) return;
            int xp = enemigo.Dueno != null || (enemigo.Def != null && enemigo.Def.Invocado) ? 0 : (enemigo.Def != null && enemigo.Def.EsJefe ? Config.XpJefe : Config.XpPorEnemigo);
            if (xp > 0 && k.Gato != null)
            {
                int ya; XpGanada.TryGetValue(k.Gato, out ya);
                XpGanada[k.Gato] = ya + xp;
            }
            if (!k.Vivo) return;
            int vamp = k.Esp(Especial.Vampirismo);
            if (vamp > 0) Curar(k, vamp, "vampirismo");
            int mk = k.Esp(Especial.ManaKill);
            if (mk > 0) k.Mana = Math.Min(k.ManaMax, k.Mana + mk);
        }

        /// <summary>Herramienta de pruebas (menú de desarrollador): cae todo el bando enemigo y el combate se gana. El primer gato vivo se lleva la experiencia.</summary>
        public void ForzarVictoria()
        {
            if (Estado != EstadoCombate.EnCurso) return;
            Unidad k = null;
            foreach (Unidad u in Vivos(Bando.Gatos)) { k = u; break; }
            foreach (Unidad e in new List<Unidad>(Vivos(Bando.Enemigos)))
            {
                e.UltimoAtacante = k;
                Matar(e);
            }
        }

        /// <summary>Mata a una unidad sin daño (el gato bomba que explota).</summary>
        public void Matar(Unidad u)
        {
            if (!u.Vivo) return;
            Morir(u);
            ComprobarFin();
        }

        // ------------------------------------------------------------------ monedas, explosiones y obstáculos
        public void SoltarMonedas(Pos p, int n)
        {
            if (n <= 0) return;
            Tab.PonerMonedas(p, n);
            Emitir(new Evento { Tipo = EvTipo.Moneda, B = p, N = n });
        }

        /// <summary>Si en la casilla hay monedas, las recoge el gato (suman al dinero) o el ladrón (las lleva encima).</summary>
        private void Recoger(Unidad u, Pos p)
        {
            int n = Tab.MonedasEn(p);
            if (n <= 0 || !u.Vivo) return;
            if (u.EsGato)
            {
                Tab.TomarMonedas(p);
                Dinero += n;
                Registrar(u.Nombre + " recoge " + n + " moneda(s).");
                Emitir(new Evento { Tipo = EvTipo.Recoger, U = u, B = p, N = n });
            }
            else if (u.Def != null && u.Def.Ladron)
            {
                Tab.TomarMonedas(p);
                u.Monedas += n;
                Registrar(u.Nombre + " roba " + n + " moneda(s).");
                Emitir(new Evento { Tipo = EvTipo.Recoger, U = u, B = p, N = n });
            }
        }

        /// <summary>Prende la hierba alta de las casillas dadas: queda fuego un par de rondas y la hierba desaparece.</summary>
        public void Encender(List<Pos> casillas, Bando dueno, int turnos)
        {
            var enc = new List<Pos>();
            foreach (Pos p in casillas)
                if (Tab.Dentro(p) && Tab.TerrenoEn(p) == Terreno.HierbaAlta)
                {
                    Tab.PonerTerreno(p, Terreno.Suelo);
                    Tab.PonerZona(ZonaTipo.Fuego, p, turnos, dueno);
                    enc.Add(p);
                }
            if (enc.Count > 0)
            {
                Registrar("La hierba alta arde.");
                Emitir(new Evento { Tipo = EvTipo.Zona, Zona = ZonaTipo.Fuego, Area = enc });
            }
        }

        /// <summary>Explosión en cruz: daña a todos (también a los enemigos), rompe cajas y barriles vecinos y prende la hierba.</summary>
        public void Explotar(Pos centro, int dano, Elemento elem)
        {
            var area = Tab.Cruz(centro);
            Registrar("¡Explosión!");
            Emitir(new Evento { Tipo = EvTipo.Explosion, B = centro, Area = area, Elem = elem });
            foreach (Pos p in area)
            {
                Unidad o = UnidadEn(p);
                if (o != null) DanoDirecto(o, dano, "la explosión", elem);
                if (Tab.EsRompible(p)) DanarObstaculo(p, dano);
            }
            if (elem == Elemento.Fuego) Encender(area, Bando.Enemigos, 2);
        }

        /// <summary>Daña un obstáculo rompible (caja, roca agrietada, barril). Devuelve true si se rompe.</summary>
        public bool DanarObstaculo(Pos p, int d)
        {
            if (!Tab.EsRompible(p)) return false;
            Terreno t = Tab.TerrenoEn(p);
            if (!Tab.DanarObstaculo(p, d))
            {
                Emitir(new Evento { Tipo = EvTipo.Texto, B = p, Txt = "-" + d });
                return false;
            }
            string nombre = t == Terreno.Caja ? "caja" : (t == Terreno.Barril ? "barril" : "roca");
            Registrar("Se rompe: " + nombre + ".");
            Emitir(new Evento { Tipo = EvTipo.Romper, B = p, Txt = nombre });
            if (t == Terreno.Caja) SoltarMonedas(p, Azar.Entre(1, Config.MonedasCaja + 2));
            if (t == Terreno.Barril) Explotar(p, Config.DanoBarril, Elemento.Fuego);
            return true;
        }

        public List<Pos> TilesRadio(Pos c, int r)
        {
            var l = new List<Pos>();
            for (int x = 0; x < Tab.Ancho; x++)
                for (int y = 0; y < Tab.Alto; y++)
                {
                    Pos p = new Pos(x, y);
                    int d = c.Dist(p);
                    if (d >= 1 && d <= r) l.Add(p);
                }
            return l;
        }

        // ------------------------------------------------------------------ movimiento
        /// <summary>Casillas a las que puede llegar la unidad activa, con el movimiento que le sobra en cada una.</summary>
        public Dictionary<Pos, int> Alcance(Unidad u, int mov, Dictionary<Pos, Pos> previo)
        {
            var rest = new Dictionary<Pos, int>();
            var cola = new Queue<Pos>();
            rest[u.Pos] = mov;
            cola.Enqueue(u.Pos);
            while (cola.Count > 0)
            {
                Pos p = cola.Dequeue();
                int r = rest[p];
                foreach (Pos q in Tab.Vecinos(p))
                {
                    if (!Tab.Pasable(q)) continue;
                    Unidad ocupante = UnidadEn(q);
                    if (ocupante != null && ocupante.Bando != u.Bando) continue;   // no se atraviesa a los enemigos
                    int coste = Tab.CosteEntrar(q);
                    if (u.Pasiva == Pasiva.HijoDelBosque && coste > 1) coste = 1;
                    if (r < coste) continue;
                    int nr = r - coste;
                    if (EnredaderaHostil(u, q)) nr = Math.Min(nr, 1);
                    int actual;
                    if (!rest.TryGetValue(q, out actual) || actual < nr)
                    {
                        rest[q] = nr;
                        if (previo != null) previo[q] = p;
                        cola.Enqueue(q);
                    }
                }
            }
            return rest;
        }

        /// <summary>Casillas libres a las que puede moverse la unidad activa ahora.</summary>
        public List<Pos> CasillasDeMovimiento()
        {
            var l = new List<Pos>();
            if (Activa == null || Estado != EstadoCombate.EnCurso) return l;
            foreach (var kv in Alcance(Activa, MovimientoRestante, null))
                if (kv.Key != Activa.Pos && UnidadEn(kv.Key) == null) l.Add(kv.Key);
            return l;
        }

        /// <summary>Mueve a la unidad activa por el camino que le deja más movimiento. Devuelve false si no puede llegar.</summary>
        public bool Mover(Pos destino)
        {
            if (Config.Medidor == null) return MoverInterno(destino);
            long t0 = System.Diagnostics.Stopwatch.GetTimestamp();
            bool r = MoverInterno(destino);
            Config.Medidor(Config.MedMover, System.Diagnostics.Stopwatch.GetTimestamp() - t0);
            return r;
        }

        private bool MoverInterno(Pos destino)
        {
            if (Activa == null || Estado != EstadoCombate.EnCurso) return false;
            Unidad u = Activa;
            var previo = new Dictionary<Pos, Pos>();
            var rest = Alcance(u, MovimientoRestante, previo);
            int sobra;
            if (!rest.TryGetValue(destino, out sobra) || destino == u.Pos || UnidadEn(destino) != null) return false;
            int pasos = 0;
            Pos p = destino;
            while (p != u.Pos && pasos < 200) { p = previo[p]; pasos++; }
            var camino = new List<Pos>();
            p = destino;
            int g2 = 0;
            while (p != u.Pos && g2++ < 200) { camino.Insert(0, p); p = previo[p]; }
            Pos origen = u.Pos;
            Pos anterior = camino.Count >= 2 ? camino[camino.Count - 2] : origen;
            u.Pos = destino;
            u.Dir = new Pos(Math.Sign(destino.X - anterior.X), Math.Sign(destino.Y - anterior.Y));
            MovimientoRestante = sobra;
            CasillasMovidas += pasos;
            Emitir(new Evento { Tipo = EvTipo.Mover, U = u, A = origen, B = destino, Camino = camino });
            foreach (Pos q in camino) Recoger(u, q);
            return true;
        }

        /// <summary>Camino que seguiría la unidad activa hasta 'destino' (sin incluir su casilla), o una lista vacía.</summary>
        public List<Pos> Camino(Pos destino)
        {
            var camino = new List<Pos>();
            if (Activa == null) return camino;
            var previo = new Dictionary<Pos, Pos>();
            var rest = Alcance(Activa, MovimientoRestante, previo);
            if (!rest.ContainsKey(destino) || destino == Activa.Pos) return camino;
            Pos p = destino;
            int g = 0;
            while (p != Activa.Pos && g++ < 200) { camino.Insert(0, p); p = previo[p]; }
            return camino;
        }

        // ------------------------------------------------------------------ habilidades
        public int RangoMaxEfectivo(Unidad u, HabilidadDef h)
        {
            int r = h.RangoMax;
            if (u.Pasiva == Pasiva.Francotirador && (h.Efecto == EfectoHab.Golpe || h.Efecto == EfectoHab.AreaCruz)) r += 2;
            if (h.RangoMax >= 2 && h.EsAtaque) r += u.Esp(Especial.RangoMas);
            return r;
        }

        /// <summary>El Francotirador no puede atacar a enemigos pegados a él (rango mínimo 2).</summary>
        public int RangoMinEfectivo(Unidad u, HabilidadDef h)
        {
            int r = h.RangoMin;
            if (u.Pasiva == Pasiva.Francotirador && (h.Efecto == EfectoHab.Golpe || h.Efecto == EfectoHab.AreaCruz)) r = Math.Max(r, 2);
            return r;
        }

        public bool PuedeUsar(Unidad u, HabilidadDef h)
        {
            if (Estado != EstadoCombate.EnCurso || u == null || !u.Vivo || h == null) return false;
            if (h.Efecto == EfectoHab.Ninguno) return false;
            if (h.Movilidad && u.Tiene(EstadoTipo.Inmovilizado)) return false;       // inmovilizado: ni teletransporte ni carrera
            if (u == Activa && ActuoEsteTurno && !h.Movilidad) return false;         // las de movilidad no gastan la acción
            return u.Mana >= CosteDe(u, h);
        }

        /// <summary>Fuego amigo (propuesto): los ataques de área de un gato hieren a la mitad a los demás gatos que pillan,
        /// salvo a quien lo lanza y a quien lleva el Amuleto de lealtad.</summary>
        public bool PagaFuegoAmigo(Unidad atacante, Unidad otro)
        {
            return otro != null && otro != atacante && atacante.EsGato && otro.EsGato && otro.Vivo && otro.Esp(Especial.SinFuegoAmigo) == 0;
        }

        /// <summary>Mejor casilla libre pegada a un enemigo, la más "detrás" de él (según hacia dónde mira), o null.</summary>
        public Pos? LugarTras(Unidad u, Unidad enemigo)
        {
            Pos? mejor = null; int mejorDot = int.MaxValue;
            foreach (Pos q in Tab.Vecinos(enemigo.Pos))
            {
                if (!CasillaLibre(q)) continue;
                int dot = enemigo.Dir.X * (q.X - enemigo.Pos.X) + enemigo.Dir.Y * (q.Y - enemigo.Pos.Y);
                if (dot < mejorDot || (dot == mejorDot && mejor != null && q.Dist(u.Pos) < mejor.Value.Dist(u.Pos))) { mejorDot = dot; mejor = q; }
            }
            return mejor;
        }

        /// <summary>Casillas que se pueden elegir como objetivo de la habilidad.</summary>
        public List<Pos> CasillasObjetivo(Unidad u, HabilidadDef h)
        {
            var l = new List<Pos>();
            int rMin = RangoMinEfectivo(u, h);
            int rMax = RangoMaxEfectivo(u, h);
            switch (h.Efecto)
            {
                case EfectoHab.Ninguno: return l;
                case EfectoHab.AreaAlrededor:
                case EfectoHab.CuraArea:
                    l.Add(u.Pos);
                    return l;
                case EfectoHab.Embestida:
                case EfectoHab.Carga:
                    foreach (Pos d in Tablero.Dirs4)
                    {
                        for (int k = 1; k <= rMax; k++)
                        {
                            Pos p = new Pos(u.Pos.X + d.X * k, u.Pos.Y + d.Y * k);
                            if (!Tab.Dentro(p)) break;
                            Unidad o = UnidadEn(p);
                            if (h.Efecto == EfectoHab.Carga)
                            {
                                if (o != null) { if (o.Bando != u.Bando && k >= Math.Max(2, rMin)) l.Add(p); break; }
                                if (!Tab.Pasable(p)) break;
                            }
                            else
                            {
                                if (o != null || !Tab.Pasable(p)) break;
                                if (k >= rMin) l.Add(p);
                            }
                        }
                    }
                    return l;
            }
            for (int x = 0; x < Tab.Ancho; x++)
                for (int y = 0; y < Tab.Alto; y++)
                {
                    Pos p = new Pos(x, y);
                    int d = u.Pos.Dist(p);
                    if (d < rMin || d > rMax) continue;
                    Unidad o = UnidadEn(p);
                    switch (h.Efecto)
                    {
                        case EfectoHab.Teletransporte:
                            if (o == null && Tab.Pasable(p) && (!h.SoloVegetacion || Tab.TerrenoEn(p) == Terreno.HierbaAlta || Tab.TerrenoEn(p) == Terreno.Flores)) l.Add(p);
                            continue;
                        case EfectoHab.TrasEnemigo:
                            if (o != null && o.Bando != u.Bando && LugarTras(u, o) != null) l.Add(p);
                            continue;
                        case EfectoHab.Intercambio:
                            if (o != null && o != u && o.Bando == u.Bando) l.Add(p);
                            continue;
                    }
                    switch (h.Objetivo)
                    {
                        case ObjetivoHab.Enemigo:
                            if (o != null && o.Bando != u.Bando) l.Add(p);
                            else if (o == null && h.Efecto == EfectoHab.Golpe && u.EsGato && Tab.EsRompible(p)) l.Add(p);   // también se atacan cajas, rocas y barriles
                            break;
                        case ObjetivoHab.Aliado: if (o != null && o.Bando == u.Bando) l.Add(p); break;
                        default: l.Add(p); break;
                    }
                }
            return l;
        }

        /// <summary>Casillas que afectaría la habilidad si se elige este objetivo (para mostrar el área).</summary>
        public List<Pos> AreaDeEfecto(Unidad u, HabilidadDef h, Pos objetivo)
        {
            if (h.Efecto == EfectoHab.AreaCruz) return Tab.Cruz(objetivo);
            if (h.Efecto == EfectoHab.ZonaLinea) return Tab.Linea3(u.Pos, objetivo);
            if (h.Efecto == EfectoHab.AreaAlrededor || h.Efecto == EfectoHab.CuraArea) return TilesRadio(u.Pos, h.Radio);
            var l = new List<Pos>(); l.Add(objetivo); return l;
        }

        /// <summary>Usa la primera habilidad de la unidad activa (compatibilidad).</summary>
        public bool UsarHabilidad(Pos objetivo)
        {
            if (Activa == null) return false;
            return EjecutarHabilidad(Activa, Activa.Habilidad, objetivo, true);
        }

        /// <summary>Usa una de las habilidades de la unidad activa (paga el maná).</summary>
        public bool UsarHabilidad(HabilidadDef h, Pos objetivo)
        {
            if (Activa == null) return false;
            if (Config.Medidor == null) return EjecutarHabilidad(Activa, h, objetivo, true);
            long t0 = System.Diagnostics.Stopwatch.GetTimestamp();
            bool r = EjecutarHabilidad(Activa, h, objetivo, true);
            Config.Medidor(Config.MedHabilidad, System.Diagnostics.Stopwatch.GetTimestamp() - t0);
            return r;
        }

        private List<Pos> LineaRecta(Pos desde, Pos hasta)
        {
            var l = new List<Pos>();
            int dx = Math.Sign(hasta.X - desde.X), dy = Math.Sign(hasta.Y - desde.Y);
            Pos p = desde;
            int g = 0;
            while (p != hasta && g++ < 50) { p = new Pos(p.X + dx, p.Y + dy); l.Add(p); }
            return l;
        }

        public bool EjecutarHabilidad(Unidad u, HabilidadDef h, Pos objetivo, bool pagar)
        {
            if (Estado != EstadoCombate.EnCurso || u == null || h == null) return false;
            if (pagar && !PuedeUsar(u, h)) return false;
            if (!CasillasObjetivo(u, h).Contains(objetivo)) return false;
            if (pagar) { u.Mana -= CosteDe(u, h); }
            if (u == Activa && !h.Movilidad) ActuoEsteTurno = true;

            Registrar(u.Nombre + " usa " + h.Nombre + ".");
            Unidad obj = UnidadEn(objetivo);
            Pos origen = u.Pos;
            if (objetivo != u.Pos && h.Efecto != EfectoHab.Teletransporte && h.Efecto != EfectoHab.Intercambio) Mirar(u, objetivo);
            Emitir(new Evento { Tipo = EvTipo.Habilidad, U = u, Obj = obj, B = objetivo, Hab = h, Area = AreaDeEfecto(u, h, objetivo), Elem = ElementoDe(u, h) });
            Elemento elem = ElementoDe(u, h);
            int canal = u.Pasiva == Pasiva.Canalizar ? 1 : 0;
            int bono = u.StatValor(h.Stat);
            switch (h.Efecto)
            {
                case EfectoHab.Golpe:
                    {
                        int bruto = BrutoGolpe(u, h, obj, CasillasMovidas);
                        if (obj == null)
                        {
                            DanarObstaculo(objetivo, Math.Max(1, bruto + ModInfligido(u)));
                            if (elem == Elemento.Fuego) Encender(Tab.Cruz(objetivo), u.Bando, 2 + canal);
                            break;
                        }
                        bool enAgua = Tab.TerrenoEn(objetivo) == Terreno.Agua;
                        Danar(u, obj, bruto, true, elem);
                        if (elem == Elemento.Fuego) Encender(Tab.Cruz(objetivo), u.Bando, 2 + canal);
                        if (elem == Elemento.Rayo && enAgua) ConducirRayo(u, objetivo, bruto);
                        if (obj.Vivo && h.Aplica.HasValue) obj.PonerEstado(h.Aplica.Value, h.AplicaTurnos, h.AplicaValor, u);
                        if (obj.Vivo && h.EmpujeDist > 0) Empujar(u, obj, h.EmpujeDist + u.Esp(Especial.EmpujeMas));
                        break;
                    }
                case EfectoHab.Carga:
                    {
                        var camino = LineaRecta(u.Pos, objetivo);
                        camino.RemoveAt(camino.Count - 1);           // la última casilla es la del enemigo
                        Pos destino = camino[camino.Count - 1];
                        Emitir(new Evento { Tipo = EvTipo.Embestida, U = u, A = origen, B = destino, Camino = camino });
                        u.Pos = destino;
                        CasillasMovidas += camino.Count;
                        foreach (Pos q in camino) Recoger(u, q);
                        int bruto = BrutoGolpe(u, h, obj, CasillasMovidas);
                        Danar(u, obj, bruto, true, elem);
                        if (obj.Vivo && h.EmpujeDist > 0) Empujar(u, obj, h.EmpujeDist + u.Esp(Especial.EmpujeMas));
                        break;
                    }
                case EfectoHab.AreaCruz:
                    {
                        var casillas = Tab.Cruz(objetivo);
                        int bruto = BrutoGolpe(u, h, null, 0);
                        foreach (Pos p in casillas)
                        {
                            Unidad o = UnidadEn(p);
                            if (o != null && o.Bando != u.Bando) Danar(u, o, bruto, true, elem);
                            else if (PagaFuegoAmigo(u, o)) Danar(u, o, bruto, false, elem, true);
                            if (Tab.EsRompible(p)) DanarObstaculo(p, Math.Max(1, bruto + ModInfligido(u)));
                        }
                        if (elem == Elemento.Fuego) Encender(casillas, u.Bando, 2 + canal);
                        break;
                    }
                case EfectoHab.AreaAlrededor:
                    {
                        var casillas = TilesRadio(u.Pos, h.Radio);
                        int bruto = BrutoGolpe(u, h, null, 0);
                        var objs = new List<Unidad>();
                        var amigos = new List<Unidad>();
                        foreach (Pos p in casillas)
                        {
                            Unidad o = UnidadEn(p);
                            if (o != null && o.Bando != u.Bando) objs.Add(o);
                            else if (PagaFuegoAmigo(u, o)) amigos.Add(o);
                        }
                        Pos centro = u.Pos;
                        objs.Sort(delegate (Unidad a, Unidad b) { return b.Pos.Dist(centro).CompareTo(a.Pos.Dist(centro)); });   // los de más lejos primero
                        foreach (Unidad o in objs)
                        {
                            Danar(u, o, bruto, true, elem);
                            if (o.Vivo && h.EmpujeDist > 0) Empujar(u, o, h.EmpujeDist + u.Esp(Especial.EmpujeMas));
                        }
                        foreach (Unidad o in amigos) Danar(u, o, bruto, false, elem, true);
                        foreach (Pos p in casillas)
                            if (Tab.EsRompible(p)) DanarObstaculo(p, Math.Max(1, bruto + ModInfligido(u)));
                        break;
                    }
                case EfectoHab.Curar:
                    {
                        Curar(obj, h.Poder + bono + u.Esp(Especial.CuraMas), h.Nombre);
                        if (h.QuitaEstado) QuitarEstadoNegativo(obj);
                        break;
                    }
                case EfectoHab.CuraArea:
                    {
                        foreach (Unidad a in Vivos(u.Bando))
                            if (a.Pos.Dist(u.Pos) <= h.Radio) Curar(a, h.Poder + bono + u.Esp(Especial.CuraMas), h.Nombre);
                        break;
                    }
                case EfectoHab.Provocar:
                    {
                        obj.PonerEstado(EstadoTipo.Provocado, 1, 0, u);
                        u.PonerEstado(EstadoTipo.DefensaMas, 1, h.DanoDefensaProp, u);
                        Registrar(obj.Nombre + " debe atacar a " + u.Nombre + " en su próximo turno si puede.");
                        Emitir(new Evento { Tipo = EvTipo.Texto, Obj = obj, Txt = "¡Provocado!" });
                        break;
                    }
                case EfectoHab.ZonaLinea:
                    {
                        int dur = h.ZonaTurnos + canal + u.Esp(Especial.ZonaMas);
                        var zl = Tab.Linea3(u.Pos, objetivo);
                        if (h.Zona == ZonaTipo.Fuego)
                            for (int i = zl.Count - 1; i >= 0; i--) if (Tab.TerrenoEn(zl[i]) == Terreno.Agua) zl.RemoveAt(i);   // el fuego no prende en el agua
                        foreach (Pos p in zl)
                        {
                            if (h.Zona == ZonaTipo.Fuego && Tab.TerrenoEn(p) == Terreno.HierbaAlta) Tab.PonerTerreno(p, Terreno.Suelo);
                            Tab.PonerZona(h.Zona, p, dur, u.Bando);
                        }
                        Emitir(new Evento { Tipo = EvTipo.Zona, Zona = h.Zona, Area = zl });
                        break;
                    }
                case EfectoHab.Teletransporte:
                    {
                        Emitir(new Evento { Tipo = EvTipo.Teletransporte, U = u, A = origen, B = objetivo, Txt = h.Id });
                        u.Pos = objetivo;
                        Recoger(u, objetivo);
                        break;
                    }
                case EfectoHab.TrasEnemigo:
                    {
                        Pos destino = LugarTras(u, obj).Value;
                        Emitir(new Evento { Tipo = EvTipo.Teletransporte, U = u, A = origen, B = destino, Txt = h.Id });
                        u.Pos = destino;
                        Mirar(u, obj.Pos);
                        Recoger(u, destino);
                        break;
                    }
                case EfectoHab.Intercambio:
                    {
                        Emitir(new Evento { Tipo = EvTipo.Teletransporte, U = u, A = origen, B = objetivo, Txt = h.Id });
                        Emitir(new Evento { Tipo = EvTipo.Teletransporte, U = obj, A = objetivo, B = origen, Txt = h.Id });
                        obj.Pos = origen;
                        u.Pos = objetivo;
                        Recoger(u, u.Pos);
                        Recoger(obj, obj.Pos);
                        break;
                    }
                case EfectoHab.Embestida:
                    {
                        var camino = LineaRecta(u.Pos, objetivo);
                        Emitir(new Evento { Tipo = EvTipo.Embestida, U = u, A = origen, B = objetivo, Camino = camino });
                        u.Pos = objetivo;
                        foreach (Pos q in camino) Recoger(u, q);
                        break;
                    }
                case EfectoHab.Escudo:
                    {
                        obj.PonerEstado(EstadoTipo.DefensaMas, Math.Max(1, h.AplicaTurnos), h.AplicaValor, u);
                        Emitir(new Evento { Tipo = EvTipo.Texto, Obj = obj, Txt = "¡Defensa +" + h.AplicaValor + "!" });
                        break;
                    }
                case EfectoHab.Potenciar:
                    {
                        obj.PonerEstado(EstadoTipo.DanoMas, Math.Max(1, h.AplicaTurnos), h.AplicaValor, u);
                        Emitir(new Evento { Tipo = EvTipo.Texto, Obj = obj, Txt = "¡Daño +" + h.AplicaValor + "!" });
                        break;
                    }
            }
            ComprobarFin();
            return true;
        }

        /// <summary>El rayo salta por el agua: los otros enemigos que estén en el agua a 2 casillas o menos reciben la mitad.</summary>
        private void ConducirRayo(Unidad u, Pos centro, int bruto)
        {
            foreach (Unidad o in Vivos(u.Bando == Bando.Gatos ? Bando.Enemigos : Bando.Gatos))
            {
                if (o.Pos == centro || o.Pos.Dist(centro) > 2 || Tab.TerrenoEn(o.Pos) != Terreno.Agua) continue;
                Emitir(new Evento { Tipo = EvTipo.Texto, Obj = o, Txt = "¡Descarga!" });
                Danar(u, o, Math.Max(1, bruto / 2), false, Elemento.Rayo);
            }
        }

        /// <summary>Quita un estado negativo (veneno, inmovilizado, ralentizado); si no hay, quita una maldición de la party (propuesto).</summary>
        public bool QuitarEstadoNegativo(Unidad u)
        {
            EstadoTipo[] orden = { EstadoTipo.Envenenado, EstadoTipo.Inmovilizado, EstadoTipo.Ralentizado };
            foreach (EstadoTipo e in orden)
                if (u.Tiene(e))
                {
                    u.QuitarEstado(e);
                    Registrar(u.Nombre + " ya no está " + Textos.Nombre(e).ToLower() + ".");
                    return true;
                }
            if (u.EsGato)
            {
                for (int i = 0; i < Efectos.Count; i++)
                    if (!Efectos[i].EsBendicion)
                    {
                        Registrar("Se quita la maldición: " + Textos.Nombre(Efectos[i].Tipo) + ".");
                        Efectos.RemoveAt(i);
                        return true;
                    }
            }
            return false;
        }

        public void Empujar(Unidad origen, Unidad objetivo) { Empujar(origen, objetivo, 1); }

        /// <summary>
        /// Empuja al objetivo 'dist' casillas alejándolo del origen. Si choca con un borde, una pared, un obstáculo o
        /// otra unidad, recibe daño por el choque (el otro también). El tanque (Inamovible) no se mueve.
        /// </summary>
        public void Empujar(Unidad origen, Unidad objetivo, int dist)
        {
            if (!objetivo.Vivo || dist <= 0) return;
            if (objetivo.Pasiva == Pasiva.Inamovible) { Registrar(objetivo.Nombre + " es inamovible."); Emitir(new Evento { Tipo = EvTipo.Texto, Obj = objetivo, Txt = "¡Inamovible!" }); return; }
            int dx = Math.Sign(objetivo.Pos.X - origen.Pos.X);
            int dy = Math.Sign(objetivo.Pos.Y - origen.Pos.Y);
            if (dx != 0 && dy != 0) dy = 0;
            if (dx == 0 && dy == 0) return;
            Pos desde = objetivo.Pos, cur = desde, choque = desde;
            int pasos = 0, dmg = 0;
            bool choca = false;
            Unidad otro = null;
            for (int i = 0; i < dist; i++)
            {
                Pos sig = new Pos(cur.X + dx, cur.Y + dy);
                bool bloqueado = !Tab.Dentro(sig) || !Tab.Pasable(sig);
                Unidad v = bloqueado ? null : UnidadEn(sig);
                if (bloqueado || v != null)
                {
                    choca = true; choque = sig; otro = v;
                    dmg = Config.DanoChoque + (dist - i - 1);
                    break;
                }
                cur = sig; pasos++;
            }
            if (pasos > 0)
            {
                objetivo.Pos = cur;
                Registrar(objetivo.Nombre + " es empujado " + pasos + (pasos == 1 ? " casilla." : " casillas."));
                Emitir(new Evento { Tipo = EvTipo.Empuje, Obj = objetivo, A = desde, B = cur });
                Recoger(objetivo, cur);
            }
            if (choca)
            {
                Registrar("¡" + objetivo.Nombre + " choca!");
                Emitir(new Evento { Tipo = EvTipo.Choque, Obj = objetivo, B = choque, N = dmg });
                if (Tab.Dentro(choque) && Tab.EsRompible(choque)) DanarObstaculo(choque, dmg);
                DanoDirecto(objetivo, dmg, "el choque");
                if (otro != null) DanoDirecto(otro, dmg, "el choque");
            }
        }

        // ------------------------------------------------------------------ consumibles
        public int Cantidad(Consumible c)
        {
            int n;
            return Inventario.TryGetValue(c, out n) ? n : 0;
        }

        public bool PuedeUsarConsumible(Unidad u, Consumible c)
        {
            if (Estado != EstadoCombate.EnCurso || u == null || !u.Vivo || !u.EsGato) return false;
            if (u == Activa && ActuoEsteTurno) return false;
            return Cantidad(c) > 0;
        }

        /// <summary>Bombas y frascos: se lanzan a una casilla a rango 1 a 4 y afectan a su cruz.</summary>
        public static bool EsLanzable(Consumible c)
        {
            return c == Consumible.Bomba || c == Consumible.BombaFuego || c == Consumible.BombaHielo || c == Consumible.FrascoVeneno;
        }

        public int RangoMinConsumible(Consumible c) { return EsLanzable(c) ? 1 : 0; }
        public int RangoMaxConsumible(Consumible c) { return EsLanzable(c) ? 4 : 1; }

        public List<Pos> CasillasConsumible(Unidad u, Consumible c)
        {
            var l = new List<Pos>();
            for (int x = 0; x < Tab.Ancho; x++)
                for (int y = 0; y < Tab.Alto; y++)
                {
                    Pos p = new Pos(x, y);
                    int d = u.Pos.Dist(p);
                    if (d < RangoMinConsumible(c) || d > RangoMaxConsumible(c)) continue;
                    if (EsLanzable(c)) { l.Add(p); continue; }
                    Unidad o = UnidadEn(p);
                    if (o == null || o.Bando != u.Bando) continue;
                    if (c == Consumible.Antidoto && !o.TieneEstadoNegativo && !HayMaldicion()) continue;
                    l.Add(p);
                }
            return l;
        }

        private bool HayMaldicion()
        {
            for (int i = 0; i < Efectos.Count; i++) if (!Efectos[i].EsBendicion) return true;
            return false;
        }

        public bool UsarConsumible(Consumible c, Pos objetivo)
        {
            Unidad u = Activa;
            if (u == null || !PuedeUsarConsumible(u, c)) return false;
            if (!CasillasConsumible(u, c).Contains(objetivo)) return false;
            int m = u.Pasiva == Pasiva.Maestria ? 1 : 0;
            Unidad o = UnidadEn(objetivo);
            Registrar(u.Nombre + " usa " + Textos.Nombre(c) + ".");
            Emitir(new Evento { Tipo = EvTipo.Consumible, U = u, Obj = o, B = objetivo, Cons = c, Area = EsLanzable(c) ? Tab.Cruz(objetivo) : new List<Pos> { objetivo } });
            switch (c)
            {
                case Consumible.PocionVida: Curar(o, 8 + m, "poción de vida"); break;
                case Consumible.PocionMana:
                    o.Mana = Math.Min(o.ManaMax, o.Mana + 4 + m);
                    Registrar(o.Nombre + " tiene " + o.Mana + " de maná.");
                    break;
                case Consumible.PocionVelocidad:
                    {
                        int antes = MovimientoTotal(o);
                        o.PonerEstado(EstadoTipo.VelocidadMas, 3, 1 + m, u);
                        if (o == Activa) MovimientoRestante = Math.Max(0, MovimientoRestante + MovimientoTotal(o) - antes);
                        break;
                    }
                case Consumible.Bomba:
                case Consumible.BombaFuego:
                case Consumible.BombaHielo:
                case Consumible.FrascoVeneno:
                    {
                        Elemento e = c == Consumible.BombaFuego ? Elemento.Fuego : (c == Consumible.BombaHielo ? Elemento.Hielo : (c == Consumible.FrascoVeneno ? Elemento.Veneno : Elemento.Fisico));
                        int bruto = (c == Consumible.Bomba ? 5 : (c == Consumible.FrascoVeneno ? 2 : 4)) + m;
                        var cruz = Tab.Cruz(objetivo);
                        foreach (Pos p in cruz)
                        {
                            Unidad v = UnidadEn(p);
                            if (v != null && v.Bando != u.Bando) Danar(u, v, bruto, false, e);
                            else if (PagaFuegoAmigo(u, v)) Danar(u, v, bruto, false, e, true);
                            if (c != Consumible.FrascoVeneno && Tab.EsRompible(p)) DanarObstaculo(p, bruto);
                        }
                        if (e == Elemento.Fuego) Encender(cruz, u.Bando, 2);
                        break;
                    }
                case Consumible.Antidoto: QuitarEstadoNegativo(o); break;
            }
            Inventario[c] = Cantidad(c) - 1;
            ActuoEsteTurno = true;
            ComprobarFin();
            return true;
        }

        // ------------------------------------------------------------------ invocaciones (jefes)
        public Unidad Invocar(EnemigoDef def, Pos pos, Unidad dueno = null)
        {
            Unidad u = GeneradorNivel.CrearEnemigo(def, Mundo, _sigId++);
            u.Pos = pos;
            u.Dueno = dueno;
            Unidades.Add(u);
            Registrar(u.Nombre + " aparece.");
            Emitir(new Evento { Tipo = EvTipo.Invocar, U = u });
            return u;
        }

        /// <summary>Casilla libre más cercana a 'cerca' (distancia máxima 'max'), o null.</summary>
        public Pos? LibreCerca(Pos cerca, int max)
        {
            Pos? mejor = null; int md = 999;
            for (int x = 0; x < Tab.Ancho; x++)
                for (int y = 0; y < Tab.Alto; y++)
                {
                    Pos p = new Pos(x, y);
                    int d = p.Dist(cerca);
                    if (d <= max && d < md && CasillaLibre(p)) { md = d; mejor = p; }
                }
            return mejor;
        }
    }
}
