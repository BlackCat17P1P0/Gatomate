// Gatomate - núcleo del juego (sin dependencias de Unity).
// Gato: personaje que dura toda la run. Unidad: participante de un combate (gato o enemigo).
using System;
using System.Collections.Generic;

namespace Gatomate.Core
{
    /// <summary>Un gato de la run (dura toda la run, conserva vida y equipamiento).</summary>
    public sealed class Gato
    {
        public int Id;
        public string ClaseId;
        public string Nombre;
        public int Vida;
        public int Nivel = 1;                                              // sube al derrotar enemigos (ver Progreso)
        public int Xp;                                                     // puntos de experiencia hacia el siguiente nivel
        public readonly List<EquipoTipo> Equipo = new List<EquipoTipo>();  // objetos: como mucho uno de CUERPO y uno de ARMA
        public readonly List<string> HabilidadIds = new List<string>();   // las habilidades que eligió (de la reserva de su clase)

        /// <summary>Cuántas habilidades tiene o puede tener a su nivel: 2 al empezar y 1 más en cada nivel de Config.NivelesHabilidadExtra.</summary>
        public int HabilidadesTotales { get { return Progreso.HabilidadesPara(Nivel); } }

        /// <summary>Habilidades elegidas. Si todavía no eligió ninguna (pruebas), usa las primeras de su clase.</summary>
        public List<HabilidadDef> Habilidades
        {
            get
            {
                var l = new List<HabilidadDef>();
                foreach (string id in HabilidadIds) { HabilidadDef h = Clase.Hab(id); if (h != null) l.Add(h); }
                if (l.Count == 0) for (int i = 0; i < Config.HabilidadesPorGato && i < Clase.Pool.Count; i++) l.Add(Clase.Pool[i]);
                return l;
            }
        }

        public ClaseDef Clase { get { return GameData.Clase(ClaseId); } }

        /// <summary>El objeto que lleva en un espacio (Cuerpo o Arma), o null.</summary>
        public ObjetoDef Objeto(SlotObjeto slot)
        {
            foreach (EquipoTipo e in Equipo) { ObjetoDef d = Objetos.Def(e); if (d.Slot == slot) return d; }
            return null;
        }

        /// <summary>Estadísticas con nivel y equipamiento sumados.</summary>
        public Stats Stats
        {
            get
            {
                Stats s = Clase.Base.Copia();
                Progreso.AplicarNivel(s, Clase, Nivel);
                foreach (EquipoTipo e in Equipo) Objetos.Aplicar(s, Objetos.Def(e), ClaseId);
                return s;
            }
        }

        public int VidaMax { get { return Stats.Vida; } }

        /// <summary>Casillas de movimiento por turno: las de su clase más lo que den los objetos (mínimo 1).</summary>
        public int Movimiento
        {
            get
            {
                int m = Clase.Movimiento;
                foreach (EquipoTipo e in Equipo) m += Objetos.Def(e).Mov;
                return Math.Max(1, m);
            }
        }

        public HabilidadDef Basico { get { return Clase.Basico; } }
    }

    /// <summary>Participante de un combate. Se crea desde un Gato o desde un EnemigoDef.</summary>
    public sealed class Unidad
    {
        public int Id;
        public string Nombre;
        public Bando Bando;
        public Pos Pos;
        public int Vida, VidaMax, Defensa, Mana, ManaMax;
        public int Velocidad, Fuerza, Inteligencia, Destreza, Suerte;
        public int MovimientoBase;          // casillas de movimiento por turno (gatos: según su clase; enemigos: su definición)
        public HabilidadDef Habilidad;      // enemigos: su ataque. Gatos: la primera habilidad (compatibilidad)
        public HabilidadDef Basico;         // gatos: ataque básico gratis
        public readonly List<EquipoTipo> Equipo = new List<EquipoTipo>();   // gatos: objetos equipados (sinergias elementales)
        public readonly List<HabilidadDef> Habilidades = new List<HabilidadDef>();   // gatos: sus 2 habilidades
        public Pos Dir;                     // hacia dónde mira (para atacar por la espalda)
        public int Monedas;                 // monedas que lleva encima (ladrones)
        public Unidad Dueno;                // quién lo invocó (null si no es una invocación)
        public Pasiva Pasiva;
        public Gato Gato;                   // null en enemigos
        public EnemigoDef Def;              // null en gatos
        public string ColorHex;
        public readonly List<EstadoActivo> Estados = new List<EstadoActivo>();
        public int TurnosPropios;           // turnos que ya jugó (para habilidades "cada N turnos")
        public bool MitadUsada;             // jefes: efecto de "mitad de vida" ya usado
        public int RotacionJefe;            // jefe final: índice de la próxima habilidad
        public Unidad UltimoAtacante;       // quién le hizo daño por última vez (para dar experiencia al derrotarlo)

