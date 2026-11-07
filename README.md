# Gatomate — prototipo funcional (título de trabajo)

Roguelike táctico de gatos por turnos, hecho a partir del documento "Gatomate: concepto de juego" y de lo que se pidió después (habilidades por gato, movilidad, pasivas, elementos, terrenos, obstáculos, enemigos nuevos, dinero).
Tiene **vista isométrica, interfaz superpuesta al juego, animaciones, efectos y sonido**, todo generado por código (no hay imágenes ni audios en el proyecto). Las reglas (carpeta `Core`) no dependen de Unity y están probadas fuera de Unity.

## Cómo abrirlo en Unity
1. Crea un proyecto nuevo en Unity (2D o 3D; recomendado Unity 2020 LTS o más nuevo).
2. Copia la carpeta `Assets/Scripts` de este zip dentro de la carpeta `Assets` de tu proyecto.
3. Abre cualquier escena (aunque esté vacía) y pulsa **Play**. El juego se crea solo (cámara, escena, interfaz y audio; no hay que arrastrar nada).
4. En la ventana Game usa 16:9 (o "Free Aspect" ancho). La interfaz se escala sola; con ventanas muy angostas el registro lateral se oculta.

**Importante (honesto):** en el entorno donde lo programé no hay Unity, así que **nunca se ejecutó dentro de Unity**. Lo que sí se verificó: las reglas (106 comprobaciones + simulaciones con bot), que toda la vista compila contra una réplica de la API de Unity (hecha de memoria), que los sprites se dibujan bien (se renderizaron a PNG y se revisaron, incluida una escena isométrica con el terreno nuevo) y que los sonidos no tienen valores inválidos. **No se pudo comprobar en vivo**: las pantallas nuevas (elegir habilidad, tienda), la barra de combate con dos habilidades, las animaciones nuevas (saltos, embestidas, explosiones, monedas, cajas que se rompen), posiciones/tamaños de la interfaz y tiempos. Si algo no compila o se ve mal, mándame el mensaje de la consola o una captura.

## Cómo se juega
- **Título**: Juego nuevo (Tutorial o Nueva run con semilla, mundos 1/3/7 y clases 8 o 4), Cargar juego, Opciones y Salir. La run se **guarda sola** al entrar en la preparación de cada nivel (no en el tutorial).
- **Tutorial** (propuesto, por confirmar): una run de 3 niveles con 8 gatos (4 en la expedición, 4 en reserva) que explica gatos, habilidades, movimiento, ataque básico, clic derecho, cajas, monedas, empujes, elementos, objetos, anillos/gemas, tienda y jefe. Siempre se puede saltar el paso, ocultar la tarjeta o salir.
- **Elegir habilidades (al empezar la run y cada vez que consigues un gato)**: cada gato elige **2 habilidades**, una por una. Cada elección te muestra **3 opciones** al azar de las 6 de su clase. Puedes **repetir las 3 opciones por 3 monedas**.
- **Preparación** de cada nivel: tus 4 gatos como tarjetas (con sus 2 habilidades y su pasiva), la reserva, el equipamiento, vista previa de enemigos y el inventario. Click en un gato para intercambiarlo con la reserva; equipa (máx. 2 por gato). Botón EMPEZAR.
- **Mapa del mundo** (entre niveles; propuesto, por confirmar): muestra el mundo actual con sus paradas **Campamento → Nivel 1 → Campamento → Nivel 2 → Campamento → Jefe**, pestañas para los mundos 1–7 y una ficha del gato-grupo que **avanza animada** de una parada a la otra. Al elegir un nivel ves la **composición enemiga** (con debilidades y resistencias) y el tamaño del tablero para decidir qué gatos llevar. Los campamentos son la preparación (cambiar gatos, equipar, tienda); el botón *IR AL CAMPAMENTO* te lleva a ella.
- **Combate** (tablero de tamaño variable, de ~7×7 a 12×12, isométrico; la cámara se ajusta). Casillas azules = a donde puedes moverte, rojas = objetivos, naranjas = área del efecto; al apuntar se ve la previsión de daño (y si es "por la espalda", "débil" o "resiste"). Barra superior con el orden de turnos, tarjeta de la unidad bajo el cursor, barra de acciones abajo y registro de combate.
- **Recompensa**: aparece **1 solo desafío** (misterioso: no dice la estadística ni la recompensa) y eliges qué gato lo intenta. Cada gato muestra solo un **+ verde** (probablemente sale bien), un **− rojo** (probablemente sale mal) o **nada** (apuesta); sin colores ni porcentajes.
- **Tienda** entre niveles: compras con las monedas que recogiste (objetos, un equipamiento, un banquete que cura).
- 3 vidas por run; si caen los 4 gatos pierdes una vida y se reinicia el nivel (mismo tablero).

