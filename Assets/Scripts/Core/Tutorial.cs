// Gatomate - núcleo del juego (sin dependencias de Unity).
// El tutorial: una run corta de 3 niveles con tableros fijos, gatos fijos y pasos de texto que explican cada mecánica.
// Los textos son provisionales: quien lleve la historia y la estética puede cambiarlos aquí sin tocar las reglas.
using System;
using System.Collections.Generic;

namespace Gatomate.Core
{
    /// <summary>Qué tiene que hacer el jugador para que el paso avance (Ninguna = basta con pulsar "Siguiente").</summary>
    public enum TutAccion { Ninguna, Mover, Basico, Habilidad, Inspeccionar, TerminarTurno, Equipar, Intercambiar, Romper, Moneda, Consumible, Empujar, Elemental }

    public sealed class PasoTut
    {
        public string Titulo, Texto;
        public TutAccion Espera;
        public PasoTut(string titulo, string texto, TutAccion espera = TutAccion.Ninguna) { Titulo = titulo; Texto = texto; Espera = espera; }
    }

    public static class Tutorial
    {
        /// <summary>Clave del grupo de pasos que toca ahora. pantalla: "hab", "prep", "combate", "recompensa", "tienda".</summary>
        public static string Clave(Run run, string pantalla)
        {
            if (pantalla == "hab") return "hab";
            if (pantalla == "tienda") return "tienda" + Math.Max(1, run.Nivel - 1);
            return pantalla + run.Nivel;
        }

