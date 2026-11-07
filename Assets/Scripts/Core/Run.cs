// Gatomate - núcleo del juego (sin dependencias de Unity).
// Run: la campaña. Controla las fases (preparación, combate, recompensa), las vidas, la reserva de gatos,
// el inventario, las bendiciones y el historial para balancear.
using System;
using System.Collections.Generic;

namespace Gatomate.Core
{
    public enum FaseRun
    {
        Preparacion,        // elegir expedición y equipamiento antes del nivel
        Combate,
        ElegirHabilidad,    // un gato nuevo (o de inicio) elige su habilidad entre 3 opciones
        Recompensa,         // un desafío: elegir qué gato lo intenta
        Tienda,             // gastar las monedas entre niveles
        RunPerdida,         // se acabaron las 3 vidas
        CampanaCompletada   // se venció al último jefe jugable
    }

    /// <summary>Una elección de habilidad que falta por hacer (cada gato hace 2).</summary>
    public sealed class PendienteHab
    {
        public Gato Gato;
        public int Numero;      // 1.. (la 1ª y la 2ª al empezar; la 3ª y la 4ª al subir de nivel)
    }

    /// <summary>Algo que se vende en la tienda entre niveles.</summary>
    public sealed class OfertaTienda
    {
        public string Nombre, Descripcion;
        public int Precio;
        public bool Vendido;
        public bool EsConsumible, EsEquipo, EsBanquete, EsDescanso;
        public Consumible Cons;
        public EquipoTipo Equipo;
    }

    public sealed class RegistroNivel
    {
        public int Mundo, Nivel, Semilla, Intento;
        public string Resultado;      // "victoria" / "derrota"
        public int VidasRestantes;
        public int Rondas;
        public string Desafio = "";   // texto del desafío elegido tras la victoria
    }

    public sealed partial class Run
    {
        public readonly int Semilla;
        public readonly Azar Azar;
        public int Vidas = Config.VidasPorRun;
        public int Mundo = 1;
        public int Nivel = 1;
        public int Intento = 1;                                   // intentos del nivel actual
        public FaseRun Fase = FaseRun.Preparacion;

        public readonly List<Gato> Expedicion = new List<Gato>();
        public readonly List<Gato> Reserva = new List<Gato>();
        public readonly Dictionary<Consumible, int> Inventario = new Dictionary<Consumible, int>();
        public readonly List<EquipoTipo> EquipoSinAsignar = new List<EquipoTipo>();
        public List<EfectoActivo> Efectos = new List<EfectoActivo>();

        public int Dinero;                                        // monedas recogidas (propuesto: se gastan en la tienda y para repetir las 3 opciones de habilidad)
        public readonly List<PendienteHab> Pendientes = new List<PendienteHab>();
        public List<HabilidadDef> OpcionesHab = new List<HabilidadDef>();
        public List<OfertaTienda> Tienda = new List<OfertaTienda>();
        private FaseRun _faseTrasHab = FaseRun.Preparacion;

        public Combate Combate;
        public List<Oferta> Ofertas = new List<Oferta>();
        public string Mensaje = "";                               // último mensaje importante para mostrar
        public readonly List<RegistroNivel> Historial = new List<RegistroNivel>();

        private readonly List<string> _clasesPendientes = new List<string>();   // gatos que darán los jefes
        private int _sigGatoId = 1;
        public bool EsTutorial;                                    // la run de tutorial: 3 niveles fijos, sin perder vidas
        private Dictionary<int, int> _vidaSnap = new Dictionary<int, int>();
        private Dictionary<Consumible, int> _invSnap = new Dictionary<Consumible, int>();
        private List<EfectoActivo> _efectosSnap = new List<EfectoActivo>();

        public Run(int semilla)
        {
            Semilla = semilla;
            Azar = new Azar(semilla);
            foreach (Consumible c in Enum.GetValues(typeof(Consumible))) Inventario[c] = 0;

            var clases = new List<string>();
            foreach (ClaseDef cd in GameData.Clases)
                if (Config.ClasesHabilitadas == null || Config.ClasesHabilitadas.Contains(cd.Id)) clases.Add(cd.Id);
            Azar.Mezclar(clases);
            for (int i = 0; i < clases.Count; i++)
            {
                if (i < Config.GatosPorExpedicion) Expedicion.Add(NuevoGato(clases[i]));
                else _clasesPendientes.Add(clases[i]);   // ya vienen en orden aleatorio
            }
            PrepararNivel(false);

            // Al empezar la run cada uno de los 4 gatos elige sus 2 habilidades (3 opciones cada vez).
            foreach (Gato g in Expedicion) EncolarHabilidades(g);
            IniciarEleccionDeHabilidades(FaseRun.Preparacion);
        }

