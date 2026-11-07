#!/usr/bin/env python3
"""MCDA y analisis de sensibilidad de las tres decisiones del PoC de Gatomate (Fase 5).

Uso:  python3 mcda.py <carpeta_resultados>

Reglas (se repiten en el documento):
  * Cada criterio se deriva de un driver. Peso = prioridad / suma de prioridades (prioridades 1 a 3; PROPUESTAS, por confirmar por el equipo).
  * Puntaje 0-100 por celda. Donde hay medicion en este PoC (tipo O) el puntaje es 100 si cumple el umbral y 0 si no.
    Donde la alternativa no se puede medir fuera de Unity, el puntaje es 50 (posible segun documentacion o sin efecto conocido) o 0/100 si es un
    juicio explicito del equipo (tipo J, por confirmar). Cada celda lleva su tipo de evidencia.
  * Sensibilidad: (a) cada peso x0 y x2, (b) 10 000 sorteos de pesos con distribucion de Dirichlet(1,...,1) y semilla 2026, (c) factor por el que hay
    que multiplicar el peso de un criterio que favorece a A2 para que A2 supere a A1.
Escribe mcda_resultados.csv, mcda_sensibilidad.csv y mcda_figura.png en la carpeta de resultados.
"""
import csv, json, os, random, sys

CARPETA = sys.argv[1] if len(sys.argv) > 1 else "resultados"

# (id criterio, nombre, driver, prioridad, puntaje A1, tipo A1, puntaje A2, tipo A2, evidencia)
DECISIONES = {
    "DEC1": ("Core de reglas independiente de Unity (A1) frente a logica dentro de MonoBehaviours (A2)", [
        ("C1", "Pruebas y simulacion sin abrir Unity", "D1", 3, 100, "O", 50, "D", "E1, E2, E3"),
        ("C2", "Vista reemplazable sin tocar las reglas", "D4", 3, 100, "O", 0, "J", "E1, E7"),
        ("C3", "Tiempo de las llamadas en el hilo principal", "D2", 3, 100, "O", 50, "A", "E9"),
        ("C4", "Esfuerzo de adoptarla desde hoy", "BD1", 2, 100, "O", 0, "J", "E1"),
        ("C5", "Familiaridad con la practica habitual de Unity", "BD2", 1, 0, "J", 100, "J", "E3"),
        ("C6", "Datos editables en el inspector por no programadores", "BD2", 1, 0, "J", 100, "D", "E3"),
    ]),
    "DEC2": ("Registro de eventos que la vista anima (A1) frente a logica guiada por corrutinas y animaciones (A2)", [
        ("C1", "Fidelidad: el registro basta para reproducir el combate", "D4", 3, 100, "O", 50, "A", "E7"),
        ("C2", "Resultado independiente de la velocidad de la animacion", "D3", 3, 100, "O", 0, "J", "E8"),
        ("C3", "Simulacion sin vista (miles de runs con el bot)", "D1", 3, 100, "O", 50, "A", "E2, E8"),
        ("C4", "Sobrecosto de tiempo y memoria", "D2", 1, 100, "O", 100, "A", "E8"),
        ("C5", "Sencillez al agregar una animacion nueva", "BD2", 2, 50, "J", 100, "J", "E7"),
    ]),
    "DEC3": ("Generador propio por instancia con estado serializable (A1) frente a generador compartido de Unity (A2)", [
        ("C1", "Reproducibilidad robusta ante tiradas de la vista", "D3", 3, 100, "O", 0, "S", "E4, E6"),
        ("C2", "Guardar y restaurar el estado del azar", "D3", 3, 100, "O", 50, "D", "E5, E10"),
        ("C3", "Estabilidad entre versiones y plataformas", "D3", 2, 100, "O", 50, "D", "E4, E12"),
        ("C4", "Calidad estadistica y velocidad", "D2", 1, 100, "O", 100, "O", "E5"),
        ("C5", "Costo de mantener codigo propio", "BD2", 2, 50, "J", 100, "J", "E5"),
    ]),
}


def puntajes(crit, pesos):
    s1 = sum(p * c[4] for p, c in zip(pesos, crit))
    s2 = sum(p * c[6] for p, c in zip(pesos, crit))
    return s1, s2


def pesos_base(crit):
    t = sum(c[3] for c in crit)
    return [c[3] / t for c in crit]


