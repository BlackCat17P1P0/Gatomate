// Gatomate - núcleo del juego (sin dependencias de Unity).
// Objetos equipables (propuesto, por confirmar): 2 espacios por gato, CUERPO y ARMA, y 50 objetos con sinergias.
// Cada objeto es un dato (ObjetoDef): estadísticas, elemento y como mucho un efecto especial. Las reglas de los especiales
// están en Combate.cs. Una "afinidad" da +1 a una estadística si el gato es de la clase indicada.
using System;
using System.Collections.Generic;
using System.Text;

namespace Gatomate.Core
{
    public enum SlotObjeto { Cuerpo, Arma }

    /// <summary>Efectos especiales de los objetos (el valor va en ObjetoDef.EspVal).</summary>
    public enum Especial
    {
        Ninguno,
        Espinas,          // devuelve N de daño a quien te golpea cuerpo a cuerpo
        Vampirismo,       // recuperas N de vida al derrotar a un enemigo
        EmpujeMas,        // tus empujes llegan N casillas más
        RangoMas,         // +N de rango máximo en tus ataques a distancia (rango 2 o más)
        CuraMas,          // +N a lo que curas con habilidades
        EspaldaMas,       // +N de daño al atacar por la espalda
        Perfora,          // ignoras N de defensa
        ManaTurno,        // +N de maná al empezar tu turno
        ManaKill,         // +N de maná al derrotar a un enemigo
        ZonaMas,          // tus zonas duran N turnos más
        AreaMas,          // +N de daño en ataques de área
        Esquiva,          // N% de esquivar ataques (como la hierba alta)
        SinFuegoAmigo,    // no recibes daño de los ataques de área de tus aliados
        MovilidadBarata,  // las habilidades de movilidad cuestan N menos de maná
        BasicoMas,        // +N de daño con el ataque básico
        FisicoMas,        // +N de daño con ataques físicos (sin elemento)
        Regenera          // recuperas N de vida al empezar tu turno
    }

    public sealed class ObjetoDef
    {
        public EquipoTipo Id;
        public string Nombre;
        public SlotObjeto Slot;
        public string Icono;                                   // icono base que usa la vista
        public int Vida, Def, Mana, Mov, Vel, Fue, Int, Des, Sue;
        public Elemento Imbuye = Elemento.Fisico;               // tus ataques físicos pasan a este elemento
        public Elemento BonoElem = Elemento.Fisico;             // +BonoElemVal de daño con este elemento
        public int BonoElemVal;
        public Especial Esp = Especial.Ninguno;
        public int EspVal;
        public string AfinClase;                                // clase a la que le sienta mejor
        public StatTipo AfinStat = StatTipo.Ninguna;            // +1 a esta estadística si el gato es de esa clase
    }

    public static class Objetos
    {
        public static readonly List<ObjetoDef> Todos = new List<ObjetoDef>();
        private static readonly Dictionary<EquipoTipo, ObjetoDef> _por = new Dictionary<EquipoTipo, ObjetoDef>();

        private static ObjetoDef A(ObjetoDef d)
        {
            Todos.Add(d); _por[d.Id] = d;
            return d;
        }

        private const SlotObjeto C = SlotObjeto.Cuerpo, W = SlotObjeto.Arma;