### Controles
| Tecla / ratón | Acción |
|---|---|
| Click izquierdo | Elegir casilla / objetivo / botón |
| Click derecho en un gato/enemigo | **Inspeccionar**: casillas a las que puede llegar (naranja), casillas que podría atacar (rojo suave), y en el panel su ataque en palabras clave, **débil / resiste** y movimiento |
| Click derecho en una **casilla** | Ver qué es: terreno y su efecto, resistencia de un obstáculo, monedas y zonas (otro clic derecho la cierra) |
| `Esc` | Cancelar la selección |
| `\|` (o `F10`) | **Menú de desarrollador** (ver abajo) |
| Pasar el ratón por una habilidad | Globo corto: nombre, coste y palabras clave con icono |
| Clic en una habilidad (tarjetas, fichas) / clic derecho (barra de acciones, porque ahí el clic la elige) | **Descripción completa** |
| `A` | Ataque básico (gratis) |
| `Q` | Mover |
| `W` / `E` / `R` / `S` | Habilidad 1 / 2 / 3 / 4 del gato (las 3 y 4 llegan con el nivel) |
| `1`–`8` | Consumibles (en el orden de la barra de objetos) |
| `Espacio` / `Enter` | Terminar turno |
| `T` | Auto turno (el bot juega por ti) |
| `F` | Velocidad de animación (x1 / x2 / x3) |
| `M` | Silenciar / activar sonido |

El registro del combate está oculto por defecto (botón *Registro* arriba).

## Reglas nuevas 

### Habilidades y pasivas
- Cada clase tiene **6 habilidades**, **1 pasiva** y un **ataque básico gratis** (tecla `A`, no cuenta entre las 2 elegidas, usa la acción del turno). Cada gato se queda con **2 habilidades** (no hay niveles: siempre 2).
- **Ataque básico** (propuesto, por confirmar): Zarpa (luchador), Chispa (mago), Topetazo (tanque), Tiro corto (cazador), Báculo (clérigo), Navaja (pícaro), Zarza (druida), Probeta (alquimista). Hacen poco daño (poder 1 o 2 + **la mitad** del stat). La pasiva del cazador sigue valiendo (rango mínimo 2).
- **Palabras clave con icono**: las descripciones largas se sustituyeron por claves (por ejemplo `+3 daño`, `espalda`, `en cruz`, `alrededor`, `gratis`, `libre`). Cada clave tiene su propio globo explicativo. La descripción completa sigue disponible con un clic.
- **Movimiento por clase** (propuesto, por confirmar; la velocidad ahora solo decide el orden de turno): Pícaro 6, Cazador 5, Luchador 4 (el medio que pediste), Druida 4, Mago 3, Clérigo 3, Alquimista 3, Tanque 2. Se cambia en `GameData.cs` (parámetro `mov` de `AddClase`).
- **Habilidades de movilidad**: cuestan solo maná y **no gastan la acción del turno** (puedes moverte con una, y luego atacar). Son teletransportes, saltos, intercambios con aliados y avances en línea recta.
- Mana: empieza lleno y sube 1 por turno. **Ojo:** tu ejemplo era Paso sombrío con 4 de maná; lo dejé en **3** porque el pícaro tiene 4 de maná máximo y con 4 no podría apuñalar el mismo turno (con 3 le queda 1 para Puñalada; y como solo recupera 1 por turno, no lo repite cada turno). Se cambia en `GameData.cs` (`paso_sombrio`).
- **Orientación**: cada unidad mira hacia donde se movió o atacó por última vez. Atacar por la espalda (el lado contrario a su orientación) activa los bonos de espalda.