def main():
    filas, sens = [], []
    rnd = random.Random(2026)
    resumen = {}
    for did, (titulo, crit) in DECISIONES.items():
        w = pesos_base(crit)
        s1, s2 = puntajes(crit, w)
        for c, p in zip(crit, w):
            filas.append([did, c[0], c[1], c[2], c[3], "%.4f" % p, c[4], c[5], c[6], c[7], c[8]])
        filas.append([did, "SCORE", "", "", "", "", "%.1f" % s1, "", "%.1f" % s2, "", ""])
        # (a) un peso a la vez x0 y x2
        ganaA1 = 0
        total = 0
        for i, c in enumerate(crit):
            for f in (0, 2):
                pw = list(w); pw[i] *= f
                t = sum(pw); pw = [x / t for x in pw]
                a, b = puntajes(crit, pw)
                total += 1
                ganaA1 += 1 if a > b else 0
                sens.append([did, "x%d en %s" % (f, c[0]), "%.1f" % a, "%.1f" % b, "A1" if a > b else ("A2" if b > a else "empate")])
        # (b) Dirichlet
        gana = 0
        N = 10000
        for _ in range(N):
            g = [rnd.gammavariate(1.0, 1.0) for _ in crit]
            t = sum(g); pw = [x / t for x in g]
            a, b = puntajes(crit, pw)
            gana += 1 if a > b else 0
        # (c) factor de vuelco: criterio que favorece a A2 (puntaje A2 > A1) -> cuanto hay que multiplicar su peso para que A2 supere
        vuelcos = []
        for i, c in enumerate(crit):
            if c[6] <= c[4]:
                continue
            lo, hi = 1.0, 1000.0
            def margen(f):
                pw = list(w); pw[i] *= f
                t = sum(pw); pw = [x / t for x in pw]
                a, b = puntajes(crit, pw); return a - b
            if margen(hi) > 0:
                vuelcos.append((c[0], None))
                continue
            for _ in range(60):
                mid = (lo + hi) / 2
                if margen(mid) > 0: lo = mid
                else: hi = mid
            vuelcos.append((c[0], hi))
        # (d) pesos iguales
        pi = [1.0 / len(crit)] * len(crit)
        a, b = puntajes(crit, pi)
        pi_a, pi_b = a, b
        sens.append([did, "pesos iguales", "%.1f" % a, "%.1f" % b, "A1" if a > b else ("A2" if b > a else "empate")])
        # (e) favorable a A2: todo puntaje de A2 sin medicion (tipos D documental y A supuesto) sube a 100; los juicios explicitos (J) y las mediciones no cambian
        # (f) extremo: ademas los juicios J de A2 suben a 100 (mejor caso imaginable para A2; no es un escenario plausible, marca el limite)
        res_fav = []
        for etiqueta, tipos in (("favorable a A2 (puntajes A2 de tipo D y A = 100)", ("D", "A")), ("extremo (puntajes A2 de tipo D, A y J = 100)", ("D", "A", "J"))):
            fav = [list(c) for c in crit]
            for c in fav:
                if c[7] in tipos:
                    c[6] = 100
            a, b = puntajes(fav, w)
            res_fav.append((a, b))
            sens.append([did, etiqueta, "%.1f" % a, "%.1f" % b, "A1" if a > b else ("A2" if b > a else "empate")])
        fav_a1, fav_a2 = res_fav[0]
        ext_a2 = res_fav[1][1]
        sens.append([did, "Dirichlet 10000 sorteos: A1 gana", "%.1f %%" % (100.0 * gana / N), "", ""])
        for cid, f in vuelcos:
            sens.append([did, "factor de peso de %s para que A2 supere a A1" % cid, ("x%.1f" % f) if f else "nunca (aunque x1000)", "", ""])
        resumen[did] = dict(A1=round(s1, 1), A2=round(s2, 1), dirichlet_A1=round(100.0 * gana / N, 1), iguales_A1=round(pi_a, 1), iguales_A2=round(pi_b, 1), fav_A1=round(fav_a1, 1), fav_A2=round(fav_a2, 1), ext_A2=round(ext_a2, 1), oat_A1=ganaA1, oat_total=total,
                            vuelcos=[(a, None if b is None else round(b, 1)) for a, b in vuelcos])
    with open(os.path.join(CARPETA, "mcda_resultados.csv"), "w", newline="", encoding="utf-8") as f:
        wr = csv.writer(f, delimiter=";")
        wr.writerow(["decision", "criterio", "nombre", "driver", "prioridad", "peso", "puntaje_A1", "tipo_A1", "puntaje_A2", "tipo_A2", "evidencia"])
        wr.writerows(filas)
    with open(os.path.join(CARPETA, "mcda_sensibilidad.csv"), "w", newline="", encoding="utf-8") as f:
        wr = csv.writer(f, delimiter=";")
        wr.writerow(["decision", "escenario", "score_A1", "score_A2", "ganadora"])
        wr.writerows(sens)
    with open(os.path.join(CARPETA, "mcda_resumen.json"), "w", encoding="utf-8") as f:
        json.dump(resumen, f, indent=2, ensure_ascii=False)
    print(json.dumps(resumen, indent=2, ensure_ascii=False))
    try:
        import matplotlib
        matplotlib.use("Agg")
        import matplotlib.pyplot as plt
        fig, ax = plt.subplots(1, 3, figsize=(10, 3.4))
        for a, (did, r) in zip(ax, resumen.items()):
            a.bar(["A1", "A2"], [r["A1"], r["A2"]], color=["#2d6a4f", "#9aa5b1"])
            for i, v in enumerate([r["A1"], r["A2"]]):
                a.text(i, v + 2, "%.1f" % v, ha="center", fontsize=9)
            a.set_ylim(0, 110); a.set_title("%s (A1 gana %.0f %% de 10 000 pesos)" % (did, r["dirichlet_A1"]), fontsize=8.5)
            a.set_ylabel("Score (0-100)" if did == "DEC1" else "")
            a.spines[["top", "right"]].set_visible(False)
        fig.tight_layout()
        fig.savefig(os.path.join(CARPETA, "mcda_figura.png"), dpi=200)
    except Exception as e:  # la figura es opcional
        print("sin figura:", e)


if __name__ == "__main__":
    main()