        static Objetos()
        {
            int g = Config.BonusGema;
            // ---------------------------------------------------------------- CUERPO (25)
            A(new ObjetoDef { Id = EquipoTipo.CollarFuerza, Nombre = "Collar de fuerza", Slot = C, Icono = "fuerza", Fue = 1 });
            A(new ObjetoDef { Id = EquipoTipo.CollarInteligencia, Nombre = "Collar de inteligencia", Slot = C, Icono = "estrella", Int = 1 });
            A(new ObjetoDef { Id = EquipoTipo.CollarDestreza, Nombre = "Collar de destreza", Slot = C, Icono = "flechaEmpuje", Des = 1 });
            A(new ObjetoDef { Id = EquipoTipo.BotasCascabel, Nombre = "Botas de cascabel", Slot = C, Icono = "bota", Vel = 1, Mov = 1 });
            A(new ObjetoDef { Id = EquipoTipo.ChalecoCuero, Nombre = "Chaleco de cuero", Slot = C, Icono = "escudo", Def = 1 });
            A(new ObjetoDef { Id = EquipoTipo.AmuletoTrebol, Nombre = "Amuleto de trébol", Slot = C, Icono = "moneda", Sue = 1 });
            A(new ObjetoDef { Id = EquipoTipo.CascabelOro, Nombre = "Cascabel de oro", Slot = C, Icono = "corazon", Vida = 4 });
            A(new ObjetoDef { Id = EquipoTipo.GemaFuego, Nombre = "Gema de fuego", Slot = C, Icono = "fuego", BonoElem = Elemento.Fuego, BonoElemVal = g, Esp = Especial.ZonaMas, EspVal = 1 });
            A(new ObjetoDef { Id = EquipoTipo.GemaHielo, Nombre = "Gema de hielo", Slot = C, Icono = "hielo", BonoElem = Elemento.Hielo, BonoElemVal = g });
            A(new ObjetoDef { Id = EquipoTipo.GemaRayo, Nombre = "Gema de rayo", Slot = C, Icono = "rayo", BonoElem = Elemento.Rayo, BonoElemVal = g });
            A(new ObjetoDef { Id = EquipoTipo.GemaVeneno, Nombre = "Gema de veneno", Slot = C, Icono = "veneno", BonoElem = Elemento.Veneno, BonoElemVal = g, Esp = Especial.ZonaMas, EspVal = 1 });
            A(new ObjetoDef { Id = EquipoTipo.ArmaduraPlacas, Nombre = "Armadura de placas", Slot = C, Icono = "escudo", Def = 2, Mov = -1, AfinClase = "tanque", AfinStat = StatTipo.Fuerza });
            A(new ObjetoDef { Id = EquipoTipo.CapaSombras, Nombre = "Capa de sombras", Slot = C, Icono = "calavera", Des = 1, Esp = Especial.EspaldaMas, EspVal = 1, AfinClase = "picaro", AfinStat = StatTipo.Destreza });
            A(new ObjetoDef { Id = EquipoTipo.TunicaArcana, Nombre = "Túnica arcana", Slot = C, Icono = "estrella", Int = 1, Mana = 2, AfinClase = "mago", AfinStat = StatTipo.Inteligencia });
            A(new ObjetoDef { Id = EquipoTipo.CollarEspinas, Nombre = "Collar de espinas", Slot = C, Icono = "calavera", Esp = Especial.Espinas, EspVal = 2, AfinClase = "tanque", AfinStat = StatTipo.Fuerza });
            A(new ObjetoDef { Id = EquipoTipo.BrazaleteSalto, Nombre = "Brazalete saltarín", Slot = C, Icono = "bota", Vel = 1, Esp = Especial.MovilidadBarata, EspVal = 1 });
            A(new ObjetoDef { Id = EquipoTipo.ColmilloVampirico, Nombre = "Colmillo vampírico", Slot = C, Icono = "calavera", Esp = Especial.Vampirismo, EspVal = 2 });
            A(new ObjetoDef { Id = EquipoTipo.BotasViento, Nombre = "Botas del viento", Slot = C, Icono = "bota", Mov = 2 });
            A(new ObjetoDef { Id = EquipoTipo.CamisaSeda, Nombre = "Camisa de seda", Slot = C, Icono = "estrella", Vel = 1, Esp = Especial.Esquiva, EspVal = 15 });
            A(new ObjetoDef { Id = EquipoTipo.MantoMusgo, Nombre = "Manto de musgo", Slot = C, Icono = "hierba", Vida = 2, Esp = Especial.Regenera, EspVal = 1, AfinClase = "druida", AfinStat = StatTipo.Inteligencia });
            A(new ObjetoDef { Id = EquipoTipo.CoronaGato, Nombre = "Corona de gato", Slot = C, Icono = "estrella", Esp = Especial.ManaTurno, EspVal = 1 });
            A(new ObjetoDef { Id = EquipoTipo.PetoHierro, Nombre = "Peto de hierro", Slot = C, Icono = "escudo", Vida = 6, Def = 1, Mov = -1 });
            A(new ObjetoDef { Id = EquipoTipo.CapuchaCazador, Nombre = "Capucha de cazador", Slot = C, Icono = "flechaEmpuje", Des = 1, Esp = Especial.RangoMas, EspVal = 1, AfinClase = "cazador", AfinStat = StatTipo.Destreza });
            A(new ObjetoDef { Id = EquipoTipo.MedallonSagrado, Nombre = "Medallón sagrado", Slot = C, Icono = "corazon", Int = 1, Esp = Especial.CuraMas, EspVal = 2, AfinClase = "clerigo", AfinStat = StatTipo.Inteligencia });
            A(new ObjetoDef { Id = EquipoTipo.AmuletoLealtad, Nombre = "Amuleto de lealtad", Slot = C, Icono = "escudo", Def = 1, Esp = Especial.SinFuegoAmigo, EspVal = 1 });

            // ---------------------------------------------------------------- ARMA (25)
            A(new ObjetoDef { Id = EquipoTipo.AnilloFuego, Nombre = "Anillo de fuego", Slot = W, Icono = "fuego", Imbuye = Elemento.Fuego });
            A(new ObjetoDef { Id = EquipoTipo.AnilloHielo, Nombre = "Anillo de hielo", Slot = W, Icono = "hielo", Imbuye = Elemento.Hielo });
            A(new ObjetoDef { Id = EquipoTipo.AnilloRayo, Nombre = "Anillo de rayo", Slot = W, Icono = "rayo", Imbuye = Elemento.Rayo });
            A(new ObjetoDef { Id = EquipoTipo.AnilloVeneno, Nombre = "Anillo de veneno", Slot = W, Icono = "veneno", Imbuye = Elemento.Veneno });
            A(new ObjetoDef { Id = EquipoTipo.EspadaAfilada, Nombre = "Espada afilada", Slot = W, Icono = "espada", Fue = 1, Esp = Especial.FisicoMas, EspVal = 1 });
            A(new ObjetoDef { Id = EquipoTipo.GarrasHierro, Nombre = "Garras de hierro", Slot = W, Icono = "espada", Esp = Especial.BasicoMas, EspVal = 2 });
            A(new ObjetoDef { Id = EquipoTipo.BaculoCristal, Nombre = "Báculo de cristal", Slot = W, Icono = "estrella", Int = 2, Esp = Especial.AreaMas, EspVal = 1, AfinClase = "mago", AfinStat = StatTipo.Inteligencia });
            A(new ObjetoDef { Id = EquipoTipo.ArcoLargo, Nombre = "Arco largo", Slot = W, Icono = "flechaEmpuje", Des = 1, Esp = Especial.RangoMas, EspVal = 1 });
            A(new ObjetoDef { Id = EquipoTipo.DagaSangrienta, Nombre = "Daga sangrienta", Slot = W, Icono = "espada", Esp = Especial.EspaldaMas, EspVal = 2, AfinClase = "picaro", AfinStat = StatTipo.Destreza });
            A(new ObjetoDef { Id = EquipoTipo.MartilloPesado, Nombre = "Martillo pesado", Slot = W, Icono = "fuerza", Fue = 1, Esp = Especial.EmpujeMas, EspVal = 1, AfinClase = "luchador", AfinStat = StatTipo.Fuerza });
            A(new ObjetoDef { Id = EquipoTipo.LanzaPerforante, Nombre = "Lanza perforante", Slot = W, Icono = "flechaEmpuje", Esp = Especial.Perfora, EspVal = 2 });
            A(new ObjetoDef { Id = EquipoTipo.VaritaChispas, Nombre = "Varita de chispas", Slot = W, Icono = "rayo", Int = 1, Esp = Especial.ManaKill, EspVal = 1 });
            A(new ObjetoDef { Id = EquipoTipo.CetroLlamas, Nombre = "Cetro de llamas", Slot = W, Icono = "fuego", BonoElem = Elemento.Fuego, BonoElemVal = 1, Esp = Especial.AreaMas, EspVal = 1 });
            A(new ObjetoDef { Id = EquipoTipo.TridenteHielo, Nombre = "Tridente de hielo", Slot = W, Icono = "hielo", Vel = 1, BonoElem = Elemento.Hielo, BonoElemVal = 1 });
            A(new ObjetoDef { Id = EquipoTipo.BastonTormenta, Nombre = "Bastón de tormenta", Slot = W, Icono = "rayo", BonoElem = Elemento.Rayo, BonoElemVal = 1, Esp = Especial.RangoMas, EspVal = 1 });
            A(new ObjetoDef { Id = EquipoTipo.AguijonVeneno, Nombre = "Aguijón venenoso", Slot = W, Icono = "veneno", BonoElem = Elemento.Veneno, BonoElemVal = 1, Esp = Especial.FisicoMas, EspVal = 1 });
            A(new ObjetoDef { Id = EquipoTipo.GuantesBoxeo, Nombre = "Guantes de boxeo", Slot = W, Icono = "fuerza", Esp = Especial.EmpujeMas, EspVal = 1, Fue = 1, Des = 1 });
            A(new ObjetoDef { Id = EquipoTipo.EscudoPinchos, Nombre = "Escudo de pinchos", Slot = W, Icono = "escudo", Def = 1, Esp = Especial.Espinas, EspVal = 1, AfinClase = "tanque", AfinStat = StatTipo.Fuerza });
            A(new ObjetoDef { Id = EquipoTipo.CuchillosGemelos, Nombre = "Cuchillos gemelos", Slot = W, Icono = "espada", Des = 1, Esp = Especial.BasicoMas, EspVal = 1, AfinClase = "picaro", AfinStat = StatTipo.Destreza });
            A(new ObjetoDef { Id = EquipoTipo.LibroHechizos, Nombre = "Libro de hechizos", Slot = W, Icono = "estrella", Int = 1, Esp = Especial.ManaTurno, EspVal = 1 });
            A(new ObjetoDef { Id = EquipoTipo.VaraSanadora, Nombre = "Vara sanadora", Slot = W, Icono = "corazon", Esp = Especial.CuraMas, EspVal = 3, AfinClase = "clerigo", AfinStat = StatTipo.Inteligencia });
            A(new ObjetoDef { Id = EquipoTipo.EstacaVampira, Nombre = "Estaca vampírica", Slot = W, Icono = "calavera", Fue = 1, Esp = Especial.Vampirismo, EspVal = 1 });
            A(new ObjetoDef { Id = EquipoTipo.HachaDoble, Nombre = "Hacha de doble filo", Slot = W, Icono = "espada", Def = -1, Esp = Especial.FisicoMas, EspVal = 2, AfinClase = "luchador", AfinStat = StatTipo.Fuerza });
            A(new ObjetoDef { Id = EquipoTipo.LatigoAgil, Nombre = "Látigo ágil", Slot = W, Icono = "bota", Vel = 2, Mov = 1 });
            A(new ObjetoDef { Id = EquipoTipo.PipetaAlquimica, Nombre = "Pipeta alquímica", Slot = W, Icono = "frascoVeneno", Int = 1, Esp = Especial.ZonaMas, EspVal = 1, AfinClase = "alquimista", AfinStat = StatTipo.Inteligencia });
        }