        private Gato NuevoGato(string claseId)
        {
            var g = new Gato { Id = _sigGatoId++, ClaseId = claseId, Nombre = GameData.Clase(claseId).Nombre };
            g.Vida = g.VidaMax;
            return g;
        }

        public IEnumerable<Gato> TodosLosGatos
        {
            get
            {
                foreach (Gato g in Expedicion) yield return g;
                foreach (Gato g in Reserva) yield return g;
            }
        }

        public int SemillaNivel { get { return Azar.Derivar(Semilla, Mundo, Nivel); } }
        public bool EsNivelDeJefe { get { return Nivel >= Config.NivelesPorMundo; } }

        // ------------------------------------------------------------------ preparación
        /// <summary>Entra en la fase de preparación de un nivel: recupera vida (propuesto) y guarda el estado para poder reintentar.</summary>
        private void PrepararNivel(bool curar)
        {
            if (curar)
            {
                foreach (Gato g in TodosLosGatos)
                {
                    int max = g.VidaMax;
                    int cura = (int)Math.Ceiling(max * Config.RecuperacionVidaEntreNiveles);
                    g.Vida = Math.Min(max, g.Vida + cura);
                }
            }
            GuardarEstado();
            Fase = FaseRun.Preparacion;
            Combate = null;
        }

        private void GuardarEstado()
        {
            _vidaSnap = new Dictionary<int, int>();
            foreach (Gato g in TodosLosGatos) _vidaSnap[g.Id] = g.Vida;
            _invSnap = new Dictionary<Consumible, int>(Inventario);
            _efectosSnap = new List<EfectoActivo>();
            foreach (EfectoActivo e in Efectos) _efectosSnap.Add(e.Copia());
        }

        private void RestaurarEstado()
        {
            foreach (Gato g in TodosLosGatos)
            {
                int v;
                g.Vida = _vidaSnap.TryGetValue(g.Id, out v) ? Math.Min(v, g.VidaMax) : g.VidaMax;
            }
            foreach (var kv in _invSnap) Inventario[kv.Key] = kv.Value;
            Efectos = new List<EfectoActivo>();
            foreach (EfectoActivo e in _efectosSnap) Efectos.Add(e.Copia());
        }

        public bool Intercambiar(Gato deExpedicion, Gato deReserva)
        {
            if (Fase != FaseRun.Preparacion) return false;
            int i = Expedicion.IndexOf(deExpedicion), j = Reserva.IndexOf(deReserva);
            if (i < 0 || j < 0) return false;
            Expedicion[i] = deReserva;
            Reserva[j] = deExpedicion;
            return true;
        }

        /// <summary>Equipa un objeto. Cada gato tiene dos espacios (Cuerpo y Arma): si el espacio ya está ocupado, el objeto viejo vuelve al inventario.</summary>
        public bool Equipar(Gato g, EquipoTipo e)
        {
            if (Fase != FaseRun.Preparacion) return false;
            if (!EquipoSinAsignar.Contains(e)) return false;
            SlotObjeto slot = Objetos.Slot(e);
            ObjetoDef viejo = g.Objeto(slot);
            if (viejo != null) { g.Equipo.Remove(viejo.Id); EquipoSinAsignar.Add(viejo.Id); }
            EquipoSinAsignar.Remove(e);
            g.Equipo.Add(e);
            if (g.Vida > g.VidaMax) g.Vida = g.VidaMax;
            return true;
        }

        public bool Desequipar(Gato g, EquipoTipo e)
        {
            if (Fase != FaseRun.Preparacion) return false;
            if (!g.Equipo.Remove(e)) return false;
            EquipoSinAsignar.Add(e);
            if (g.Vida > g.VidaMax) g.Vida = g.VidaMax;
            return true;
        }

        // ------------------------------------------------------------------ combate
        public void Empezar()
        {
            if (Config.Medidor == null) { EmpezarInterno(); return; }
            long t0 = System.Diagnostics.Stopwatch.GetTimestamp();
            EmpezarInterno();
            Config.Medidor(Config.MedEmpezar, System.Diagnostics.Stopwatch.GetTimestamp() - t0);
        }

