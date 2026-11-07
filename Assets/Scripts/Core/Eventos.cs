// Gatomate - núcleo del juego (sin dependencias de Unity).
// Eventos de combate: el núcleo resuelve todo al instante y deja aquí un registro de lo que pasó,
// para que la vista pueda animarlo en orden (mover, atacar, daño, curación...). No cambian las reglas.
using System.Collections.Generic;

namespace Gatomate.Core
{
    public enum EvTipo
    {
        Turno,        // empieza el turno de U
        Mover,        // U se mueve de A a B por Camino
        Habilidad,    // U usa Hab sobre B (Obj si hay unidad), Area = casillas afectadas
        Consumible,   // U usa Cons sobre B
        Dano,         // Obj recibe N de daño (U = atacante o null; Txt = fuente si no es ataque)
        Esquiva,      // Obj esquiva un ataque
        Cura,         // Obj recupera N
        Muerte,       // U cae
        Empuje,       // Obj se desplaza de A a B
        Salto,        // U salta de A a B (jefe)
        Invocar,      // aparece la unidad U
        Zona,         // se crean zonas Zona en las casillas Area
        Terreno,      // cambia el terreno de las casillas Area (Txt = "barro", "muro")
        Texto,        // texto flotante Txt sobre Obj (o sobre la casilla B)
        Tormenta,     // U desata una tormenta sobre todos los gatos
        Teletransporte, // U desaparece de A y aparece en B (Txt = "salto" para un salto en arco)
        Embestida,    // U avanza en línea recta de A a B por Camino (carrera rápida)
        Moneda,       // aparecen N monedas en la casilla B
        Recoger,      // U recoge N monedas en B
        Romper,       // el obstáculo de la casilla B se rompe (Txt = "caja", "roca", "barril")
        Explosion,    // explosión en las casillas Area (Elem = elemento)
        Choque,       // Obj choca contra algo en la casilla B (N = daño)
        Fin           // terminó el combate (N = 1 victoria, 0 derrota)
    }

    public sealed class Evento
    {
        public EvTipo Tipo;
        public Unidad U;
        public Unidad Obj;
        public Pos A, B;
        public int N;
        public List<Pos> Camino;
        public List<Pos> Area;
        public HabilidadDef Hab;
        public ZonaTipo Zona;
        public Consumible Cons;
        public string Txt;
        public Elemento Elem;
    }
}