        public static ObjetoDef Def(EquipoTipo e) { return _por[e]; }

        /// <summary>Suma las estadísticas de un objeto (y su afinidad de clase) a unas estadísticas.</summary>
        public static void Aplicar(Stats s, ObjetoDef d, string claseId)
        {
            s.Vida += d.Vida; s.Defensa += d.Def; s.Mana += d.Mana; s.Velocidad += d.Vel;
            s.Fuerza += d.Fue; s.Inteligencia += d.Int; s.Destreza += d.Des; s.Suerte += d.Sue;
            if (d.AfinClase != null && d.AfinClase == claseId)
                switch (d.AfinStat)
                {
                    case StatTipo.Fuerza: s.Fuerza++; break;
                    case StatTipo.Inteligencia: s.Inteligencia++; break;
                    case StatTipo.Destreza: s.Destreza++; break;
                    case StatTipo.Velocidad: s.Velocidad++; break;
                    case StatTipo.Suerte: s.Suerte++; break;
                }
        }

        public static SlotObjeto Slot(EquipoTipo e) { return _por[e].Slot; }

        public static string NombreSlot(SlotObjeto s) { return s == SlotObjeto.Cuerpo ? "Cuerpo" : "Arma"; }

        /// <summary>Descripción corta generada desde los datos del objeto.</summary>
        public static string Descripcion(EquipoTipo e)
        {
            ObjetoDef d = Def(e);
            var p = new List<string>();
            if (d.Vida != 0) p.Add(Signo(d.Vida) + " de vida");
            if (d.Def != 0) p.Add(Signo(d.Def) + " de defensa");
            if (d.Mana != 0) p.Add(Signo(d.Mana) + " de maná");
            if (d.Mov != 0) p.Add(Signo(d.Mov) + " de movimiento");
            if (d.Vel != 0) p.Add(Signo(d.Vel) + " de velocidad");
            if (d.Fue != 0) p.Add(Signo(d.Fue) + " de fuerza");
            if (d.Int != 0) p.Add(Signo(d.Int) + " de inteligencia");
            if (d.Des != 0) p.Add(Signo(d.Des) + " de destreza");
            if (d.Sue != 0) p.Add(Signo(d.Sue) + " de suerte");
            if (d.Imbuye != Elemento.Fisico) p.Add("tus ataques físicos pasan a ser de " + Textos.Nombre(d.Imbuye).ToLower());
            if (d.BonoElem != Elemento.Fisico) p.Add("+" + d.BonoElemVal + " de daño con " + Textos.Nombre(d.BonoElem).ToLower());
            if (d.Esp != Especial.Ninguno) p.Add(TextoEspecial(d.Esp, d.EspVal));
            string t = string.Join(", ", p.ToArray());
            if (t.Length > 0) t = char.ToUpper(t[0]) + t.Substring(1);
            if (d.AfinClase != null)
                t += ". Afín a " + GameData.Clase(d.AfinClase).Nombre + " (+1 " + Textos.Nombre(d.AfinStat).ToLower() + ")";
            return t;
        }

