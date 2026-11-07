// Gatomate - núcleo del juego (sin dependencias de Unity).
// TODO el contenido (clases, habilidades, enemigos, jefes, mundos) está aquí, en un solo lugar,
// tal como pide el documento de concepto. Los valores son los del documento; los marcados
// "propuesto" no estaban definidos allí.
using System.Collections.Generic;

namespace Gatomate.Core
{
    public static class GameData
    {
        public static readonly List<ClaseDef> Clases = new List<ClaseDef>();
        public static readonly Dictionary<string, ClaseDef> ClasePorId = new Dictionary<string, ClaseDef>();
        public static readonly Dictionary<string, EnemigoDef> Enemigos = new Dictionary<string, EnemigoDef>();
        public static readonly List<MundoDef> Mundos = new List<MundoDef>();

        public static ClaseDef Clase(string id) { return ClasePorId[id]; }
        public static EnemigoDef Enemigo(string id) { return Enemigos[id]; }

        private static void AddClase(string id, string nombre, string color, Stats st, int mov, HabilidadDef basico, Pasiva p, string pasivaTxt, params HabilidadDef[] pool)
        {
            var c = new ClaseDef { Id = id, Nombre = nombre, ColorHex = color, Base = st, Movimiento = mov, Basico = basico, Pasiva = p, PasivaTexto = pasivaTxt };
            basico.Basico = true;
            basico.Descripcion = Textos.Descripcion(basico);
            foreach (HabilidadDef h in pool)
            {
                if (string.IsNullOrEmpty(h.Descripcion)) h.Descripcion = Textos.Descripcion(h);
                c.Pool.Add(h);
            }
            Clases.Add(c);
            ClasePorId[id] = c;
        }

        /// <summary>Ataque básico de una clase: 0 de maná, sin acción extra, suma la mitad de su estadística (propuesto, por confirmar).</summary>
        private static HabilidadDef Basico(string id, string nombre, int rMin, int rMax, int poder, StatTipo stat)
        {
            return new HabilidadDef { Id = id, Nombre = nombre, Objetivo = ObjetivoHab.Enemigo, Efecto = EfectoHab.Golpe, RangoMin = rMin, RangoMax = rMax, CosteMana = 0, Poder = poder, Stat = stat };
        }

        /// <summary>Habilidad de un gato. Todas las descripciones se generan solas a partir de estos datos (Textos.Descripcion).</summary>
        private static HabilidadDef H(string id, string nombre, ObjetivoHab obj, EfectoHab ef, int rMin, int rMax, int mana, int poder, StatTipo stat)
        {
            return new HabilidadDef { Id = id, Nombre = nombre, Objetivo = obj, Efecto = ef, RangoMin = rMin, RangoMax = rMax, CosteMana = mana, Poder = poder, Stat = stat };
        }

        private static HabilidadDef Atq(string id, string nombre, int rMin, int rMax, int poder)
        {
            return new HabilidadDef { Id = id, Nombre = nombre, Objetivo = ObjetivoHab.Enemigo, Efecto = EfectoHab.Golpe, RangoMin = rMin, RangoMax = rMax, Poder = poder, Descripcion = "Rango " + (rMin == rMax ? rMax.ToString() : rMin + "-" + rMax) + ", poder " + poder };
        }

        private static HabilidadDef AtqE(string id, string nombre, int rMin, int rMax, int poder, Elemento e, int empuje)
        {
            HabilidadDef h = Atq(id, nombre, rMin, rMax, poder);
            h.Elemento = e; h.EmpujeDist = empuje;
            if (e != Elemento.Fisico) h.Descripcion += ", " + Textos.Nombre(e).ToLower();
            if (empuje > 0) h.Descripcion += ", empuja " + empuje;
            return h;
        }

        private static EnemigoDef AddEnemigo(string id, string nombre, string color, int vida, int def, int mov, HabilidadDef atq, bool jefe, string nota)
        {
            var e = new EnemigoDef { Id = id, Nombre = nombre, ColorHex = color, Vida = vida, Defensa = def, Movimiento = mov, Ataque = atq, EsJefe = jefe, Nota = nota };
            Enemigos[id] = e;
            return e;
        }

