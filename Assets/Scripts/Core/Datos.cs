// Gatomate - núcleo del juego (sin dependencias de Unity).
// Tipos de datos: enumeraciones y definiciones (clases, habilidades, enemigos, mundos).
// El contenido concreto (los números) está en GameData.cs.
using System.Collections.Generic;

namespace Gatomate.Core
{
    public enum Bando { Gatos, Enemigos }

    /// <summary>Terreno base de la casilla. Roca y Muro bloquean el paso.</summary>
    /// Agua cuesta 3 de movimiento, Barro 2. Caja, RocaFragil y Barril bloquean, pero se pueden romper atacándolos.
    public enum Terreno { Suelo, HierbaAlta, Flores, Barro, Roca, Muro, Agua, Caja, RocaFragil, Barril }

    /// <summary>Elemento de un ataque. Fuego prende la hierba, Hielo ralentiza (congela en el agua), Rayo pega más en el agua, Veneno envenena.</summary>
    public enum Elemento { Fisico, Fuego, Hielo, Rayo, Veneno }

    /// <summary>Efecto temporal sobre una casilla (capa aparte del terreno).</summary>
    public enum ZonaTipo { Fuego, Enredadera, Frasco, Veneno }

    public enum StatTipo { Ninguna, Fuerza, Inteligencia, Destreza, Velocidad, Suerte }

    public enum Pasiva { Ninguna, Impulso, Canalizar, Inamovible, Francotirador, Aura, Apunalar, HijoDelBosque, Maestria }

    public enum EfectoHab
    {
        Golpe, AreaCruz, Curar, Provocar, ZonaLinea,
        AreaAlrededor,   // daña (y empuja) a los enemigos alrededor del lanzador
        CuraArea,        // cura a los aliados alrededor del lanzador
        Teletransporte,  // aparece en una casilla libre
        TrasEnemigo,     // aparece detrás de un enemigo
        Intercambio,     // cambia de lugar con un aliado
        Embestida,       // avanza en línea recta por casillas libres
        Carga,           // corre en línea recta hacia un enemigo y lo golpea
        Escudo,          // defensa extra a un aliado
        Potenciar,       // daño extra a un aliado
        Ninguno          // (enemigos que no atacan)
    }

    public enum ObjetivoHab { Enemigo, Aliado, Casilla }

    public enum EstadoTipo { Provocado, Inmovilizado, DefensaMas, VelocidadMas, Envenenado, Ralentizado, DanoMas }

    public enum Consumible { PocionVida, PocionMana, PocionVelocidad, Bomba, Antidoto, BombaFuego, BombaHielo, FrascoVeneno }

    /// <summary>
    /// Los 50 objetos equipables (propuesto, por confirmar). Cada gato lleva 2: uno de CUERPO y uno de ARMA (ver Objetos.cs).
    /// Los 15 primeros son los que ya existían; el orden de este enum no cambia para no romper partidas guardadas.
    /// </summary>
    public enum EquipoTipo
    {
        CollarFuerza, CollarInteligencia, CollarDestreza, BotasCascabel, ChalecoCuero, AmuletoTrebol, CascabelOro,
        // Sinergias elementales: los anillos convierten tus ataques físicos en ese elemento; las gemas potencian ese elemento.
        AnilloFuego, AnilloHielo, AnilloRayo, AnilloVeneno,
        GemaFuego, GemaHielo, GemaRayo, GemaVeneno,
        // Cuerpo (14 nuevos)
        ArmaduraPlacas, CapaSombras, TunicaArcana, CollarEspinas, BrazaleteSalto, ColmilloVampirico, BotasViento, CamisaSeda,
        MantoMusgo, CoronaGato, PetoHierro, CapuchaCazador, MedallonSagrado, AmuletoLealtad,
        // Arma (21 nuevos)
        EspadaAfilada, GarrasHierro, BaculoCristal, ArcoLargo, DagaSangrienta, MartilloPesado, LanzaPerforante, VaritaChispas,
        CetroLlamas, TridenteHielo, BastonTormenta, AguijonVeneno, GuantesBoxeo, EscudoPinchos, CuchillosGemelos, LibroHechizos,
        VaraSanadora, EstacaVampira, HachaDoble, LatigoAgil, PipetaAlquimica
    }

    /// <summary>Bendiciones y maldiciones que afectan a toda la party.</summary>
    public enum EfectoParty { Furia, Debilidad, PielDePiedra, Fragilidad, PiesLigeros, Lentitud, MenteClara, Sed }

    public enum DesafioTipo { Roca, Trampa, Glifo, Cerradura, Altar, Cofre }

    /// <summary>Las ocho estadísticas de un gato.</summary>
    public sealed class Stats
    {
        public int Vida, Defensa, Mana, Velocidad, Fuerza, Inteligencia, Destreza, Suerte;

        public Stats() { }

        public Stats(int vida, int def, int mana, int vel, int fue, int inte, int des, int suerte)
        {
            Vida = vida; Defensa = def; Mana = mana; Velocidad = vel;
            Fuerza = fue; Inteligencia = inte; Destreza = des; Suerte = suerte;
        }

        public Stats Copia()
        {
            return new Stats(Vida, Defensa, Mana, Velocidad, Fuerza, Inteligencia, Destreza, Suerte);
        }

        public int Get(StatTipo t)
        {
            switch (t)
            {
                case StatTipo.Fuerza: return Fuerza;
                case StatTipo.Inteligencia: return Inteligencia;
                case StatTipo.Destreza: return Destreza;
                case StatTipo.Velocidad: return Velocidad;
                case StatTipo.Suerte: return Suerte;
                default: return 0;
            }
        }
    }