        private static string Signo(int n) { return (n > 0 ? "+" : "") + n; }

        public static string TextoEspecial(Especial e, int n)
        {
            switch (e)
            {
                case Especial.Espinas: return "devuelve " + n + " de daño a quien te golpea cuerpo a cuerpo";
                case Especial.Vampirismo: return "recuperas " + n + " de vida al derrotar a un enemigo";
                case Especial.EmpujeMas: return "tus empujes llegan " + n + " casilla más";
                case Especial.RangoMas: return "+" + n + " de rango en tus ataques a distancia";
                case Especial.CuraMas: return "+" + n + " a lo que curas";
                case Especial.EspaldaMas: return "+" + n + " de daño por la espalda";
                case Especial.Perfora: return "ignoras " + n + " de defensa";
                case Especial.ManaTurno: return "+" + n + " de maná al empezar tu turno";
                case Especial.ManaKill: return "+" + n + " de maná al derrotar a un enemigo";
                case Especial.ZonaMas: return "tus zonas duran " + n + " turno más";
                case Especial.AreaMas: return "+" + n + " de daño en ataques de área";
                case Especial.Esquiva: return n + "% de esquivar ataques";
                case Especial.SinFuegoAmigo: return "no recibes daño de los ataques de área de tus aliados";
                case Especial.MovilidadBarata: return "las habilidades de movilidad cuestan " + n + " menos";
                case Especial.BasicoMas: return "+" + n + " de daño con el ataque básico";
                case Especial.FisicoMas: return "+" + n + " de daño con ataques físicos";
                case Especial.Regenera: return "recuperas " + n + " de vida al empezar tu turno";
                default: return "";
            }
        }
    }
}