        public static List<PasoTut> Pasos(string clave)
        {
            var l = new List<PasoTut>();
            switch (clave)
            {
                case "hab":
                    l.Add(new PasoTut("¡Bienvenido a Gatomate!", "Cada gato aprende 2 habilidades de su clase. Aquí el Luchador elige 1 de 3 opciones, dos veces. Pasa el ratón sobre una para ver lo básico y haz clic para ver la descripción completa."));
                    l.Add(new PasoTut("¿No te convence ninguna?", "Puedes sortear otras 3 por 3 monedas ('Repetir'). Tienes 3 monedas de tutorial. Elige la que quieras para seguir."));
                    break;

                case "prep1":
                    l.Add(new PasoTut("Tu expedición", "Llevas 4 gatos a cada nivel. Cada clase tiene su vida (corazones), maná (gotas) y defensa. El resto son estadísticas: Fue, Int, Des, Vel y Sue."));
                    l.Add(new PasoTut("Cada gato se mueve distinto", "Los ágiles llegan lejos (Pícaro 6, Cazador 5) y los pesados poco (Tanque 2). El Luchador es el medio, con 4 casillas. La velocidad solo decide quién juega antes."));
                    l.Add(new PasoTut("Ataque básico y habilidades", "Todos tienen un ataque básico GRATIS (tecla A) y 2 habilidades que cuestan maná (teclas W y E). El maná sube 1 por turno, así que no puedes usar las habilidades siempre."));
                    l.Add(new PasoTut("Equipo", "Haz clic en el Collar de fuerza y luego en un gato para equiparlo. Cada gato lleva 2 objetos: uno de Cuerpo y uno de Arma.", TutAccion.Equipar));
                    l.Add(new PasoTut("¡A jugar!", "Cuando estés listo, pulsa ¡EMPEZAR NIVEL!."));
                    break;

                case "combate1":
                    l.Add(new PasoTut("Orden de turnos", "Arriba ves quién juega y quién sigue. Los gatos y los enemigos se mezclan según su velocidad. Ahora es el turno de uno de tus gatos."));
                    l.Add(new PasoTut("Moverse", "Las casillas azules son donde puedes ir. Haz clic en una para mover al gato (cuesta casillas de movimiento).", TutAccion.Mover));
                    l.Add(new PasoTut("Ataque básico", "Pulsa A (o el botón del ataque básico) y haz clic en un enemigo en rango. Es gratis, pero solo puedes atacar una vez por turno.", TutAccion.Basico));
                    l.Add(new PasoTut("Habilidades y maná", "En su turno otro gato puede usar W o E. Gastan maná y suelen hacer más daño o algo especial. Pasa el ratón para ver qué hacen.", TutAccion.Habilidad));
                    l.Add(new PasoTut("Mira a tu enemigo", "Haz clic DERECHO sobre un enemigo: verás sus debilidades, resistencias, vida y hasta dónde puede moverse.", TutAccion.Inspeccionar));
                    l.Add(new PasoTut("El terreno importa", "Hierba alta: 25% de esquivar (pero arde). Flores: curan 2 al empezar tu turno. Barro y agua cuestan más movimiento. Las cajas se rompen atacándolas y sueltan monedas."));
                    l.Add(new PasoTut("Monedas", "Pisa las monedas del suelo para recogerlas. Se gastan en la tienda entre niveles.", TutAccion.Moneda));
                    l.Add(new PasoTut("Cierra el turno", "Cuando termines con un gato pulsa TERMINAR (tecla T o Enter). Si ya no puedes hacer nada, pasa solo.", TutAccion.TerminarTurno));
                    l.Add(new PasoTut("Ganas si caen todos los enemigos", "Si caen tus 4 gatos pierdes una vida (en el tutorial no se gastan). ¡Sigue!"));
                    break;

                case "recompensa1":
                    l.Add(new PasoTut("Desafíos", "Tras cada victoria aparece 1 desafío misterioso y eliges qué gato lo enfrenta. Un + verde junto al gato significa que probablemente le irá bien, un - rojo que probablemente mal, y sin marca es una apuesta; la Suerte permite repetir la tirada. Puede dar equipo, bendiciones... o maldiciones."));
                    break;
                case "tienda1":
                    l.Add(new PasoTut("La tienda", "Gasta las monedas en pociones, bombas, objetos o un banquete que cura. Prueba el Anillo de fuego: hace que tus ataques físicos quemen."));
                    break;

                case "prep2":
                    l.Add(new PasoTut("Cambia de gato", "Tienes 4 gatos más en la reserva. Haz clic en uno de la reserva y luego en uno de la expedición para intercambiarlos. Este nivel es de elementos: prueba al Alquimista o al Druida.", TutAccion.Intercambiar));
                    l.Add(new PasoTut("Anillos y gemas", "Un anillo convierte tus ataques físicos en un elemento. Una gema suma daño con ese elemento. ¡Combínalos en un mismo gato!"));
                    l.Add(new PasoTut("A por el nivel", "Pulsa ¡EMPEZAR NIVEL! cuando estés listo."));
                    break;

                case "combate2":
                    l.Add(new PasoTut("Elementos", "Fuego quema la hierba y daña más a los slimes de hielo. Hielo ralentiza (y congela en el agua). Rayo pega +3 a quien está en el agua. Veneno daña cada turno."));
                    l.Add(new PasoTut("Debilidades", "Haz clic derecho en el slime de hielo: es débil al fuego. Usa una habilidad de fuego o el anillo de fuego con él.", TutAccion.Inspeccionar));
                    l.Add(new PasoTut("Rayo en el agua", "El Sapo está dentro del agua. Un ataque de rayo le pega +3 y salta a otros enemigos mojados.", TutAccion.Elemental));
                    l.Add(new PasoTut("Empujones y barriles", "Empujar a un enemigo contra una roca le hace daño. Los barriles explotan al romperse (6 de daño en cruz): ¡úsalos contra los enemigos!", TutAccion.Empujar));
                    l.Add(new PasoTut("Invocadores", "El Nido de ratas suelta ratas cada pocos turnos. Mátalo pronto. Puedes usar objetos con las teclas 1-8 (poción, bomba...).", TutAccion.Consumible));
                    break;
                case "recompensa2":
                    l.Add(new PasoTut("Otro desafío", "Elige con cabeza: el gato con más estadística tiene más probabilidades."));
                    break;
                case "tienda2":
                    l.Add(new PasoTut("Gema de fuego", "Compra la Gema de fuego para el gato que lleva el Anillo de fuego: +2 de daño y el fuego dura más."));
                    break;

                case "prep3":
                    l.Add(new PasoTut("El jefe", "Al final de cada mundo hay un jefe con habilidades especiales (aquí: un salto que cae a 4 casillas). Vencerlo da un gato nuevo en la partida real."));
                    l.Add(new PasoTut("Prepárate", "Equipa lo que te falte, elige a tus 4 gatos y pulsa ¡EMPEZAR NIVEL!."));
                    break;
                case "combate3":
                    l.Add(new PasoTut("El jefe avisa", "Haz clic derecho en el jefe para leer sus habilidades. Evita quedarte donde va a caer su salto."));
                    l.Add(new PasoTut("Ladrones y bombas", "La Rata ladrona va por las monedas del suelo. El Gato bomba explota al llegar a un gato: mátalo de lejos."));
                    break;

                case "fin":
                    l.Add(new PasoTut("¡Tutorial completado!", "Ya sabes lo básico: movimiento, ataque básico, habilidades, maná, terreno, elementos, empujes, objetos, desafíos y tienda. ¡Empieza una run de verdad!"));
                    break;
            }
            return l;
        }
    }
}