        private void EmpezarInterno()
        {
            if (Fase != FaseRun.Preparacion) return;
            GuardarEstado();   // por si se cambió la expedición o el equipo
            var efectosCombate = new List<EfectoActivo>();
            foreach (EfectoActivo e in Efectos) efectosCombate.Add(e.Copia());
            Combate = EsTutorial
                ? GeneradorNivel.CrearTutorial(Nivel, Expedicion, efectosCombate, Inventario)
                : GeneradorNivel.Crear(Expedicion, Mundo, Nivel, SemillaNivel, efectosCombate, Inventario);
            Fase = FaseRun.Combate;
            Mensaje = "";
            Combate.Iniciar();
        }

        /// <summary>Se llama cuando el combate terminó (Victoria o Derrota) para pasar a la fase siguiente.</summary>
        public void ResolverFinDeCombate()
        {
            if (Fase != FaseRun.Combate || Combate == null || Combate.Estado == EstadoCombate.EnCurso) return;

            var reg = new RegistroNivel { Mundo = Mundo, Nivel = Nivel, Semilla = SemillaNivel, Intento = Intento, Rondas = Combate.Ronda };

            if (Combate.Estado == EstadoCombate.Victoria)
            {
                Efectos = Combate.Efectos;   // lo que queda de bendiciones y maldiciones sigue en el nivel siguiente
                reg.Resultado = "victoria";
                reg.VidasRestantes = Vidas;
                Historial.Add(reg);
                Mensaje = "Nivel superado.";
                if (Combate.Dinero > 0) { Dinero += Combate.Dinero; Mensaje += " Recogiste " + Combate.Dinero + " monedas."; }
                AplicarExperiencia();
                CurarReserva(Combate.Ronda);

                if (!EsTutorial && EsNivelDeJefe) DarGatoDelJefe();

                if (EsTutorial && Nivel >= NivelesTutorial)
                {
                    Fase = FaseRun.CampanaCompletada;
                    Mensaje += " ¡Tutorial completado!";
                }
                else if (EsNivelDeJefe && Mundo >= Config.MundosJugables)
                {
                    Fase = FaseRun.CampanaCompletada;
                    Mensaje += " ¡Campaña completada!";
                }
                else
                {
                    Ofertas = Desafios.Ofrecer(Azar, Mundo);
                    Fase = FaseRun.Recompensa;
                    if (Pendientes.Count > 0) IniciarEleccionDeHabilidades(FaseRun.Recompensa);   // el gato nuevo elige antes de la recompensa
                }
            }
            else
            {
                if (!EsTutorial) Vidas--;   // en el tutorial no se pierden vidas
                reg.Resultado = "derrota";
                reg.VidasRestantes = Vidas;
                Historial.Add(reg);
                RestaurarEstado();
                if (Vidas <= 0)
                {
                    Fase = FaseRun.RunPerdida;
                    Mensaje = "Se acabaron las vidas: la run termina.";
                }
                else
                {
                    Intento++;
                    Mensaje = EsTutorial ? "Cayeron los 4 gatos. En el tutorial no pierdes vidas: el nivel se reinicia." : "Cayeron los 4 gatos: pierdes una vida (te quedan " + Vidas + "). El nivel se reinicia.";
                    Fase = FaseRun.Preparacion;
                }
            }
            // El último combate queda en 'Combate' para mostrar el resumen; la vista debe mirar 'Fase'.
        }

        /// <summary>Los gatos que dejaron caer enemigos ganan experiencia (solo si el combate se ganó). Si suben a un nivel con habilidad extra, la eligen después.</summary>
        private void AplicarExperiencia()
        {
            SubidasDeNivel.Clear();
            foreach (var kv in Combate.XpGanada)
            {
                Gato g = kv.Key;
                int nivelAntes = g.Nivel;
                List<int> subidos = Progreso.GanarXp(g, kv.Value);
                foreach (int n in subidos) SubidasDeNivel.Add(g.Nombre + " sube al nivel " + n + ".");
                if (subidos.Count > 0) { Mensaje += " " + g.Nombre + " sube al nivel " + g.Nivel + "."; EncolarHabilidades(g); }
            }
        }

        /// <summary>Los gatos en la reserva se curan un poco por cada ronda que dura el combate (propuesto), para poder cambiarlos.</summary>
        private void CurarReserva(int rondas)
        {
            foreach (Gato g in Reserva) g.Vida = Math.Min(g.VidaMax, g.Vida + rondas * Config.CuraReservaPorRonda);
        }

