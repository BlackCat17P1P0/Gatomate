// Gatomate - núcleo del juego (sin dependencias de Unity).
// TODOS los números de balance generales están aquí; los de cada clase, enemigo y objeto están en GameData.cs.
using System.Collections.Generic;

namespace Gatomate.Core
{
    public static class Config
    {
        // ---------- Estructura de la campaña (documento de concepto) ----------
        public const int TableroAncho = 10;
        public const int TableroAlto = 10;
        public const int NivelesPorMundo = 3;          // el jefe cierra el nivel 3
        public const int GatosPorExpedicion = 4;
        public const int VidasPorRun = 3;

        /// <summary>
        /// Mundos que se pueden jugar. El documento pide un mundo para el primer prototipo;
        /// se dejan los 7 jugables para probar la campaña completa. Los jefes 2 a 7 son
        /// provisionales (solo ataque básico, ver GameData.Jefes). Ponlo en 1 para probar solo el mundo 1.
        /// </summary>
        public static int MundosJugables = 7;

        /// <summary>Dificultad base de los desafíos por mundo (documento: 5,5,6,6,7,7,8).</summary>
        public static readonly int[] DificultadPorMundo = { 5, 5, 6, 6, 7, 7, 8 };

        /// <summary>Mundos cuyo jefe da un gato nuevo (documento: 1, 3, 5 y 6).</summary>
        public static readonly int[] MundosConGato = { 1, 3, 5, 6 };

        /// <summary>La vista lo pone en true para que cada combate nuevo registre eventos (animaciones). Las simulaciones lo dejan en false.</summary>
        public static bool GrabarEventos = false;

        /// <summary>Cronómetro opcional (null = apagado, sin costo): recibe (id de la llamada, ticks de Stopwatch). Lo usa la herramienta de evidencia (Herramientas/Evidencia); el juego no lo enciende.</summary>
        public static System.Action<int, long> Medidor;
        public const int MedTerminarTurno = 1, MedMover = 2, MedHabilidad = 3, MedConsumible = 4, MedEmpezar = 5, MedSerializar = 6, MedCargar = 7, MedPlanear = 8;

        /// <summary>Clases que pueden salir al azar. null = las 8.</summary>
        public static HashSet<string> ClasesHabilitadas = null;

        // ---------- Habilidades (propuesto, por confirmar) ----------
        public const int HabilidadesPorGato = 2;       // cada gato aprende 2 habilidades de su clase
        public const int OpcionesDeHabilidad = 3;      // se ofrecen 3 y se queda con 1
        public const int CosteRepetirOpciones = 3;     // monedas por volver a sortear las 3 opciones

        // ---------- Elementos, terreno y obstáculos (propuesto, por confirmar) ----------
        public const int CosteMovBarro = 2;
        public const int CosteMovAgua = 3;
        public const int DanoVeneno = 2;               // por turno, ignora la defensa
        public const int TurnosVeneno = 3;
        public const int TurnosRalentizado = 1;
        public const int MovimientoRalentizado = 2;    // casillas que quita Ralentizado
        public const int BonusRayoEnAgua = 3;
        public const int DanoChoque = 2;               // al chocar al ser empujado (+1 por cada casilla que le faltaba)
        public const int VidaCaja = 4;
        public const int VidaRocaFragil = 8;
        public const int VidaBarril = 3;
        public const int VidaRoca = 16;                // roca normal: el doble que la agrietada
        public const int VidaMuro = 24;                // roca grande: el triple
        public const int DanoBarril = 6;
        public const double ProbMonedaEnemigo = 0.35;  // un enemigo suelta monedas al morir
        public const int MonedasCaja = 2;

        // ---------- Objetos de sinergia (propuesto, por confirmar) ----------
        public const int BonusGema = 2;                // daño extra de una gema con ataques de su elemento
        public const int TurnosExtraZonaGema = 1;      // las gemas de fuego y veneno alargan sus zonas

        // ---------- Dinero y tienda (propuesto, por confirmar) ----------
        public static bool TiendaActiva = true;

        // ---------- Combate ----------
        public const int DanoMinimo = 1;
        public const int ManaPorTurno = 1;
        public const double EsquivaHierbaAlta = 0.25;  // propuesta: el documento no da el porcentaje
        public const int DanoFuego = 2;
        public const int CuraFlores = 2;
        public const int DanoFrascoEnemigo = 3;
        public const int CuraFrascoAliado = 3;
        public const int RondasMaximas = 60;           // seguro contra combates que no terminan

        /// <summary>Tablero: porcentaje máximo de casillas con obstáculo que se acepta.</summary>
        public const int IntentosGenerarTablero = 40;

        // ---------- Entre niveles (propuesta, por confirmar) ----------
        /// <summary>Fracción de la vida máxima que se recupera al empezar cada nivel. Los gatos caídos reviven con esa misma fracción.</summary>
        public const double RecuperacionVidaEntreNiveles = 0.5;

        // ---------- Desafíos de recompensa (descripción aportada por Sergio) ----------
        public const double ProbBase = 0.5;            // con estadística = dificultad
        public const double ProbPorPunto = 0.10;
        public const double ProbMin = 0.05;
        public const double ProbMax = 0.95;
        public const int SuerteNeutra = 5;
        public const double RepetirPorPuntoDeSuerte = 0.10;
        public const int TurnosBendicionMin = 3;
        public const int TurnosBendicionMax = 8;       // máximo incluido
        public const int MaxEquipamientoPorGato = 2;     // un objeto de CUERPO y uno de ARMA

        // ---------- Niveles de gato (propuesto, por confirmar) ----------
        public const int NivelMaximo = 10;
        public const int XpPorEnemigo = 1;               // por enemigo derrotado (los invocados no dan; los jefes dan XpJefe)
        public const int XpJefe = 3;
        public static readonly int[] NivelesHabilidadExtra = { 3, 6 };   // al llegar a estos niveles el gato elige una habilidad más

        // ---------- Reserva y descanso (propuesto, por confirmar) ----------
        public const int CuraReservaPorRonda = 1;        // vida que recupera cada gato de la reserva por cada ronda de combate
        public const int PrecioDescanso = 30;            // monedas: descansar cura toda la vida de todos los gatos
        public const int OpcionesDeRecompensa = 1;
    }
}