        public bool Vivo { get { return Vida > 0; } }
        public bool EsGato { get { return Bando == Bando.Gatos; } }
        public string ClaseId { get { return Gato != null ? Gato.ClaseId : (Def != null ? Def.Id : ""); } }

        public bool Lleva(EquipoTipo e) { return Equipo.Contains(e); }

        /// <summary>Suma del valor de un efecto especial entre sus objetos (0 si no lleva ninguno).</summary>
        public int Esp(Especial e)
        {
            int n = 0;
            for (int i = 0; i < Equipo.Count; i++) { ObjetoDef d = Objetos.Def(Equipo[i]); if (d.Esp == e) n += d.EspVal; }
            return n;
        }

        /// <summary>Elemento al que un anillo convierte los ataques físicos (Fisico si no lleva ninguno).</summary>
        public Elemento Imbuido
        {
            get
            {
                for (int i = 0; i < Equipo.Count; i++) { ObjetoDef d = Objetos.Def(Equipo[i]); if (d.Imbuye != Elemento.Fisico) return d.Imbuye; }
                return Elemento.Fisico;
            }
        }

        /// <summary>Daño extra de gemas y armas elementales para un elemento.</summary>
        public int BonusGema(Elemento e)
        {
            if (e == Elemento.Fisico) return 0;
            int n = 0;
            for (int i = 0; i < Equipo.Count; i++) { ObjetoDef d = Objetos.Def(Equipo[i]); if (d.BonoElem == e) n += d.BonoElemVal; }
            return n;
        }

        public bool Tiene(EstadoTipo t)
        {
            for (int i = 0; i < Estados.Count; i++) if (Estados[i].Tipo == t) return true;
            return false;
        }

        public EstadoActivo Estado(EstadoTipo t)
        {
            for (int i = 0; i < Estados.Count; i++) if (Estados[i].Tipo == t) return Estados[i];
            return null;
        }

        public void PonerEstado(EstadoTipo t, int turnos, int valor, Unidad fuente)
        {
            EstadoActivo e = Estado(t);
            if (e == null) { e = new EstadoActivo { Tipo = t }; Estados.Add(e); }
            e.Turnos = turnos; e.Valor = valor; e.Fuente = fuente;
            if (fuente != null && fuente.Bando != Bando) UltimoAtacante = fuente;
        }

        public void QuitarEstado(EstadoTipo t)
        {
            for (int i = Estados.Count - 1; i >= 0; i--) if (Estados[i].Tipo == t) Estados.RemoveAt(i);
        }

        /// <summary>Estados negativos que se pueden quitar con Plegaria o Antídoto.</summary>
        public bool TieneEstadoNegativo { get { return Tiene(EstadoTipo.Inmovilizado) || Tiene(EstadoTipo.Envenenado) || Tiene(EstadoTipo.Ralentizado); } }

        public int DefensaEfectiva
        {
            get
            {
                int d = Defensa;
                EstadoActivo e = Estado(EstadoTipo.DefensaMas);
                if (e != null) d += e.Valor;
                return d;
            }
        }

        public int VelocidadEfectiva
        {
            get
            {
                int v = Velocidad;
                EstadoActivo e = Estado(EstadoTipo.VelocidadMas);
                if (e != null) v += e.Valor;
                return v;
            }
        }

        public int StatValor(StatTipo t)
        {
            switch (t)
            {
                case StatTipo.Fuerza: return Fuerza;
                case StatTipo.Inteligencia: return Inteligencia;
                case StatTipo.Destreza: return Destreza;
                case StatTipo.Velocidad: return VelocidadEfectiva;
                case StatTipo.Suerte: return Suerte;
                default: return 0;
            }
        }

        public override string ToString() { return Nombre + " " + Vida + "/" + VidaMax + " " + Pos; }
    }
}