        static GameData()
        {
            // ------------------------------------------------------------------ CLASES (documento: estadísticas)
            // Cada clase tiene una reserva de 6 habilidades; cada gato elige 2 (de 3 opciones sorteadas cada vez). Los números de
            // las habilidades nuevas son propuestos, por confirmar. La primera habilidad de cada lista es la del documento.
            //                          Vida Def Maná Vel Fue Int Des Suerte
            AddClase("luchador", "Luchador", "#C0392B", new Stats(24, 1, 3, 4, 6, 1, 3, 3), 4,
                Basico("zarpa", "Zarpa", 1, 1, 2, StatTipo.Fuerza),
                Pasiva.Impulso, "Impulso: +2 de daño si se movió 3 o más casillas antes de atacar",
                new HabilidadDef { Id = "zarpazo", Nombre = "Zarpazo cargado", Objetivo = ObjetivoHab.Enemigo, Efecto = EfectoHab.Golpe, RangoMin = 1, RangoMax = 1, CosteMana = 1, Poder = 4, Stat = StatTipo.Fuerza, EmpujeDist = 1 },
                new HabilidadDef { Id = "carga", Nombre = "Carga", Objetivo = ObjetivoHab.Enemigo, Efecto = EfectoHab.Carga, RangoMin = 2, RangoMax = 4, CosteMana = 2, Poder = 2, Stat = StatTipo.Fuerza, EmpujeDist = 1 },
                new HabilidadDef { Id = "tajo", Nombre = "Tajo giratorio", Objetivo = ObjetivoHab.Casilla, Efecto = EfectoHab.AreaAlrededor, RangoMin = 0, RangoMax = 0, CosteMana = 2, Poder = 2, Stat = StatTipo.Fuerza, Radio = 1 },
                new HabilidadDef { Id = "rugido", Nombre = "Rugido de guerra", Objetivo = ObjetivoHab.Aliado, Efecto = EfectoHab.Potenciar, RangoMin = 0, RangoMax = 2, CosteMana = 1, Aplica = EstadoTipo.DanoMas, AplicaTurnos = 2, AplicaValor = 2 },
                new HabilidadDef { Id = "salto", Nombre = "Salto felino", Objetivo = ObjetivoHab.Casilla, Efecto = EfectoHab.Teletransporte, RangoMin = 2, RangoMax = 3, CosteMana = 2, Movilidad = true },
                new HabilidadDef { Id = "demoledor", Nombre = "Golpe demoledor", Objetivo = ObjetivoHab.Enemigo, Efecto = EfectoHab.Golpe, RangoMin = 1, RangoMax = 1, CosteMana = 3, Poder = 6, Stat = StatTipo.Fuerza, EmpujeDist = 2 });

            AddClase("mago", "Mago", "#8E44AD", new Stats(16, 0, 8, 3, 1, 6, 3, 4), 3,
                Basico("chispa", "Chispa", 1, 3, 1, StatTipo.Inteligencia),
                Pasiva.Canalizar, "Canalizar: +1 de daño con fuego, hielo y rayo; sus zonas duran 1 turno más",
                new HabilidadDef { Id = "bola", Nombre = "Bola de fuego", Objetivo = ObjetivoHab.Casilla, Efecto = EfectoHab.AreaCruz, RangoMin = 1, RangoMax = 4, CosteMana = 3, Poder = 3, Stat = StatTipo.Inteligencia, Elemento = Elemento.Fuego },
                new HabilidadDef { Id = "rayo", Nombre = "Rayo", Objetivo = ObjetivoHab.Enemigo, Efecto = EfectoHab.Golpe, RangoMin = 2, RangoMax = 5, CosteMana = 3, Poder = 4, Stat = StatTipo.Inteligencia, Elemento = Elemento.Rayo },
                new HabilidadDef { Id = "lanza_hielo", Nombre = "Lanza de hielo", Objetivo = ObjetivoHab.Enemigo, Efecto = EfectoHab.Golpe, RangoMin = 1, RangoMax = 4, CosteMana = 2, Poder = 3, Stat = StatTipo.Inteligencia, Elemento = Elemento.Hielo },
                new HabilidadDef { Id = "parpadeo", Nombre = "Parpadeo", Objetivo = ObjetivoHab.Casilla, Efecto = EfectoHab.Teletransporte, RangoMin = 1, RangoMax = 4, CosteMana = 2, Movilidad = true },
                new HabilidadDef { Id = "onda", Nombre = "Onda de choque", Objetivo = ObjetivoHab.Casilla, Efecto = EfectoHab.AreaAlrededor, RangoMin = 0, RangoMax = 0, CosteMana = 3, Poder = 1, Stat = StatTipo.Inteligencia, Radio = 1, EmpujeDist = 2 },
                new HabilidadDef { Id = "muro_fuego", Nombre = "Muro de fuego", Objetivo = ObjetivoHab.Casilla, Efecto = EfectoHab.ZonaLinea, RangoMin = 2, RangoMax = 4, CosteMana = 3, Zona = ZonaTipo.Fuego, ZonaTurnos = 2 });

            AddClase("tanque", "Tanque", "#7F8C8D", new Stats(30, 3, 3, 2, 5, 1, 2, 3), 2,
                Basico("topetazo", "Topetazo", 1, 1, 2, StatTipo.Fuerza),
                Pasiva.Inamovible, "Inamovible: no puede ser empujado, y recibe 1 de daño menos de ataques cuerpo a cuerpo",
                new HabilidadDef { Id = "provocar", Nombre = "Provocar", Objetivo = ObjetivoHab.Enemigo, Efecto = EfectoHab.Provocar, RangoMin = 1, RangoMax = 99, CosteMana = 1, DanoDefensaProp = 2 },
                new HabilidadDef { Id = "embestida_escudo", Nombre = "Embestida de escudo", Objetivo = ObjetivoHab.Enemigo, Efecto = EfectoHab.Golpe, RangoMin = 1, RangoMax = 1, CosteMana = 2, Poder = 3, Stat = StatTipo.Fuerza, EmpujeDist = 2 },
                new HabilidadDef { Id = "baluarte", Nombre = "Baluarte", Objetivo = ObjetivoHab.Aliado, Efecto = EfectoHab.Escudo, RangoMin = 0, RangoMax = 3, CosteMana = 2, Aplica = EstadoTipo.DefensaMas, AplicaTurnos = 1, AplicaValor = 3 },
                new HabilidadDef { Id = "sacudida", Nombre = "Sacudida", Objetivo = ObjetivoHab.Casilla, Efecto = EfectoHab.AreaAlrededor, RangoMin = 0, RangoMax = 0, CosteMana = 2, Poder = 2, Stat = StatTipo.Fuerza, Radio = 1, EmpujeDist = 1 },
                new HabilidadDef { Id = "protector", Nombre = "Intercambio protector", Objetivo = ObjetivoHab.Aliado, Efecto = EfectoHab.Intercambio, RangoMin = 1, RangoMax = 4, CosteMana = 1, Movilidad = true },
                new HabilidadDef { Id = "arremetida", Nombre = "Arremetida", Objetivo = ObjetivoHab.Casilla, Efecto = EfectoHab.Embestida, RangoMin = 1, RangoMax = 3, CosteMana = 1, Movilidad = true });

            AddClase("cazador", "Cazador", "#27AE60", new Stats(18, 0, 4, 4, 2, 2, 6, 3), 5,
                Basico("tiro_corto", "Tiro corto", 2, 3, 1, StatTipo.Destreza),
                Pasiva.Francotirador, "Francotirador: +2 de rango máximo, pero no puede atacar a enemigos pegados a él (rango mínimo 2)",
                new HabilidadDef { Id = "disparo", Nombre = "Disparo certero", Objetivo = ObjetivoHab.Enemigo, Efecto = EfectoHab.Golpe, RangoMin = 2, RangoMax = 4, CosteMana = 2, Poder = 3, Stat = StatTipo.Destreza },
                new HabilidadDef { Id = "flecha_fuego", Nombre = "Flecha de fuego", Objetivo = ObjetivoHab.Enemigo, Efecto = EfectoHab.Golpe, RangoMin = 2, RangoMax = 4, CosteMana = 3, Poder = 2, Stat = StatTipo.Destreza, Elemento = Elemento.Fuego },
                new HabilidadDef { Id = "flecha_veneno", Nombre = "Flecha venenosa", Objetivo = ObjetivoHab.Enemigo, Efecto = EfectoHab.Golpe, RangoMin = 2, RangoMax = 4, CosteMana = 2, Poder = 1, Stat = StatTipo.Destreza, Elemento = Elemento.Veneno },
                new HabilidadDef { Id = "voltereta", Nombre = "Voltereta", Objetivo = ObjetivoHab.Casilla, Efecto = EfectoHab.Embestida, RangoMin = 1, RangoMax = 3, CosteMana = 1, Movilidad = true },
                new HabilidadDef { Id = "lazo", Nombre = "Trampa de lazo", Objetivo = ObjetivoHab.Casilla, Efecto = EfectoHab.ZonaLinea, RangoMin = 2, RangoMax = 5, CosteMana = 2, Zona = ZonaTipo.Enredadera, ZonaTurnos = 3 },
                new HabilidadDef { Id = "flecha_impacto", Nombre = "Flecha de impacto", Objetivo = ObjetivoHab.Enemigo, Efecto = EfectoHab.Golpe, RangoMin = 2, RangoMax = 4, CosteMana = 3, Poder = 2, Stat = StatTipo.Destreza, EmpujeDist = 1 });

            AddClase("clerigo", "Clérigo", "#F1C40F", new Stats(20, 1, 6, 3, 1, 5, 3, 4), 3,
                Basico("baculo", "Báculo", 1, 2, 1, StatTipo.Inteligencia),
                Pasiva.Aura, "Aura: al terminar su turno cura 1 de vida a los aliados vecinos",
                new HabilidadDef { Id = "plegaria", Nombre = "Plegaria", Objetivo = ObjetivoHab.Aliado, Efecto = EfectoHab.Curar, RangoMin = 0, RangoMax = 3, CosteMana = 3, Poder = 4, Stat = StatTipo.Inteligencia, QuitaEstado = true },
                new HabilidadDef { Id = "castigo", Nombre = "Castigo divino", Objetivo = ObjetivoHab.Enemigo, Efecto = EfectoHab.Golpe, RangoMin = 1, RangoMax = 3, CosteMana = 2, Poder = 3, Stat = StatTipo.Inteligencia },
                new HabilidadDef { Id = "bendicion", Nombre = "Bendición", Objetivo = ObjetivoHab.Aliado, Efecto = EfectoHab.Potenciar, RangoMin = 0, RangoMax = 3, CosteMana = 2, Aplica = EstadoTipo.DanoMas, AplicaTurnos = 2, AplicaValor = 2 },
                new HabilidadDef { Id = "paso_divino", Nombre = "Paso divino", Objetivo = ObjetivoHab.Aliado, Efecto = EfectoHab.Intercambio, RangoMin = 1, RangoMax = 4, CosteMana = 2, Movilidad = true },
                new HabilidadDef { Id = "nova", Nombre = "Nova sagrada", Objetivo = ObjetivoHab.Casilla, Efecto = EfectoHab.CuraArea, RangoMin = 0, RangoMax = 0, CosteMana = 4, Poder = 2, Stat = StatTipo.Inteligencia, Radio = 2 },
                new HabilidadDef { Id = "purificar", Nombre = "Purificar", Objetivo = ObjetivoHab.Aliado, Efecto = EfectoHab.Curar, RangoMin = 0, RangoMax = 3, CosteMana = 1, Poder = 1, Stat = StatTipo.Inteligencia, QuitaEstado = true });

            AddClase("picaro", "Pícaro", "#34495E", new Stats(16, 0, 4, 6, 3, 1, 5, 3), 6,
                Basico("navaja", "Navaja", 1, 1, 2, StatTipo.Destreza),
                Pasiva.Apunalar, "Puñalada trasera: +3 de daño si ataca a un enemigo por la espalda (el lado contrario a hacia donde mira)",
                new HabilidadDef { Id = "punalada", Nombre = "Puñalada", Objetivo = ObjetivoHab.Enemigo, Efecto = EfectoHab.Golpe, RangoMin = 1, RangoMax = 1, CosteMana = 1, Poder = 3, Stat = StatTipo.Destreza, BonusSiAislado = 3 },
                new HabilidadDef { Id = "paso_sombrio", Nombre = "Paso sombrío", Objetivo = ObjetivoHab.Enemigo, Efecto = EfectoHab.TrasEnemigo, RangoMin = 1, RangoMax = 5, CosteMana = 3, Movilidad = true },
                new HabilidadDef { Id = "daga_veneno", Nombre = "Daga envenenada", Objetivo = ObjetivoHab.Enemigo, Efecto = EfectoHab.Golpe, RangoMin = 1, RangoMax = 1, CosteMana = 2, Poder = 2, Stat = StatTipo.Destreza, Elemento = Elemento.Veneno },
                new HabilidadDef { Id = "cuchillos", Nombre = "Cuchillos arrojadizos", Objetivo = ObjetivoHab.Enemigo, Efecto = EfectoHab.Golpe, RangoMin = 2, RangoMax = 3, CosteMana = 2, Poder = 3, Stat = StatTipo.Destreza },
                new HabilidadDef { Id = "pirueta", Nombre = "Pirueta", Objetivo = ObjetivoHab.Casilla, Efecto = EfectoHab.Embestida, RangoMin = 1, RangoMax = 3, CosteMana = 1, Movilidad = true },
                new HabilidadDef { Id = "golpe_bajo", Nombre = "Golpe bajo", Objetivo = ObjetivoHab.Enemigo, Efecto = EfectoHab.Golpe, RangoMin = 1, RangoMax = 1, CosteMana = 3, Poder = 4, Stat = StatTipo.Destreza, BonusEspalda = 4 });

            AddClase("druida", "Druida", "#16A085", new Stats(20, 1, 6, 3, 2, 5, 3, 3), 4,
                Basico("zarza", "Zarza", 1, 2, 1, StatTipo.Inteligencia),
                Pasiva.HijoDelBosque, "Hijo del bosque: recupera 1 de vida al empezar su turno sobre hierba alta o flores, y el barro y el agua no le cuestan movimiento extra",
                new HabilidadDef { Id = "enredadera", Nombre = "Enredadera", Objetivo = ObjetivoHab.Casilla, Efecto = EfectoHab.ZonaLinea, RangoMin = 1, RangoMax = 4, CosteMana = 2, Zona = ZonaTipo.Enredadera, ZonaTurnos = 3 },
                new HabilidadDef { Id = "latigo", Nombre = "Látigo de zarzas", Objetivo = ObjetivoHab.Enemigo, Efecto = EfectoHab.Golpe, RangoMin = 1, RangoMax = 3, CosteMana = 2, Poder = 3, Stat = StatTipo.Inteligencia },
                new HabilidadDef { Id = "brote", Nombre = "Brote curativo", Objetivo = ObjetivoHab.Aliado, Efecto = EfectoHab.Curar, RangoMin = 0, RangoMax = 3, CosteMana = 2, Poder = 3, Stat = StatTipo.Inteligencia },
                new HabilidadDef { Id = "viaje_raiz", Nombre = "Viaje de raíz", Objetivo = ObjetivoHab.Casilla, Efecto = EfectoHab.Teletransporte, RangoMin = 1, RangoMax = 6, CosteMana = 2, Movilidad = true, SoloVegetacion = true },
                new HabilidadDef { Id = "trueno", Nombre = "Llamado del trueno", Objetivo = ObjetivoHab.Casilla, Efecto = EfectoHab.AreaCruz, RangoMin = 2, RangoMax = 4, CosteMana = 3, Poder = 2, Stat = StatTipo.Inteligencia, Elemento = Elemento.Rayo },
                new HabilidadDef { Id = "esporas", Nombre = "Nube de esporas", Objetivo = ObjetivoHab.Casilla, Efecto = EfectoHab.ZonaLinea, RangoMin = 1, RangoMax = 4, CosteMana = 2, Zona = ZonaTipo.Veneno, ZonaTurnos = 3 });

            AddClase("alquimista", "Alquimista", "#E67E22", new Stats(18, 0, 6, 4, 1, 5, 4, 4), 3,
                Basico("probeta", "Probeta", 1, 3, 1, StatTipo.Inteligencia),
                Pasiva.Maestria, "Maestría: las pociones y objetos que usa rinden +1 en su efecto numérico",
                new HabilidadDef { Id = "frasco", Nombre = "Frasco", Objetivo = ObjetivoHab.Casilla, Efecto = EfectoHab.ZonaLinea, RangoMin = 1, RangoMax = 4, CosteMana = 2, Zona = ZonaTipo.Frasco, ZonaTurnos = 2 },
                new HabilidadDef { Id = "incendiario", Nombre = "Frasco incendiario", Objetivo = ObjetivoHab.Casilla, Efecto = EfectoHab.AreaCruz, RangoMin = 1, RangoMax = 4, CosteMana = 3, Poder = 2, Stat = StatTipo.Inteligencia, Elemento = Elemento.Fuego },
                new HabilidadDef { Id = "escarcha", Nombre = "Frasco de escarcha", Objetivo = ObjetivoHab.Casilla, Efecto = EfectoHab.AreaCruz, RangoMin = 1, RangoMax = 4, CosteMana = 3, Poder = 2, Stat = StatTipo.Inteligencia, Elemento = Elemento.Hielo },
                new HabilidadDef { Id = "toxico", Nombre = "Frasco tóxico", Objetivo = ObjetivoHab.Casilla, Efecto = EfectoHab.ZonaLinea, RangoMin = 1, RangoMax = 4, CosteMana = 2, Zona = ZonaTipo.Veneno, ZonaTurnos = 3 },
                new HabilidadDef { Id = "burbuja", Nombre = "Burbuja saltarina", Objetivo = ObjetivoHab.Casilla, Efecto = EfectoHab.Teletransporte, RangoMin = 1, RangoMax = 3, CosteMana = 2, Movilidad = true },
                new HabilidadDef { Id = "elixir", Nombre = "Elixir de fuerza", Objetivo = ObjetivoHab.Aliado, Efecto = EfectoHab.Potenciar, RangoMin = 0, RangoMax = 3, CosteMana = 2, Aplica = EstadoTipo.DanoMas, AplicaTurnos = 2, AplicaValor = 3 });

            // ------------------------------------------------------------------ ENEMIGOS COMUNES (documento)
            AddEnemigo("maton", "Matón", "#B03A2E", 14, 0, 3, Atq("maton_atq", "Golpe", 1, 1, 4), false, "");
            AddEnemigo("arquero", "Arquero", "#AF601A", 10, 0, 3, Atq("arquero_atq", "Flecha", 1, 4, 3), false, "");
            AddEnemigo("bruto", "Bruto", "#6E2C00", 24, 2, 2, AtqE("bruto_atq", "Porrazo", 1, 1, 5, Elemento.Fisico, 1), false, "Su porrazo empuja 1 casilla (propuesto)");
            AddEnemigo("hechicero", "Hechicero", "#6C3483", 10, 0, 2,
                new HabilidadDef { Id = "hechicero_atq", Nombre = "Hechizo", Objetivo = ObjetivoHab.Casilla, Efecto = EfectoHab.AreaCruz, RangoMin = 1, RangoMax = 3, Poder = 3, Descripcion = "Rango 3, poder 3 en la casilla elegida y sus 4 vecinas" }, false, "");
            AddEnemigo("sanador", "Sanador", "#1E8449", 12, 0, 3,
                new HabilidadDef { Id = "sanador_atq", Nombre = "Curar", Objetivo = ObjetivoHab.Aliado, Efecto = EfectoHab.Curar, RangoMin = 0, RangoMax = 3, Poder = 4, Descripcion = "Cura 4 a un aliado a rango 3; no ataca" }, false, "");
            AddEnemigo("acosador", "Acosador", "#D35400", 8, 0, 5, Atq("acosador_atq", "Mordida", 1, 1, 3), false, "");

            // ------------------------------------------------------------------ ENEMIGOS NUEVOS (todos propuestos, por confirmar)
            EnemigoDef nido = AddEnemigo("nido", "Nido de ratas", "#7E5109", 12, 1, 0,
                new HabilidadDef { Id = "nido_atq", Nombre = "Nido", Objetivo = ObjetivoHab.Enemigo, Efecto = EfectoHab.Ninguno, RangoMin = 1, RangoMax = 1, Descripcion = "No ataca ni se mueve" }, false,
                "Invocador: cada 3 turnos suelta una rata (máximo 3 vivas)");
            nido.Invoca = "rata"; nido.InvocaCada = 1; nido.InvocaMax = 6;
            AddEnemigo("rata", "Rata", "#95A5A6", 6, 0, 4, Atq("rata_atq", "Mordisco", 1, 1, 2), false, "Sale de los nidos y los chamanes").Invocado = true;
            EnemigoDef bomb = AddEnemigo("bombardero", "Gato bomba", "#C0392B", 6, 0, 4,
                new HabilidadDef { Id = "bombardero_atq", Nombre = "Explotar", Objetivo = ObjetivoHab.Enemigo, Efecto = EfectoHab.Golpe, RangoMin = 1, RangoMax = 1, Poder = 5, Elemento = Elemento.Fuego, Descripcion = "Corre hacia un gato y explota: 5 de fuego en cruz (daña a todos)" }, false,
                "Explota al llegar a un gato o al morir (5 de fuego en cruz; enciende la hierba)");
            bomb.Kamikaze = true; bomb.ExplotaAlMorir = 5;
            EnemigoDef sf = AddEnemigo("slime_fuego", "Slime de fuego", "#E74C3C", 14, 0, 2, AtqE("slime_fuego_atq", "Llamarada", 1, 1, 3, Elemento.Fuego, 0), false, "Resiste el fuego, débil al hielo");
            sf.Resiste = Elemento.Fuego; sf.Debil = Elemento.Hielo;
            EnemigoDef sh = AddEnemigo("slime_hielo", "Slime de hielo", "#5DADE2", 14, 0, 2, AtqE("slime_hielo_atq", "Escarcha", 1, 1, 3, Elemento.Hielo, 0), false, "Resiste el hielo, débil al fuego");
            sh.Resiste = Elemento.Hielo; sh.Debil = Elemento.Fuego;
            EnemigoDef sapo = AddEnemigo("sapo", "Sapo venenoso", "#6AA84F", 12, 0, 2, AtqE("sapo_atq", "Lengüetazo", 1, 2, 2, Elemento.Veneno, 0), false, "Envenena; resiste el veneno");
            sapo.Resiste = Elemento.Veneno;
            EnemigoDef an = AddEnemigo("anguila", "Anguila eléctrica", "#F4D03F", 12, 0, 3, AtqE("anguila_atq", "Descarga", 1, 3, 3, Elemento.Rayo, 0), false, "Rayo: +3 de daño a quien esté en el agua; resiste el rayo");
            an.Resiste = Elemento.Rayo;
            EnemigoDef cha = AddEnemigo("chaman", "Chamán", "#884EA0", 12, 0, 2, Atq("chaman_atq", "Maleficio", 1, 3, 2), false, "Invocador: cada 4 turnos llama a una rata (máximo 2 vivas)");
            cha.Invoca = "rata"; cha.InvocaCada = 4; cha.InvocaMax = 2;
            EnemigoDef lad = AddEnemigo("ladron", "Rata ladrona", "#7B7D7D", 8, 0, 5, Atq("ladron_atq", "Zarpa", 1, 1, 2), false, "Va a por las monedas del suelo; si muere suelta lo que llevaba");
            lad.Ladron = true;

            // ------------------------------------------------------------------ JEFES (documento; movimiento y ataque básico de 2 y 3 son propuestos)
            AddEnemigo("jefe1", "Gato callejero gigante", "#922B21", 40, 1, 3, Atq("jefe1_atq", "Zarpazo", 1, 1, 6), true,
                "Salto, cada 3 turnos: cae a 4 casillas y hace 4 de daño en la casilla y sus 4 vecinas");
            AddEnemigo("jefe2", "Espíritu del bosque", "#196F3D", 36, 0, 3, Atq("jefe2_atq", "Zarpazo de ramas", 1, 1, 4), true,
                "Raíces, cada 2 turnos: inmoviliza 1 turno a un gato. Se cura 2 por turno mientras haya hierba alta en el tablero. (Ataque básico propuesto: rango 1, poder 4)");
            AddEnemigo("jefe3", "Criatura del lodo", "#6E4B1F", 50, 2, 2, Atq("jefe3_atq", "Golpe de lodo", 1, 1, 5), true,
                "Barro: convierte 4 casillas al azar en barro que frena (cada 3 turnos: propuesto). Al llegar a la mitad de vida invoca 2 lodos pequeños de 8 de vida. (Ataque básico propuesto: rango 1, poder 5)");
            AddEnemigo("jefe4", "Rey de los murciélagos", "#4A235A", 38, 0, 4, Atq("jefe4_atq", "Mordisco", 1, 1, 5), true,
                "Mordisco: rango 1, poder 5, y se cura lo que daña. Cada 3 turnos invoca 2 Acosadores");
            AddEnemigo("jefe5", "Guardián de las ruinas", "#7B7D7D", 60, 3, 2, Atq("jefe5_atq", "Golpe de piedra", 1, 1, 7), true,
                "Golpe de piedra: rango 1, poder 7. Cada 4 turnos levanta 5 casillas de muro");
            AddEnemigo("jefe6", "Bestia de la tormenta", "#2E86C1", 55, 1, 3, Atq("jefe6_atq", "Rayo", 1, 5, 5), true,
                "Rayo: rango 5, poder 5 contra el gato con más vida. Cada 3 turnos, Tormenta: 2 de daño a todos los gatos");
            AddEnemigo("jefe7", "Jefe final", "#17202A", 80, 2, 3, Atq("jefe7_atq", "Zarpazo", 1, 1, 6), true,
                "Rota las habilidades de los jefes 1 a 6, una cada 2 turnos; a mitad de vida usa una cada turno. (Ataque básico propuesto: rango 1, poder 6)");

            // Invocaciones (propuesto: el documento solo da la vida de los lodos pequeños)
            AddEnemigo("lodo", "Lodo pequeño", "#8D6E63", 8, 0, 2, Atq("lodo_atq", "Salpicadura", 1, 1, 3), false, "Invocado por la Criatura del lodo (ataque propuesto: rango 1, poder 3)").Invocado = true;

            // ------------------------------------------------------------------ MUNDOS (temas y porcentajes de terreno: propuesto)
            // Los tipos de enemigo se repiten para dar más peso a unos que a otros. Monedas = montones que se reparten por el tablero.
            Mundos.Add(new MundoDef { Nombre = "Mundo 1", Ambiente = "Tejados de una ciudad", ColorHex = "#34495E", TiposEnemigo = new[] { "maton", "arquero", "acosador", "ladron", "nido" }, PctHierba = 4, PctFlores = 3, PctBarro = 0, PctRoca = 8, PctAgua = 0, PctCaja = 5, PctRocaFragil = 3, PctBarril = 2, Monedas = 5, JefeId = "jefe1" });
            Mundos.Add(new MundoDef { Nombre = "Mundo 2", Ambiente = "Bosque", ColorHex = "#1E5631", TiposEnemigo = new[] { "maton", "hechicero", "sanador", "chaman", "bombardero" }, PctHierba = 22, PctFlores = 6, PctBarro = 0, PctRoca = 5, PctAgua = 6, PctCaja = 3, PctRocaFragil = 2, PctBarril = 2, BonusVida = 8, BonusPoder = 1, Monedas = 5, JefeId = "jefe2" });
            Mundos.Add(new MundoDef { Nombre = "Mundo 3", Ambiente = "Pantano", ColorHex = "#4B5320", TiposEnemigo = new[] { "bruto", "arquero", "sanador", "sapo", "anguila", "nido" }, PctHierba = 12, PctFlores = 2, PctBarro = 10, PctRoca = 4, PctAgua = 14, PctCaja = 3, PctRocaFragil = 2, PctBarril = 2, BonusVida = 12, BonusPoder = 1, Monedas = 6, JefeId = "jefe3" });
            Mundos.Add(new MundoDef { Nombre = "Mundo 4", Ambiente = "Cuevas", ColorHex = "#2C2C54", TiposEnemigo = new[] { "acosador", "bruto", "hechicero", "bombardero", "slime_fuego", "nido" }, PctHierba = 3, PctFlores = 2, PctBarro = 4, PctRoca = 10, PctAgua = 4, PctCaja = 4, PctRocaFragil = 6, PctBarril = 4, BonusVida = 13, BonusPoder = 3, Monedas = 6, JefeId = "jefe4" });
            Mundos.Add(new MundoDef { Nombre = "Mundo 5", Ambiente = "Desierto", ColorHex = "#9C6B30", TiposEnemigo = new[] { "arquero", "hechicero", "maton", "slime_fuego", "ladron", "bombardero" }, PctHierba = 2, PctFlores = 0, PctBarro = 0, PctRoca = 8, PctAgua = 0, PctCaja = 6, PctRocaFragil = 4, PctBarril = 4, BonusVida = 17, BonusPoder = 3, Monedas = 6, JefeId = "jefe5" });
            Mundos.Add(new MundoDef { Nombre = "Mundo 6", Ambiente = "Montañas de hielo", ColorHex = "#5D8AA8", TiposEnemigo = new[] { "bruto", "acosador", "sanador", "slime_hielo", "chaman", "anguila" }, PctHierba = 0, PctFlores = 2, PctBarro = 0, PctRoca = 8, PctAgua = 8, PctCaja = 3, PctRocaFragil = 5, PctBarril = 3, BonusVida = 17, BonusPoder = 4, Monedas = 6, JefeId = "jefe6" });
            Mundos.Add(new MundoDef { Nombre = "Mundo 7", Ambiente = "Mundo final", ColorHex = "#512E5F", TiposEnemigo = new[] { "maton", "arquero", "bruto", "hechicero", "sanador", "acosador", "nido", "bombardero", "chaman", "slime_fuego", "slime_hielo", "anguila" }, PctHierba = 8, PctFlores = 3, PctBarro = 6, PctRoca = 6, PctAgua = 6, PctCaja = 4, PctRocaFragil = 4, PctBarril = 3, BonusVida = 22, BonusPoder = 4, Monedas = 7, JefeId = "jefe7" });
        }
    }
}