| Clase | Pasiva | Habilidades (maná) |
|---|---|---|
| Luchador | **Impulso**: +2 de daño si se movió 3 o más casillas antes de atacar | Zarpazo cargado (1, empuja 1), Carga (2, corre y golpea), Tajo giratorio (2, alrededor), Rugido de guerra (1, +2 de daño a un aliado), **Salto felino** (2, movilidad), Golpe demoledor (3, empuja 2) |
| Mago | **Canalizar**: +1 de daño elemental; sus zonas duran 1 turno más | Bola de fuego (3), Rayo (3), Lanza de hielo (2), **Parpadeo** (2, movilidad), Onda de choque (3, empuja 2), Muro de fuego (3) |
| Tanque | **Inamovible**: no puede ser empujado y recibe 1 menos de daño cuerpo a cuerpo | Provocar (1, **a cualquier enemigo sin importar la distancia**), Embestida de escudo (2, empuja 2), Baluarte (2, +3 def a un aliado), Sacudida (2, alrededor, empuja 1), **Intercambio protector** (1, movilidad), **Arremetida** (1, movilidad) |
| Cazador | **Francotirador**: +2 de rango máximo, pero **no puede atacar a enemigos pegados** (rango mínimo 2) | Disparo certero (2), Flecha de fuego (3), Flecha venenosa (2), **Voltereta** (1, movilidad), Trampa de lazo (2), Flecha de impacto (3, empuja 1) |
| Clérigo | **Aura**: al terminar su turno cura 1 a los aliados vecinos | Plegaria (3), Castigo divino (2), Bendición (2), **Paso divino** (2, movilidad: cambia de lugar con un aliado), Nova sagrada (4, cura en área), Purificar (1) |
| Pícaro | **Puñalada trasera**: +3 de daño por la espalda | Puñalada (1), **Paso sombrío** (3, movilidad: se teletransporta detrás de un enemigo), Daga envenenada (2), Cuchillos arrojadizos (2), **Pirueta** (1, movilidad), Golpe bajo (3, +4 por la espalda) |
| Druida | **Hijo del bosque**: se cura 1 al empezar su turno en hierba alta o flores; barro y agua no le cuestan extra | Enredadera (2), Látigo de zarzas (2), Brote curativo (2), **Viaje de raíz** (2, movilidad: a hierba o flores), Llamado del trueno (3, rayo), Nube de esporas (2) |
| Alquimista | **Maestría**: las pociones y objetos que usa rinden +1 | Frasco (2), Frasco incendiario (3), Frasco de escarcha (3), Frasco tóxico (2), **Burbuja saltarina** (2, movilidad), Elixir de fuerza (2, +3 de daño a un aliado) |

### Menú de desarrollador (tecla `|` o `F10`)
Panel a la derecha, en cualquier pantalla de la run: **ganar / perder el nivel al instante**, ir directo a un **mundo y nivel**, abrir la **tienda** o la **recompensa**, sumar monedas, dar consumibles, dar cualquiera de los **50 objetos** (o uno al azar), curar o sumar vida, **+1 nivel** a los gatos, añadir un **gato nuevo**, y en combate curar / matar enemigos.

### Niveles de gato
- Cada gato gana **1 de experiencia por enemigo que derrota él mismo** (jefe: 3; las ratas invocadas no dan). La experiencia se reparte **al ganar el combate**. Cada nivel sube **+2 de vida**, y los niveles **3 y 6** dan una **habilidad extra** (hasta 4 habilidades; se elige entre 3 de su clase). Nivel máximo 10.
- Los gatos nuevos llegan con el **nivel medio de tu expedición menos 0–1**.
- Un gato en la **reserva se cura 1 de vida por ronda de cada combate ganado**, para poder rotarlo.

### Objetos equipables (propuesto, por confirmar)
Hay **50 objetos** en **2 espacios por gato: Cuerpo y Arma** (25 de cada uno). Equipar uno en un espacio ocupado devuelve el anterior al inventario; también se puede **desequipar**. Hay sinergias con clases, elementos, habilidades (área, empuje, movilidad, cura, veneno, espalda...), terreno y monedas. Los que ya existían (collares, chaleco, anillos, gemas...) quedaron repartidos entre los 50. La lista completa está en `Core/Objetos.cs`.

