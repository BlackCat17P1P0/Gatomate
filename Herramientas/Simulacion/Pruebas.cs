// Comprobaciones automáticas de las reglas del documento. Uso: dotnet run -- pruebas
using System;
using System.Collections.Generic;
using Gatomate.Core;

public static partial class Pruebas
{
    private static int _ok, _mal;

    private static void Chequear(bool cond, string msg)
    {
        if (cond) { _ok++; return; }
        _mal++;
        Console.WriteLine("FALLA: " + msg);
    }

    private static Gato G(string clase, int id)
    {
        var g = new Gato { Id = id, ClaseId = clase, Nombre = GameData.Clase(clase).Nombre };
        g.Vida = g.VidaMax;
        return g;
    }

    private static Combate Vacio(params Unidad[] us)
    {
        var tab = new Tablero(10, 10);
        var l = new List<Unidad>(us);
        var inv = new Dictionary<Consumible, int>();
        foreach (Consumible c in Enum.GetValues(typeof(Consumible))) inv[c] = 1;
        return new Combate(tab, l, new List<EfectoActivo>(), inv, new Azar(1), 1, 1);
    }

    private static Unidad Gt(string clase, int x, int y, int id)
    {
        Unidad u = GeneradorNivel.CrearGato(G(clase, id), id);
        u.Pos = new Pos(x, y);
        return u;
    }

    private static Unidad En(string tipo, int x, int y, int id)
    {
        Unidad u = GeneradorNivel.CrearEnemigo(GameData.Enemigo(tipo), GameData.Mundos[0], id);
        u.Pos = new Pos(x, y);
        return u;
    }