        /// <summary>Nivel de un gato nuevo: la media de la party, o hasta 1 menos (propuesto).</summary>
        public int NivelNuevoGato()
        {
            int suma = 0, n = 0;
            foreach (Gato g in TodosLosGatos) { suma += g.Nivel; n++; }
            int media = n == 0 ? 1 : (int)Math.Round((double)suma / n);
            return Math.Max(1, media - Azar.Entre(0, 2));
        }

        public readonly List<string> SubidasDeNivel = new List<string>();   // avisos de la última victoria

        private void DarGatoDelJefe()
        {
            foreach (int m in Config.MundosConGato)
            {
                if (m != Mundo) continue;
                if (_clasesPendientes.Count == 0) { Mensaje += " (No quedan gatos por conseguir.)"; return; }
                string id = _clasesPendientes[0];
                _clasesPendientes.RemoveAt(0);
                Gato g = NuevoGato(id);
                Progreso.FijarNivel(g, NivelNuevoGato());
                Reserva.Add(g);
                EncolarHabilidades(g);
                Mensaje += " El jefe suelta un gato nuevo: " + g.Nombre + " (pasa a la reserva).";
                return;
            }
        }

        // ------------------------------------------------------------------ recompensa
        /// <summary>Elige un desafío y qué gato de la expedición lo enfrenta. Luego pasa al nivel siguiente.</summary>
        public ResultadoDesafio ElegirDesafio(int indice, Gato gato)
        {
            if (Fase != FaseRun.Recompensa || indice < 0 || indice >= Ofertas.Count || !Expedicion.Contains(gato)) return null;
            Oferta o = Ofertas[indice];
            ResultadoDesafio r = Desafios.Resolver(this, o, gato);
            Mensaje = r.Texto;
            if (Historial.Count > 0) Historial[Historial.Count - 1].Desafio = o.Def.Nombre + (r.Exito ? " (bien)" : " (mal)");
            Ofertas = new List<Oferta>();
            AvanzarNivel();
            return r;
        }

        private void AvanzarNivel()
        {
            if (Nivel >= Config.NivelesPorMundo) { Nivel = 1; Mundo++; }
            else Nivel++;
            Intento = 1;
            if (Config.TiendaActiva) { GenerarTienda(); Fase = FaseRun.Tienda; Combate = null; }
            else PrepararNivel(true);
        }

        // ------------------------------------------------------------------ habilidades (cada gato elige 2, de 3 opciones cada vez)
        private void EncolarHabilidades(Gato g)
        {
            int ya = g.HabilidadIds.Count;
            foreach (PendienteHab p in Pendientes) if (p.Gato == g) ya++;
            for (int n = ya + 1; n <= g.HabilidadesTotales; n++) Pendientes.Add(new PendienteHab { Gato = g, Numero = n });
        }

        private void IniciarEleccionDeHabilidades(FaseRun despues)
        {
            if (Pendientes.Count == 0) return;
            _faseTrasHab = despues;
            Fase = FaseRun.ElegirHabilidad;
            GenerarOpciones();
        }

        public Gato GatoEligiendo { get { return Pendientes.Count > 0 ? Pendientes[0].Gato : null; } }

        private void GenerarOpciones()
        {
            OpcionesHab = new List<HabilidadDef>();
            if (Pendientes.Count == 0) return;
            Gato g = Pendientes[0].Gato;
            var libres = new List<HabilidadDef>();
            foreach (HabilidadDef h in g.Clase.Pool) if (!g.HabilidadIds.Contains(h.Id)) libres.Add(h);
            Azar.Mezclar(libres);
            for (int i = 0; i < libres.Count && i < Config.OpcionesDeHabilidad; i++) OpcionesHab.Add(libres[i]);
        }

        /// <summary>El gato que toca se queda con la habilidad elegida.</summary>
        public bool ElegirHabilidad(int indice)
        {
            if (Fase != FaseRun.ElegirHabilidad || Pendientes.Count == 0 || indice < 0 || indice >= OpcionesHab.Count) return false;
            Gato g = Pendientes[0].Gato;
            g.HabilidadIds.Add(OpcionesHab[indice].Id);
            Pendientes.RemoveAt(0);
            if (Pendientes.Count > 0) GenerarOpciones();
            else
            {
                OpcionesHab = new List<HabilidadDef>();
                if (_faseTrasHab == FaseRun.Preparacion) { PrepararNivel(false); }
                else Fase = _faseTrasHab;
            }
            return true;
        }