### Objetos que dan sinergias (propuesto, por confirmar)
- **Anillos** (fuego, hielo, rayo, veneno): los ataques **físicos** del gato pasan a ser de ese elemento.
- **Gemas**: +2 de daño con ataques de su elemento; las de fuego y veneno alargan 1 turno sus zonas.
- Collares (+Fue/+Int/+Des), chaleco, trébol y cascabel (+1 de movimiento) siguen igual. Máximo 2 objetos por gato (uno de Cuerpo y uno de Arma).

### Elementos y estados
| Elemento | Efecto |
|---|---|
| Fuego | Prende la hierba alta en la casilla y sus 4 vecinas: queda fuego un par de rondas y la hierba desaparece del tablero |
| Hielo | **Ralentizado**: −2 de movimiento 1 turno; si el objetivo está en el agua queda **Inmovilizado** |
| Rayo | +3 de daño si el objetivo está en el agua; además la descarga pasa a la mitad de daño a los enemigos que estén en el agua a 2 casillas o menos |
| Veneno | **Envenenado**: 2 de daño al empezar cada turno, 3 turnos. También hay nubes de veneno en el suelo |
- Algunos enemigos son **débiles** (+50% de daño) o **resisten** (−50%) a un elemento; la previsión del daño lo muestra.

### Fuego amigo 
Los **ataques en área de tus gatos** (cruz, alrededor y bombas) también hieren a **tus otros gatos** que estén dentro, con **la mitad del daño**. Quien lanza no se daña, y los enemigos no se dañan entre sí. El *Amuleto de lealtad* da inmunidad. Aparece como la palabra clave `amigo ½` en las habilidades de área.

### Terreno y obstáculos
- **Tableros de tamaño variable** y con distintas disposiciones de salida: los gatos y los enemigos ya no arrancan siempre en la misma esquina y **los enemigos siempre aparecen agrupados**.
- **Agua**: forma **charcos de al menos 2×2** o **ríos de 1 casilla de ancho continuos**; nunca una casilla suelta. El **barro** puede ser una casilla sola, pero tiende a estar **cerca del agua**.
- **Todos los obstáculos se rompen**, unos con más vida que otros: roca agrietada 8, **roca normal 16 (el doble)**, **roca grande 24 (el triple)**, caja 4, barril 3. Al dañarlos se oscurecen según su vida restante.
- **Barro**: cuesta 2 de movimiento por casilla. **Agua**: cuesta 3. (Si te sobra menos movimiento del que cuesta la casilla, no puedes entrar.) El druida no paga extra.
- Detalle de los obstáculos (cuentan como muros hasta que se rompen; el nivel siempre sigue siendo recorrible):
  - **Caja** (4 de vida): suelta monedas.
  - **Roca agrietada** (8 de vida): abre camino.
  - **Barril** (3 de vida): explota al romperse (6 de fuego en cruz, daña a todos y prende la hierba).
- **Empujes**: algunas habilidades empujan 1 o 2 casillas. Si el empujado choca con un muro, un gato o un enemigo, recibe daño (2 + casillas que le quedaban por recorrer) y el que está al otro lado también; los obstáculos rompibles se dañan. El tanque no se puede empujar.
- **Monedas**: hay montoncitos en el suelo (se recogen pasando o cayendo encima), las cajas sueltan monedas y los enemigos a veces dejan una (35%) y los jefes sueltan 5.

### Enemigos nuevos
| Enemigo | Qué hace |
|---|---|
| Nido de ratas | No se mueve; **suelta una rata cada turno** (máx. 6 vivas) |
| Chamán | Invoca una rata cada 4 turnos (máx. 2) y ataca a distancia |
| Gato bomba | Corre hacia un gato y explota (5 de fuego en cruz; también si lo matan) |
| Slime de fuego / de hielo | Resisten su elemento y son débiles al contrario |
| Sapo venenoso | Ataca a rango 2 y envenena |
| Anguila eléctrica | Ataca con rayo (más daño a quien esté en el agua) |
| Rata ladrona | Va a por las monedas del suelo; si muere suelta lo que llevaba |