    public static int Correr()
    {
        // --- estadísticas iniciales del documento
        Chequear(GameData.Clase("luchador").Base.Vida == 24 && GameData.Clase("mago").Base.Mana == 8 && GameData.Clase("tanque").Base.Defensa == 3, "tabla de estadísticas iniciales");
        Chequear(GameData.Clases.Count == 8, "hay 8 clases");

        // --- movimiento propio de cada clase (propuesto): pícaro el que más, tanque el que menos, luchador medio con 4
        var c0 = Vacio();
        Chequear(c0.MovimientoTotal(Gt("luchador", 0, 0, 1)) == 4, "movimiento luchador = 4");
        Chequear(c0.MovimientoTotal(Gt("tanque", 0, 0, 1)) == 2, "movimiento tanque = 2");
        Chequear(c0.MovimientoTotal(Gt("picaro", 0, 0, 1)) == 6, "movimiento pícaro = 6");
        Chequear(c0.MovimientoTotal(Gt("mago", 0, 0, 1)) == 3, "movimiento mago = 3");

        // --- daño: poder + stat - defensa, mínimo 1
        var tanque = Gt("tanque", 5, 5, 1);
        Chequear(c0.CalcularDano(tanque, 3) == 1, "daño mínimo 1");
        Chequear(c0.CalcularDano(tanque, 10) == 7, "daño = bruto - defensa");

        // --- Zarpazo cargado: 4 + Fuerza, empuja 1; Inamovible no se empuja
        var luc = Gt("luchador", 3, 3, 1); var mat = En("maton", 4, 3, 101);
        var c1 = Vacio(luc, mat); c1.Activa = luc; luc.Mana = 3;
        int antes = mat.Vida;
        c1.UsarHabilidad(new Pos(4, 3));
        Chequear(antes - mat.Vida == 10, "Zarpazo hace 4+6=10 (hizo " + (antes - mat.Vida) + ")");
        Chequear(mat.Pos == new Pos(5, 3), "Zarpazo empuja 1 casilla");
        var luc2 = Gt("luchador", 3, 3, 1); var tan = Gt("tanque", 4, 3, 2);
        // el tanque es gato: para probar el empuje se usa un enemigo con la pasiva puesta a mano
        var bruto = En("bruto", 4, 3, 102); bruto.Pasiva = Pasiva.Inamovible;
        var c2 = Vacio(luc2, bruto); c2.Activa = luc2; luc2.Mana = 3;
        c2.UsarHabilidad(new Pos(4, 3));
        Chequear(bruto.Pos == new Pos(4, 3), "Inamovible no se empuja");
        Chequear(luc2.Mana == 2, "la habilidad cuesta 1 de maná");
        Chequear(!c2.UsarHabilidad(new Pos(4, 3)), "solo una habilidad por turno");

        // --- Francotirador: Disparo certero 2-4 de rango, +2 de pasiva => 2 a 6; no puede atacar a un enemigo pegado
        var caz = Gt("cazador", 0, 0, 1); var e6 = En("maton", 6, 0, 101); var e7 = En("maton", 7, 0, 102); var e1 = En("maton", 1, 0, 103);
        var c3 = Vacio(caz, e6, e7, e1); c3.Activa = caz;
        var objs = c3.CasillasObjetivo(caz, caz.Habilidad);
        Chequear(objs.Contains(new Pos(6, 0)) && !objs.Contains(new Pos(7, 0)), "Francotirador: rango 2 a 6");
        Chequear(!objs.Contains(new Pos(1, 0)), "Francotirador: no puede atacar a un enemigo pegado");
        Chequear(!objs.Contains(new Pos(1, 0)), "Disparo certero no llega a rango 1");

        // --- Bola de fuego: cruz, enciende hierba, Canalizar +1 turno
        var mag = Gt("mago", 3, 3, 1); var m1 = En("maton", 6, 3, 101); var m2 = En("maton", 7, 3, 102);
        var c4 = Vacio(mag, m1, m2); c4.Activa = mag; mag.Mana = 8;
        c4.Tab.PonerTerreno(new Pos(8, 3), Terreno.HierbaAlta);   // sin unidad encima (la hierba daría 25% de esquivar)
        // el mago tiene rango 4 => objetivo (7,3) está a 4; la cruz alcanza (6,3), (7,3) y (8,3)
        c4.UsarHabilidad(new Pos(7, 3));
        Chequear(m1.Vida < m1.VidaMax && m2.Vida < m2.VidaMax, "Bola de fuego daña la casilla y su vecina");
        Zona zf = c4.Tab.ZonaEn(new Pos(8, 3));
        Chequear(zf != null && zf.Tipo == ZonaTipo.Fuego && zf.Turnos == 3, "enciende la hierba alta y Canalizar la deja 3 turnos");

        // --- Puñalada: +3 si el objetivo no tiene aliados vecinos
        var pic = Gt("picaro", 3, 3, 1); var a = En("maton", 4, 3, 101); var b = En("maton", 4, 4, 102);
        var c5 = Vacio(pic, a, b); c5.Activa = pic; pic.Mana = 4;
        int va = a.Vida; c5.UsarHabilidad(new Pos(4, 3));
        Chequear(va - a.Vida == 3 + 5 - 0, "Puñalada con aliado vecino: 3+Des(5)=8 (hizo " + (va - a.Vida) + ")");
        var pic2 = Gt("picaro", 3, 3, 1); var solo = En("maton", 4, 3, 101);
        var c6 = Vacio(pic2, solo); c6.Activa = pic2; pic2.Mana = 4;
        int vs = solo.Vida; c6.UsarHabilidad(new Pos(4, 3));
        Chequear(vs - solo.Vida == 11, "Puñalada a objetivo aislado: +3 => 11 (hizo " + (vs - solo.Vida) + ")");

        // --- bendiciones: Furia +2 daño infligido; Piel de piedra -2 recibido
        var luc3 = Gt("luchador", 3, 3, 1); var mat3 = En("maton", 4, 3, 101);
        var c7 = Vacio(luc3, mat3); c7.Activa = luc3; luc3.Mana = 3;
        c7.Efectos.Add(new EfectoActivo { Tipo = EfectoParty.Furia, Turnos = 3 });
        int v3 = mat3.Vida; c7.UsarHabilidad(new Pos(4, 3));
        Chequear(v3 - mat3.Vida == 12, "Furia: 10+2 = 12");
        var c7b = Vacio(Gt("tanque", 1, 1, 1));
        c7b.Efectos.Add(new EfectoActivo { Tipo = EfectoParty.PielDePiedra, Turnos = 3 });
        Chequear(c7b.CalcularDano(c7b.Unidades[0], 10) == 5, "Piel de piedra: 10-3-2=5");

        // --- desafíos: probabilidades
        var o = new Oferta { Def = Desafios.Todos[0], Dificultad = 5 };   // Roca (Fuerza)
        Chequear(Math.Abs(Desafios.Probabilidad(G("luchador", 1), o) - 0.7) < 1e-9, "Luchador Fuerza 6 (+1 por ser la más alta) vs 5 => 70%");
        Chequear(Math.Abs(Desafios.Probabilidad(G("mago", 1), o) - 0.05) < 1e-9, "Mago Fuerza 1 (-1 por ser la más baja) => mínimo 5%");
        var oc = new Oferta { Def = Desafios.Todos[3], Dificultad = 5 };  // Cerradura (Destreza)
        Chequear(Math.Abs(Desafios.Probabilidad(G("cazador", 1), oc) - 0.7) < 1e-9, "Cazador Destreza 6 vs 5 => 70%");
        var ocofre = new Oferta { Def = Desafios.Todos[5], Dificultad = 8 };
        Chequear(Desafios.Probabilidad(G("mago", 1), ocofre) == 1.0, "Cofre sin cerradura siempre sale bien");
        var gs = G("mago", 1); gs.Equipo.Add(EquipoTipo.AmuletoTrebol); gs.Equipo.Add(EquipoTipo.AmuletoTrebol); gs.Equipo.Add(EquipoTipo.AmuletoTrebol);
        Chequear(Math.Abs(Desafios.ProbRepetir(gs) - 0.2) < 1e-9, "suerte 7 => 20% de repetir");
        Chequear(Desafios.Dificultad(1) == 5 && Desafios.Dificultad(2) == 5 && Desafios.Dificultad(3) == 6 && Desafios.Dificultad(4) == 6 && Desafios.Dificultad(5) == 7 && Desafios.Dificultad(6) == 7 && Desafios.Dificultad(7) == 8, "dificultad por mundo 5,5,6,6,7,7,8");

        // --- tablero procedural: tamaños variables, conexo, reproducible, agua en charcos/ríos, grupos juntos
        bool conexo = true, igual = true, aguaOk = true, agrupados = true, libresOk = true;
        var tamanos = new HashSet<string>(); var disposiciones = new HashSet<int>();
        int rocasGrandes = 0, aguaTotal = 0, barroCerca = 0, barroTotal = 0;
        for (int mundo = 1; mundo <= 7; mundo++)
            for (int sem = 1; sem <= 40; sem++)
                for (int niv = 1; niv <= 3; niv++)
                {
                    var p1 = GeneradorNivel.Planear(mundo, niv, sem * 13 + niv);
                    var p2 = GeneradorNivel.Planear(mundo, niv, sem * 13 + niv);
                    Tablero t1 = p1.Tab, t2 = p2.Tab;
                    tamanos.Add(t1.Ancho + "x" + t1.Alto); disposiciones.Add(p1.Disposicion);
                    if (!t1.EsConexo()) conexo = false;
                    if (t1.Ancho != t2.Ancho || t1.Alto != t2.Alto) igual = false;
                    else for (int x = 0; x < t1.Ancho; x++) for (int y = 0; y < t1.Alto; y++) if (t1.TerrenoEn(new Pos(x, y)) != t2.TerrenoEn(new Pos(x, y))) igual = false;
                    if (!GeneradorNivel.AguaValida(t1)) aguaOk = false;
                    for (int x = 0; x < t1.Ancho; x++)
                        for (int y = 0; y < t1.Alto; y++)
                        {
                            Pos q = new Pos(x, y);
                            Terreno tt = t1.TerrenoEn(q);
                            if (tt == Terreno.Muro) rocasGrandes++;
                            if (tt == Terreno.Agua) aguaTotal++;
                            if (tt == Terreno.Barro)
                            {
                                barroTotal++;
                                bool junto = false;
                                for (int dx = -2; dx <= 2; dx++) for (int dy = -2; dy <= 2; dy++) { Pos oo = new Pos(x + dx, y + dy); if (t1.Dentro(oo) && t1.TerrenoEn(oo) == Terreno.Agua) junto = true; }
                                if (junto) barroCerca++;
                            }
                        }
                    foreach (Pos q in p1.PosGatos) if (!t1.Pasable(q)) libresOk = false;
                    foreach (Pos q in p1.PosEnemigos) if (!t1.Pasable(q)) libresOk = false;
                    int maxd = 0;
                    foreach (Pos a1 in p1.PosEnemigos) foreach (Pos b1 in p1.PosEnemigos) maxd = Math.Max(maxd, a1.Dist(b1));
                    if (maxd > 4) agrupados = false;
                    maxd = 0;
                    foreach (Pos a1 in p1.PosGatos) foreach (Pos b1 in p1.PosGatos) maxd = Math.Max(maxd, a1.Dist(b1));
                    if (maxd > 4) agrupados = false;
                }
        Chequear(conexo, "todos los tableros son conexos");
        Chequear(igual, "la misma semilla da el mismo tablero");
        Chequear(tamanos.Count >= 8, "los tableros tienen tamaños variados (" + tamanos.Count + " distintos)");
        Chequear(disposiciones.Count == 8, "salen las 8 disposiciones de inicio");
        Chequear(aguaOk && aguaTotal > 0, "el agua forma charcos 2x2 o ríos continuos, nunca casillas sueltas");
        Chequear(libresOk && agrupados, "gatos y enemigos empiezan en casillas libres y agrupados");
        Chequear(rocasGrandes > 0, "hay rocas grandes");
        Chequear(barroTotal == 0 || barroCerca * 100 >= barroTotal * 55, "el barro tiende a estar cerca del agua (" + barroCerca + "/" + barroTotal + ")");

        // --- enemigos por nivel
        Config.MundosJugables = 7;
        var inv = new Dictionary<Consumible, int>();
        var grupo = new List<Gato> { G("luchador", 1), G("mago", 2), G("tanque", 3), G("cazador", 4) };
        Chequear(GeneradorNivel.Crear(grupo, 1, 1, 5, new List<EfectoActivo>(), inv).Vivos(Bando.Enemigos).Count == 3, "nivel 1: 3 enemigos");
        Chequear(GeneradorNivel.Crear(grupo, 1, 2, 5, new List<EfectoActivo>(), inv).Vivos(Bando.Enemigos).Count == 4, "nivel 2: 4 enemigos");
        var n3 = GeneradorNivel.Crear(grupo, 3, 3, 5, new List<EfectoActivo>(), inv);
        Chequear(n3.Vivos(Bando.Enemigos).Count == 3 && n3.Vivos(Bando.Enemigos).Exists(delegate (Unidad u) { return u.Def.EsJefe && u.Def.Id == "jefe3"; }), "nivel 3: jefe del mundo + 2 escoltas");
        var n5 = GeneradorNivel.Crear(grupo, 5, 1, 5, new List<EfectoActivo>(), inv);
        Chequear(n5.Vivos(Bando.Enemigos).TrueForAll(delegate (Unidad u) { return u.VidaMax >= GameData.Enemigo(u.Def.Id).Vida + 8; }), "mundo 5: +8 de vida a los enemigos");

        // --- la run completa: reglas de campaña
        bool gatosOrden = true, gatosUnicos = true, vidasOk = true, caidosVuelven = true, bendicionOk = true, perdioAlgunaRun = false, completoAlguna = false;
        for (int s = 1; s <= 60; s++)
        {
            Run r = new Run(s * 31);
            if (r.Expedicion.Count != 4) gatosOrden = false;
            var clases = new HashSet<string>();
            foreach (Gato g in r.Expedicion) clases.Add(g.ClaseId);
            if (clases.Count != 4) gatosUnicos = false;
            if (r.Reserva.Count != 0) gatosOrden = false;

            int guardia = 0;
            while (r.Fase != FaseRun.RunPerdida && r.Fase != FaseRun.CampanaCompletada && guardia++ < 500)
            {
                if (r.Fase == FaseRun.Preparacion)
                {
                    BotJugador.Equipar(r);
                    if (r.Efectos.Count == 0 && r.Nivel == 1 && r.Mundo == 1 && s % 2 == 0) r.AgregarEfecto(EfectoParty.Furia, 8);
                    int turnosAntes = -1;
                    foreach (EfectoActivo e in r.Efectos) if (e.Tipo == EfectoParty.Furia) turnosAntes = e.Turnos;
                    r.Empezar();
                    BotJugador.JugarCombate(r.Combate);
                    int rondas = r.Combate.Ronda;
                    var est = r.Combate.Estado;
                    int vidasAntes = r.Vidas;
                    r.ResolverFinDeCombate();
                    if (est == EstadoCombate.Victoria && turnosAntes > 0)
                    {
                        int quedan = 0; foreach (EfectoActivo e in r.Efectos) if (e.Tipo == EfectoParty.Furia) quedan = e.Turnos;
                        if (quedan != Math.Max(0, turnosAntes - rondas)) bendicionOk = false;
                    }
                    if (est == EstadoCombate.Derrota)
                    {
                        if (r.Vidas != vidasAntes - 1) vidasOk = false;
                        if (r.Vidas > 0) { foreach (Gato g in r.Expedicion) if (g.Vida < 1) caidosVuelven = false; }
                    }
                }
                else if (r.Fase == FaseRun.Combate) { BotJugador.JugarCombate(r.Combate); r.ResolverFinDeCombate(); }
                else if (r.Fase == FaseRun.Recompensa) BotJugador.ElegirDesafio(r);
                else if (r.Fase == FaseRun.ElegirHabilidad) BotJugador.ElegirHabilidades(r);
                else if (r.Fase == FaseRun.Tienda) { BotJugador.Comprar(r); r.SalirDeTienda(); }
            }
            if (r.Fase == FaseRun.RunPerdida) { perdioAlgunaRun = true; if (r.Vidas != 0) vidasOk = false; }
            if (r.Fase == FaseRun.CampanaCompletada)
            {
                completoAlguna = true;
                // 4 gatos en reserva, distintos entre sí y de la expedición => los 8 gatos
                var todos = new HashSet<string>();
                foreach (Gato g in r.TodosLosGatos) todos.Add(g.ClaseId);
                if (r.Reserva.Count != 4 || todos.Count != 8) gatosUnicos = false;
                if (r.Historial.FindAll(delegate (RegistroNivel x) { return x.Resultado == "victoria"; }).Count != 21) gatosOrden = false;
            }
        }
        Chequear(gatosOrden, "run empieza con 4 gatos y reserva vacía; la campaña completa tiene 21 victorias");
        Chequear(gatosUnicos, "4 clases distintas al inicio; al completar hay 8 gatos distintos (4 en reserva)");
        Chequear(vidasOk, "cada derrota resta exactamente 1 vida; sin vidas la run termina");
        Chequear(caidosVuelven, "los gatos caídos vuelven al reiniciar el nivel");
        Chequear(bendicionOk, "los turnos de una bendición siguen contando en el nivel siguiente (8 - rondas jugadas)");
        Chequear(perdioAlgunaRun, "en 60 runs alguna se pierde (el juego no es trivial)");
        Chequear(completoAlguna, "en 60 runs alguna completa la campaña (el juego no es imposible)");

        // --- eventos para animar: se graban sin alterar el resultado
        bool eventosOk = true;
        for (int sem = 1; sem <= 25; sem++)
        {
            int mundo = 1 + sem % 7;
            var g1 = new List<Gato> { G("luchador", 1), G("mago", 2), G("tanque", 3), G("clerigo", 4) };
            var g2 = new List<Gato> { G("luchador", 1), G("mago", 2), G("tanque", 3), G("clerigo", 4) };
            Combate x = GeneradorNivel.Crear(g1, mundo, 3, sem, new List<EfectoActivo>(), new Dictionary<Consumible, int>());
            Combate y = GeneradorNivel.Crear(g2, mundo, 3, sem, new List<EfectoActivo>(), new Dictionary<Consumible, int>());
            x.GrabarEventos = true;
            x.Iniciar(); y.Iniciar();
            BotJugador.JugarCombate(x); BotJugador.JugarCombate(y);
            bool tieneMover = false, tieneFin = false, tieneTurno = false;
            foreach (Evento e in x.Eventos)
            {
                if (e.Tipo == EvTipo.Mover && e.Camino != null && e.Camino.Count > 0 && e.Camino[e.Camino.Count - 1] == e.B) tieneMover = true;
                if (e.Tipo == EvTipo.Fin) tieneFin = true;
                if (e.Tipo == EvTipo.Turno) tieneTurno = true;
            }
            if (x.Estado != y.Estado || x.Ronda != y.Ronda || !tieneFin || !tieneTurno || y.Eventos.Count != 0) eventosOk = false;
            if (!tieneMover && sem > 3) eventosOk = false;
        }
        Chequear(eventosOk, "grabar eventos no cambia el combate y registra turnos, movimientos y fin");

        // --- reproducibilidad de una run completa con bot
        Run ra = new Run(12345), rb = new Run(12345);
        BotJugador.JugarRun(ra); BotJugador.JugarRun(rb);
        bool mismo = ra.Historial.Count == rb.Historial.Count && ra.Fase == rb.Fase;
        for (int i = 0; mismo && i < ra.Historial.Count; i++) mismo = ra.Historial[i].Resultado == rb.Historial[i].Resultado && ra.Historial[i].Rondas == rb.Historial[i].Rondas;
        Chequear(mismo, "la misma semilla reproduce la misma run");

        CorrerNuevas();
        CorrerSinergias();
        CorrerTutorialYGuardado();
        CorrerTanda2();

        Console.WriteLine("Comprobaciones correctas: " + _ok + ", fallidas: " + _mal);
        return _mal == 0 ? 0 : 1;
    }
}