    public sealed class HabilidadDef
    {
        public string Id;
        public string Nombre;
        public string Descripcion;
        public ObjetivoHab Objetivo = ObjetivoHab.Enemigo;
        public EfectoHab Efecto = EfectoHab.Golpe;
        public int RangoMin = 1;
        public int RangoMax = 1;
        public int CosteMana;
        public int Poder;
        public StatTipo Stat = StatTipo.Ninguna;   // estadística que se suma al poder
        public int EmpujeDist;                     // casillas que empuja al objetivo (0 = no empuja)
        public bool QuitaEstado;                   // Plegaria
        public int BonusSiAislado;                 // Puñalada
        public int BonusEspalda;                   // daño extra si golpea por la espalda
        public Elemento Elemento = Elemento.Fisico;
        public int Radio;                          // AreaAlrededor / CuraArea: distancia en casillas
        public bool Movilidad;                     // true = no gasta la acción del turno (solo maná)
        public bool Basico;                        // ataque básico de la clase: 0 de maná, y suma solo la mitad de su estadística (propuesto)
        public bool SoloVegetacion;                // Teletransporte: solo a hierba alta o flores
        public EstadoTipo? Aplica;                 // estado que deja en el objetivo
        public int AplicaTurnos;
        public int AplicaValor;
        public ZonaTipo Zona = ZonaTipo.Fuego;     // para ZonaLinea
        public int ZonaTurnos;                     // para ZonaLinea
        public int DanoDefensaProp;                // Provocar: defensa extra al lanzador

        public HabilidadDef Copia() { return (HabilidadDef)MemberwiseClone(); }

        public bool EsAtaque
        {
            get { return Efecto == EfectoHab.Golpe || Efecto == EfectoHab.AreaCruz || Efecto == EfectoHab.AreaAlrededor || Efecto == EfectoHab.Carga; }
        }

        public string Categoria
        {
            get
            {
                if (Movilidad) return "Movilidad";
                return EsAtaque || Efecto == EfectoHab.ZonaLinea ? "Ataque" : "Apoyo";
            }
        }
    }

    public sealed class ClaseDef
    {
        public string Id;
        public string Nombre;
        public string ColorHex;                    // placeholder visual
        public Stats Base;
        public readonly List<HabilidadDef> Pool = new List<HabilidadDef>();   // habilidades que se pueden aprender (cada gato elige 2)
        public Pasiva Pasiva;
        public string PasivaTexto;
        public int Movimiento;                     // casillas que se mueve por turno (propuesto: ágiles más, pesados menos)
        public HabilidadDef Basico;                // ataque básico gratis (0 de maná)

        public HabilidadDef Hab(string id)
        {
            for (int i = 0; i < Pool.Count; i++) if (Pool[i].Id == id) return Pool[i];
            return null;
        }
    }

    public sealed class EnemigoDef
    {
        public string Id;
        public string Nombre;
        public string ColorHex;
        public int Vida;
        public int Defensa;
        public int Movimiento;
        public HabilidadDef Ataque;
        public bool EsJefe;
        public string Nota;                        // texto de las habilidades especiales (para mostrar)
        public string Invoca;                      // id del enemigo que crea
        public int InvocaCada;                     // cada cuántos turnos propios
        public int InvocaMax;                      // máximo de invocados vivos a la vez
        public int ExplotaAlMorir;                 // daño en cruz (fuego) al morir
        public bool Kamikaze;                      // se lanza contra un gato y explota
        public bool Ladron;                        // va a por las monedas del suelo
        public bool Invocado;                      // solo aparece invocado (no sale en los niveles)
        public Elemento Debil = Elemento.Fisico;   // recibe +50% de este elemento (Fisico = ninguno)
        public Elemento Resiste = Elemento.Fisico; // recibe la mitad de este elemento
        /// <summary>El documento no da velocidad a los enemigos; se deriva para que ceil(v/2)+1 = movimiento (propuesto).</summary>
        public int Velocidad { get { return System.Math.Max(0, 2 * (Movimiento - 1)); } }
    }

    public sealed class MundoDef
    {
        public string Nombre;
        public string Ambiente;
        public string ColorHex;
        public string[] TiposEnemigo;
        public int PctHierba, PctFlores, PctBarro, PctRoca;   // % del tablero (propuesto)
        public int PctAgua, PctCaja, PctRocaFragil, PctBarril;
        public int Monedas;                                  // montones de monedas en el suelo
        public int BonusVida, BonusPoder;                    // escalado de enemigos comunes
        public string JefeId;
    }

    public sealed class EstadoActivo
    {
        public EstadoTipo Tipo;
        public int Turnos;       // turnos propios que quedan
        public int Valor;
        public Unidad Fuente;    // quién lo causó (Provocado)
    }

    public sealed class Zona
    {
        public ZonaTipo Tipo;
        public Pos Pos;
        public int Turnos;       // rondas que quedan
        public Bando Dueno;
    }

    public sealed class EfectoActivo
    {
        public EfectoParty Tipo;
        public int Turnos;       // rondas que quedan; siguen contando entre niveles
        public bool EsBendicion { get { return Tipo == EfectoParty.Furia || Tipo == EfectoParty.PielDePiedra || Tipo == EfectoParty.PiesLigeros || Tipo == EfectoParty.MenteClara; } }
        public EfectoActivo Copia() { return new EfectoActivo { Tipo = Tipo, Turnos = Turnos }; }
    }
}