### Dinero y tienda
- Las monedas del nivel se suman a la run al ganarlo. **Las usas en la tienda de entre niveles** y para **repetir las 3 opciones de habilidad (3 monedas)**. *(Tu respuesta a "¿para qué sirve el dinero?" llegó como texto ilegible, así que elegí esto yo; es fácil de cambiar.)*
- Tienda: 3 consumibles al azar (2–5 monedas), 1 equipamiento (7), el banquete de pescado (8, cura el 50% a todos) y **Descanso (30 monedas): todos los gatos duermen y se curan por completo**.

## Qué se ve y se oye
- **Vista isométrica** (rombos de 128×64): casillas por mundo (7 paletas), rocas, muros, **agua, cajas, rocas agrietadas, barriles, monedas**, hierba alta que se mece, flores, barro, sombras y orden de profundidad.
- **Animaciones** (el Core resuelve la acción al instante y deja un registro de eventos; la vista los reproduce en orden): saltos casilla a casilla, cada habilidad con su animación, teletransportes (arco o desvanecimiento), embestidas, empujes y choques, explosiones, objetos que se rompen, monedas, daño con color por elemento, esquivas, curaciones, muertes, invocaciones, zonas, tormenta y confeti.
- **Interfaz encima del juego** (IMGUI/OnGUI) con paneles crema y morado oscuro, botones gordos, retratos, iconos, tooltips, banners y fundidos. **Cada habilidad tiene su icono**: el color indica el elemento y el dibujo qué hace; las de movilidad tienen el botón verde en la barra de acciones.
- **Sonido sintetizado por código**: efectos para cada acción (incluidos los nuevos) y 3 músicas. Se generan en un hilo al abrir el juego, así que la música puede tardar unos segundos en sonar.

## Estructura del código (Assets/Scripts)
```
Core/   Reglas del juego. No usa nada de Unity (se puede probar fuera de Unity).
  Config.cs        Todos los números generales (tamaño, vidas, costes de terreno, daños de elementos, precios...).
  GameData.cs      Clases (6 habilidades + pasiva), enemigos, jefes y mundos como datos en UN solo lugar.
  Datos.cs         Tipos de datos y enumeraciones (terrenos, elementos, pasivas, efectos de habilidad...).
  Eventos.cs       Registro de eventos de combate (lo que la vista anima).
  Textos.cs        Nombres y descripciones (las de las habilidades se generan solas desde sus datos).
  Tablero.cs       Casillas, terreno, obstáculos rompibles, monedas y zonas.
  GeneradorNivel.cs  Tablero procedural + colocación de gatos, enemigos, obstáculos y monedas a partir de una semilla.
  Combate.cs       Turnos, movimiento con coste de terreno, habilidades, elementos, empujes, consumibles, fin del nivel.
  IA.cs            IA de enemigos y jefes (invocadores, kamikaze, ladrón).
  Desafios.cs      Desafíos de recompensa y su probabilidad.
  Run.cs           Campaña: fases (incluye elegir habilidad y tienda), vidas, reserva, inventario, dinero.
  BotJugador.cs    Jugador automático (simulaciones y botón "Auto").
  Util.cs          Posición y generador de azar con semilla (guardable).
  TextosClaves.cs  Palabras clave con icono y su glosario.
  Tutorial.cs / RunTutorial.cs / GeneradorTutorial.cs   Pasos del tutorial, run del tutorial y sus 3 niveles fijos.
  Objetos.cs / Progreso.cs / RunDebug.cs   Los 50 objetos y sus efectos, niveles de gato, y la API del menú de desarrollador.
  GeneradorNivel.cs   Tableros de tamaño variable, agua en charcos/ríos, disposición de gatos y enemigos (también para la vista previa del mapa).
  RunGuardado.cs   Guardar y cargar la run (texto).
View/   (solo esta carpeta toca Unity)
  GatomateJuego.cs Arranca solo; pantallas y entrada (preparación, combate, recompensa, elegir habilidad, tienda, final). Es `partial`:
  GatomateMenus.cs     Título (juego nuevo, cargar, opciones, salir), opciones y guardado automático (PlayerPrefs).
  GatomateTutorial.cs  Tarjeta/ventana del tutorial en pantalla.
  GatomateMapa.cs      Mapa del mundo entre niveles (paradas, ficha animada, vista previa de enemigos).
  GatomateDebug.cs     Menú de desarrollador (tecla | o F10).
  GatomateInspeccion.cs  Tarjetas limpias de gato y unidad, ficha completa de habilidad, clic derecho (alcance y amenaza).
  EscenaIso.cs     Escena isométrica y reproducción de animaciones por evento.
  Interfaz.cs      Herramientas de interfaz (paneles, botones, barras, retratos, tooltips).
  Fx.cs            Partículas, anillos, textos flotantes, temblor de cámara.
  Arte.cs          Caché de sprites y texturas.
  Dibujos.cs / DibujosNuevos.cs   Dibujo por código de gatos, enemigos, jefes, casillas, obstáculos, monedas e iconos.
  Pintor.cs        Mini lienzo (supersampling, contorno) usado por Dibujos.
  Sonido.cs        Sintetizador de efectos y músicas + reproductor.
```
La vista solo llama a `Run` y `Combate`; las reglas no dependen de la vista.

