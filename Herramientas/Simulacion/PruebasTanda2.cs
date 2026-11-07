// Comprobaciones de la segunda tanda: 50 objetos en 2 espacios, niveles de gato, fuego amigo, obstáculos con más vida,
// descanso en la tienda, reserva que se cura y herramientas de desarrollador. Todas son propuestas por confirmar.
using System;
using System.Collections.Generic;
using Gatomate.Core;

public static partial class Pruebas
{
    private static void CorrerTanda2()
    {
        Config.GrabarEventos = false;

        // ====================================================== 50 objetos, 2 espacios
        var nombres = new HashSet<string>(); var descs = new HashSet<string>();
        int cuerpo = 0, arma = 0; bool defOk = true;
        foreach (EquipoTipo e in Enum.GetValues(typeof(EquipoTipo)))
        {
            ObjetoDef d = Objetos.Def(e);
            if (d == null || d.Id != e || string.IsNullOrEmpty(d.Icono) || string.IsNullOrEmpty(d.Nombre)) defOk = false;
            nombres.Add(Textos.Nombre(e)); descs.Add(Textos.Descripcion(e));
            if (d.Slot == SlotObjeto.Cuerpo) cuerpo++; else arma++;
        }
        Chequear(Enum.GetValues(typeof(EquipoTipo)).Length == 50 && defOk, "hay 50 objetos y todos tienen datos");
        Chequear(nombres.Count == 50 && descs.Count == 50, "los 50 objetos tienen nombre y descripción distintos");
        Chequear(cuerpo == 25 && arma == 25, "25 objetos de cuerpo y 25 de arma");

        Run r = new Run(7);
        r.ElegirHabilidadesAutomatico();
        Gato g0 = r.Expedicion[0];
        r.EquipoSinAsignar.Add(EquipoTipo.ChalecoCuero); r.EquipoSinAsignar.Add(EquipoTipo.PetoHierro); r.EquipoSinAsignar.Add(EquipoTipo.EspadaAfilada);
        Chequear(r.Equipar(g0, EquipoTipo.ChalecoCuero) && g0.Equipo.Count == 1, "equipar un objeto de cuerpo");
        Chequear(r.Equipar(g0, EquipoTipo.PetoHierro) && g0.Equipo.Count == 1 && g0.Equipo.Contains(EquipoTipo.PetoHierro) && r.EquipoSinAsignar.Contains(EquipoTipo.ChalecoCuero), "un segundo objeto de cuerpo reemplaza al primero, que vuelve al inventario");
        Chequear(r.Equipar(g0, EquipoTipo.EspadaAfilada) && g0.Equipo.Count == 2, "el espacio de arma es aparte");
        Chequear(r.Desequipar(g0, EquipoTipo.PetoHierro) && g0.Equipo.Count == 1 && r.EquipoSinAsignar.Contains(EquipoTipo.PetoHierro), "se puede desequipar un objeto");
        Chequear(!r.Equipar(g0, EquipoTipo.AnilloRayo), "no se puede equipar lo que no está en el inventario");

        // ---------------- efectos especiales
        var tan = Ge("tanque", 3, 3, 1, new[] { EquipoTipo.CollarEspinas }, "provocar", "embestida_escudo");
        var mat = En("maton", 4, 3, 101);
        var ce = Vacio(tan, mat);
        int antes = mat.Vida;
        ce.Danar(mat, tan, 5, false);
        Chequear(mat.Vida == antes - 2, "Collar de espinas: quien te golpea cuerpo a cuerpo pierde 2 de vida");

        var vam = Ge("luchador", 3, 3, 1, new[] { EquipoTipo.ColmilloVampirico }, "zarpazo", "carga");
        var rat = En("rata", 4, 3, 101); rat.Vida = 1;
        var m2 = En("maton", 8, 8, 102);
        var cv = Vacio(vam, rat, m2);
        vam.Vida = vam.VidaMax - 6; int vidaAntes = vam.Vida;
        cv.Danar(vam, rat, 9, false);
        Chequear(vam.Vida == vidaAntes + 2, "Colmillo vampírico: recuperas 2 de vida al derrotar a un enemigo");
        Chequear(vam.Gato.Nombre != null && cv.XpGanada.Count == 0, "una rata invocada no da experiencia");
        var m3 = En("maton", 4, 4, 103); m3.Vida = 1;
        var cx = Vacio(vam, m3, m2);
        cx.Danar(vam, m3, 9, false);
        int xp; cx.XpGanada.TryGetValue(vam.Gato, out xp);
        Chequear(xp == Config.XpPorEnemigo, "el gato que derrota a un enemigo gana experiencia");

        var lan = Ge("luchador", 3, 3, 1, new[] { EquipoTipo.LanzaPerforante }, "zarpazo", "carga");
        var sin = Ge("luchador", 3, 3, 1, new EquipoTipo[0], "zarpazo", "carga");
        var duro = En("maton", 4, 3, 101); duro.Defensa = 3;
        var cl = Vacio(lan, duro);
        int conLanza = cl.CalcularDano(duro, 6, Elemento.Fisico, true, lan.Esp(Especial.Perfora));
        int sinLanza = cl.CalcularDano(duro, 6, Elemento.Fisico, true, 0);
        Chequear(conLanza == sinLanza + 2, "Lanza perforante: ignora 2 de defensa (" + conLanza + " vs " + sinLanza + ")");

        var bro = Ge("mago", 1, 1, 1, new[] { EquipoTipo.BrazaleteSalto }, "bola", "parpadeo");
        var cb = Vacio(bro);
        Chequear(cb.CosteDe(bro, Hb(bro, "parpadeo")) == Math.Max(0, Hb(bro, "parpadeo").CosteMana - 1) && cb.CosteDe(bro, Hb(bro, "bola")) == Hb(bro, "bola").CosteMana, "Brazalete saltarín: la movilidad cuesta 1 menos y lo demás igual");

        var corona = Ge("mago", 1, 1, 1, new[] { EquipoTipo.CoronaGato }, "bola", "rayo");
        var cco = Vacio(corona, En("maton", 9, 9, 101));
        corona.Mana = 2; cco.Iniciar();
        Chequear(corona.Mana == 2 + Config.ManaPorTurno + 1, "Corona de gato: +1 de maná al empezar el turno (" + corona.Mana + ")");

        var botas = Ge("mago", 1, 1, 1, new[] { EquipoTipo.BotasViento }, "bola", "rayo");
        Chequear(Vacio(botas).MovimientoTotal(botas) == GameData.Clase("mago").Movimiento + 2, "Botas del viento: +2 de movimiento");
        var peto = Ge("picaro", 1, 1, 1, new[] { EquipoTipo.ArmaduraPlacas }, "punalada", "paso_sombrio");
        Chequear(Vacio(peto).MovimientoTotal(peto) == GameData.Clase("picaro").Movimiento - 1, "Armadura de placas: -1 de movimiento");

        // ---------------- fuego amigo
        var mag = Ge("mago", 2, 5, 1, new EquipoTipo[0], "bola", "rayo");
        var aliado = Gt("luchador", 5, 6, 2);
        var enemigo = En("maton", 5, 5, 101);
        var lejano = En("maton", 9, 0, 102);
        var cf = Vacio(mag, aliado, enemigo, lejano); cf.Activa = mag; mag.Mana = 8;
        int vEn = enemigo.Vida, vAl = aliado.Vida, vMag = mag.Vida;
        Chequear(cf.UsarHabilidad(Hb(mag, "bola"), new Pos(5, 5)), "se lanza la bola de fuego");
        int danoEn = vEn - enemigo.Vida, danoAl = vAl - aliado.Vida;
        Chequear(danoEn > 0 && danoAl > 0 && danoAl <= danoEn, "fuego amigo: el aliado en el área recibe daño (" + danoAl + " vs " + danoEn + " al enemigo)");
        Chequear(mag.Vida == vMag, "quien lanza el ataque no se hiere a sí mismo");
        var aliado2 = Ge("luchador", 5, 6, 2, new[] { EquipoTipo.AmuletoLealtad }, "zarpazo", "carga");
        var mag2 = Ge("mago", 2, 5, 1, new EquipoTipo[0], "bola", "rayo");
        var enem2 = En("maton", 5, 5, 101);
        var cf2 = Vacio(mag2, aliado2, enem2, En("maton", 9, 0, 102)); cf2.Activa = mag2; mag2.Mana = 8;
        int vAl2 = aliado2.Vida;
        cf2.UsarHabilidad(Hb(mag2, "bola"), new Pos(5, 5));
        Chequear(aliado2.Vida == vAl2, "Amuleto de lealtad: no recibes fuego amigo");
        var enA = En("arquero", 5, 5, 101); var enB = En("maton", 5, 6, 102); enB.Bando = Bando.Enemigos;
        Chequear(!cf.PagaFuegoAmigo(enA, enB), "los enemigos no se hieren entre sí");

        // ---------------- obstáculos: roca agrietada 8, roca 16, roca grande 24
        var tb = new Tablero(6, 6);
        tb.PonerTerreno(new Pos(1, 1), Terreno.RocaFragil); tb.PonerTerreno(new Pos(2, 1), Terreno.Roca); tb.PonerTerreno(new Pos(3, 1), Terreno.Muro);
        Chequear(tb.VidaObstaculo(new Pos(1, 1)) == 8 && tb.VidaObstaculo(new Pos(2, 1)) == 16 && tb.VidaObstaculo(new Pos(3, 1)) == 24, "vida de los obstáculos: agrietada 8, normal 16 (doble), grande 24 (triple)");
        Chequear(!tb.Pasable(new Pos(2, 1)) && tb.EsRompible(new Pos(2, 1)) && tb.EsRompible(new Pos(3, 1)), "las rocas bloquean el paso pero se pueden romper");
        Chequear(!tb.DanarObstaculo(new Pos(2, 1), 15) && tb.VidaObstaculo(new Pos(2, 1)) == 1 && tb.DanarObstaculo(new Pos(2, 1), 1) && tb.Pasable(new Pos(2, 1)), "una roca se rompe al llegar a 0 y deja suelo");

        // ====================================================== niveles
        Chequear(Progreso.HabilidadesPara(1) == 2 && Progreso.HabilidadesPara(2) == 2 && Progreso.HabilidadesPara(3) == 3 && Progreso.HabilidadesPara(6) == 4 && Progreso.HabilidadesPara(10) == 4, "habilidades por nivel: 2, 3 desde el nivel 3, 4 desde el 6");
        Gato gn = G("luchador", 9);
        int v0 = gn.VidaMax, f0 = gn.Stats.Fuerza;
        List<int> sub = Progreso.GanarXp(gn, Progreso.XpParaSubir(1));
        Chequear(sub.Count == 1 && gn.Nivel == 2 && gn.VidaMax == v0 + 2 && gn.Stats.Fuerza == f0 + 1 && gn.Xp == 0, "subir de nivel: +2 de vida y +1 a la estadística principal");
        Progreso.GanarXp(gn, 999);
        Chequear(gn.Nivel == Config.NivelMaximo, "el nivel máximo es " + Config.NivelMaximo);
        Gato gm = G("mago", 10); Progreso.GanarXp(gm, Progreso.XpParaSubir(1));
        Chequear(gm.Stats.Inteligencia == GameData.Clase("mago").Base.Inteligencia + 1, "el mago sube inteligencia");

        Run rn = new Run(11);
        rn.ElegirHabilidadesAutomatico();
        Gato gx = rn.Expedicion[0];
        Progreso.GanarXp(gx, Progreso.XpParaSubir(1) + Progreso.XpParaSubir(2));
        Chequear(gx.Nivel == 3, "dos subidas seguidas");
        rn.DebugSubirNivel();
        Chequear(rn.Pendientes.Count > 0 && rn.Fase == FaseRun.ElegirHabilidad, "al subir al nivel de habilidad extra hay que elegirla");
        bool extra = false;
        foreach (PendienteHab ph in rn.Pendientes) if (ph.Gato == gx && ph.Numero == 3) extra = true;
        Chequear(extra, "el gato de nivel 4 elige su 3ª habilidad");
        rn.ElegirHabilidadesAutomatico();
        Chequear(gx.HabilidadIds.Count == 3 && gx.Habilidades.Count == 3 && rn.Fase == FaseRun.Preparacion, "…y la tiene");

        // nuevos gatos al nivel medio
        Run rl = new Run(12); rl.ElegirHabilidadesAutomatico();
        foreach (Gato g in rl.Expedicion) { g.Nivel = 5; g.Vida = g.VidaMax; }
        bool nivelOk = true;
        for (int i = 0; i < 30; i++) { int n = rl.NivelNuevoGato(); if (n < 4 || n > 5) nivelOk = false; }
        Chequear(nivelOk, "un gato nuevo llega con el nivel medio de la party o 1 menos");
        Chequear(rl.DebugNuevoGato() && rl.Reserva.Count == 1 && rl.Reserva[0].Nivel >= 4 && rl.Reserva[0].Vida == rl.Reserva[0].VidaMax, "el gato nuevo llega a la reserva con ese nivel y la vida llena");

        // la experiencia se aplica al ganar un nivel
        Run rv = new Run(13); rv.ElegirHabilidadesAutomatico();
        rv.Empezar();
        Gato matador = rv.Expedicion[0];
        rv.Combate.XpGanada[matador] = Progreso.XpParaSubir(1);
        foreach (Unidad u in new List<Unidad>(rv.Combate.Vivos(Bando.Enemigos))) { u.UltimoAtacante = null; rv.Combate.Matar(u); }
        rv.ResolverFinDeCombate();
        Chequear(matador.Nivel == 2 && rv.SubidasDeNivel.Count == 1, "al ganar el combate el gato sube de nivel con la experiencia que reunió");

        // la reserva se cura al ganar
        Run rr = new Run(14); rr.ElegirHabilidadesAutomatico();
        rr.DebugNuevoGato(); rr.ElegirHabilidadesAutomatico();
        Gato res = rr.Reserva[0]; res.Vida = 1;
        rr.DebugTerminarNivel();
        Chequear(res.Vida > 1, "los gatos de la reserva se curan un poco por ronda de combate");

        // ====================================================== tienda: descansar
        Run rt = new Run(15); rt.ElegirHabilidadesAutomatico();
        rt.DebugIrATienda();
        int idxD = -1; for (int i = 0; i < rt.Tienda.Count; i++) if (rt.Tienda[i].EsDescanso) idxD = i;
        Chequear(idxD >= 0 && rt.Tienda[idxD].Precio == 30, "la tienda ofrece Descansar por 30 monedas");
        foreach (Gato g in rt.TodosLosGatos) g.Vida = 1;
        rt.Dinero = 29;
        Chequear(!rt.Comprar(idxD), "con 29 monedas no se puede descansar");
        rt.Dinero = 30;
        bool todos = rt.Comprar(idxD) && rt.Dinero == 0;
        foreach (Gato g in rt.TodosLosGatos) if (g.Vida != g.VidaMax) todos = false;
        Chequear(todos, "Descansar cura toda la vida de todos los gatos");

        // ====================================================== menú de desarrollador
        Run rd = new Run(16); rd.ElegirHabilidadesAutomatico();
        rd.DebugIrA(3, 2);
        Chequear(rd.Mundo == 3 && rd.Nivel == 2 && rd.Fase == FaseRun.Preparacion && rd.UsoDebug, "debug: ir directo al mundo 3, nivel 2");
        rd.DebugDinero(40);
        Chequear(rd.Dinero == 40, "debug: dar dinero");
        rd.DebugTerminarNivel();
        Chequear(rd.Fase == FaseRun.Recompensa && rd.Historial.Count == 1 && rd.Historial[0].Resultado == "victoria", "debug: terminar el nivel al instante lleva a la recompensa");
        rd.DebugIrATienda();
        Chequear(rd.Fase == FaseRun.Tienda && rd.Tienda.Count >= 5, "debug: ir a la tienda");
        rd.DebugIrA(1, 3);
        rd.DebugTerminarNivel();
        Chequear((rd.Fase == FaseRun.ElegirHabilidad || rd.Fase == FaseRun.Recompensa) && rd.Reserva.Count == 1, "debug: terminar el nivel del jefe da su gato nuevo");

        // ====================================================== guardado con niveles
        Run rg = new Run(17); rg.ElegirHabilidadesAutomatico();
        rg.Expedicion[1].Nivel = 4; rg.Expedicion[1].Xp = 3;
        rg.Equipar(rg.Expedicion[1], EquipoTipo.CollarFuerza);
        rg.EquipoSinAsignar.Add(EquipoTipo.CollarFuerza); rg.Equipar(rg.Expedicion[1], EquipoTipo.CollarFuerza);
        string txt = rg.Serializar();
        Run cargado = txt == null ? null : Run.Cargar(txt);
        Chequear(cargado != null && cargado.Expedicion[1].Nivel == 4 && cargado.Expedicion[1].Xp == 3 && cargado.Expedicion[1].Equipo.Contains(EquipoTipo.CollarFuerza), "el guardado conserva nivel, experiencia y equipo");
    }
}