        /// <summary>Vuelve a sortear las 3 opciones a cambio de monedas.</summary>
        public bool RepetirOpciones()
        {
            if (Fase != FaseRun.ElegirHabilidad || Dinero < Config.CosteRepetirOpciones) return false;
            Dinero -= Config.CosteRepetirOpciones;
            GenerarOpciones();
            return true;
        }

        // ------------------------------------------------------------------ tienda (propuesto, por confirmar)
        public static int Precio(Consumible c)
        {
            switch (c)
            {
                case Consumible.Antidoto: return 2;
                case Consumible.PocionMana: case Consumible.PocionVelocidad: return 3;
                case Consumible.PocionVida: case Consumible.Bomba: case Consumible.FrascoVeneno: return 4;
                default: return 5;       // bombas de fuego y de hielo
            }
        }

        public const int PrecioEquipo = 7;
        public const int PrecioBanquete = 8;

        private void GenerarTienda()
        {
            if (EsTutorial) { GenerarTiendaTutorial(); return; }
            Tienda = new List<OfertaTienda>();
            var cons = new List<Consumible>();
            foreach (Consumible c in Enum.GetValues(typeof(Consumible))) cons.Add(c);
            Azar.Mezclar(cons);
            for (int i = 0; i < 3; i++)
                Tienda.Add(new OfertaTienda { EsConsumible = true, Cons = cons[i], Nombre = Textos.Nombre(cons[i]), Descripcion = Textos.Descripcion(cons[i]), Precio = Precio(cons[i]) });
            var equipos = new List<EquipoTipo>();
            foreach (EquipoTipo e in Enum.GetValues(typeof(EquipoTipo))) equipos.Add(e);
            EquipoTipo eq = Azar.Elegir(equipos);
            Tienda.Add(new OfertaTienda { EsEquipo = true, Equipo = eq, Nombre = Textos.Nombre(eq), Descripcion = Textos.Descripcion(eq), Precio = PrecioEquipo });
            Tienda.Add(new OfertaTienda { EsBanquete = true, Nombre = "Banquete de pescado", Descripcion = "Cura el 50% de la vida máxima a todos los gatos", Precio = PrecioBanquete });
            Tienda.Add(OfertaDescanso());
        }

        private static OfertaTienda OfertaDescanso()
        {
            return new OfertaTienda { EsDescanso = true, Nombre = "Descansar", Descripcion = "Todos los gatos descansan y recuperan toda su vida", Precio = Config.PrecioDescanso };
        }

        public bool Comprar(int indice)
        {
            if (Fase != FaseRun.Tienda || indice < 0 || indice >= Tienda.Count) return false;
            OfertaTienda o = Tienda[indice];
            if (o.Vendido || Dinero < o.Precio) return false;
            Dinero -= o.Precio;
            o.Vendido = true;
            if (o.EsConsumible) AgregarConsumible(o.Cons);
            else if (o.EsEquipo) EquipoSinAsignar.Add(o.Equipo);
            else if (o.EsBanquete)
                foreach (Gato g in TodosLosGatos)
                    g.Vida = Math.Min(g.VidaMax, g.Vida + (int)Math.Ceiling(g.VidaMax * 0.5));
            else if (o.EsDescanso)
                foreach (Gato g in TodosLosGatos) g.Vida = g.VidaMax;
            return true;
        }

        public void SalirDeTienda()
        {
            if (Fase != FaseRun.Tienda) return;
            Tienda = new List<OfertaTienda>();
            PrepararNivel(true);
        }

        /// <summary>Atajo para pruebas y el bot: elige siempre la primera opción hasta terminar.</summary>
        public void ElegirHabilidadesAutomatico()
        {
            int g = 0;
            while (Fase == FaseRun.ElegirHabilidad && g++ < 100) ElegirHabilidad(0);
        }

        public void AgregarConsumible(Consumible c)
        {
            int n;
            Inventario.TryGetValue(c, out n);
            Inventario[c] = n + 1;
        }

        public void AgregarEfecto(EfectoParty tipo, int turnos)
        {
            foreach (EfectoActivo e in Efectos)
                if (e.Tipo == tipo) { e.Turnos = Math.Max(e.Turnos, turnos); return; }
            Efectos.Add(new EfectoActivo { Tipo = tipo, Turnos = turnos });
        }
    }
}