### Cómo cambiar cosas
- **Una habilidad** (coste, rango, poder, elemento, empuje, si es de movilidad): `GameData.cs`; su texto de ayuda se genera solo.
- **Cuántas habilidades tiene cada gato / cuántas opciones se muestran / precio de repetir**: `Config.HabilidadesPorGato`, `OpcionesDeHabilidad`, `CosteRepetirOpciones`.
- **Coste de barro/agua, vida de cajas, daño de choque, veneno, precios**: `Config.cs`. La tienda se apaga con `Config.TiendaActiva = false`.
- **Arte**: todo pasa por `Arte.cs`; cada función devuelve un `Sprite`/`Texture2D`, así que se puede cambiar su cuerpo para cargar PNG desde `Resources` (mismo `id` que en `GameData.cs`).
- **Audio**: `Sonido.Tocar("id", vol, tono)` y `Sonido.Musica(...)`.
- **Animaciones**: `EscenaIso.cs`, un método por tipo de evento (`EvMover`, `EvTeletransporte`, `EvExplosion`, ...).

## Herramientas (fuera de Unity)
`Herramientas/Simulacion` (necesita .NET 8): juega muchas runs con el bot y resume el balance.
```
cd Herramientas/Simulacion
dotnet run -c Release -- 400 7            # 400 runs, 7 mundos
dotnet run -c Release -- 400 1 4clases    # solo mundo 1, 4 clases
dotnet run -c Release -- pruebas          # comprobaciones de reglas (106)
dotnet run -c Release -- log 2 3 5 luchador,mago,tanque,clerigo   # registro de un combate
```
- `Herramientas/ChequeoVista`: réplica mínima de la API de Unity (hecha de memoria) que solo sirve para comprobar que la vista compila. **No es Unity.**
- `Herramientas/VistaPrevia`: dibuja los sprites (incluida la hoja del arte nuevo y los iconos de habilidades) y escenas isométricas a PNG sin abrir Unity.
- `Herramientas/PruebaSonido`: genera los sonidos a WAV y revisa saturación.
- `Herramientas/compilar.sh`: compila con el compilador de C# directo (útil si `dotnet build` no puede restaurar paquetes).

## Resultado de las simulaciones (bot sencillo)
Con el ataque básico y el movimiento por clase el juego se volvió **mucho más fácil** para el bot (casi 60% de campañas, contra 24,5% antes, antes de añadir niveles de gato y objetos). Para compensar, **subí la vida y el poder de los enemigos de los mundos 2 a 7** (propuesto, por confirmar; `BonusVida` y `BonusPoder` en `GameData.cs`). Con los niveles de gato, los 50 objetos y vida/poder extra en los enemigos, el bot sencillo completa la campaña en **~35%** (80 runs, 7 mundos, 8 clases; la expedición termina en nivel medio ~2,3). **Es solo un ajuste provisional: el balance se hace después de cerrar las mecánicas.** El mundo 1 quedó igual.
- Los números cambian con un jugador humano (que combina movilidad, empujes, terreno y objetos mejor que el bot); sirven para ver tendencias, no valores finales.
- Los 199 chequeos del Core pasan (incluye ataque básico, movimiento, anillos y gemas, claves, guardado/carga y que el bot supere el tutorial).
