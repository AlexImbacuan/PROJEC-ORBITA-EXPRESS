# Historia de Órbita Express

Documento de referencia del equipo para que cinemáticas, textos, niveles y menús cuenten la misma historia. Lo marcado como **(propuesta)** todavía no está decidido: si alguien lo cambia, que actualice este archivo.

## Resumen en una frase

Un repartidor novato de una pizzería galáctica recibe la ruta más peligrosa del sector y tiene que entregar todos los pedidos **calientes e intactos** cruzando planetas helados, lunas de baja gravedad y campos de asteroides.

## El origen de Órbita Express

Tras la expansión de las estaciones orbitales y las colonias de investigación del sistema exterior, los trabajadores espaciales tenían un problema serio: la comida deshidratada era insoportable y ninguna empresa se atrevía a cruzar sectores inestables para llevar comida caliente.

Así nació **Órbita Express**, una pizzería galáctica famosa por su lema:

> **"Ni el cero absoluto enfría nuestro queso."**

La empresa se apoya en hornos de fusión rápida y cajas de reparto con aislamiento térmico de alta resistencia.

## El turno de la Ruta Crítica

Durante el turno de máxima demanda, una tormenta de meteoritos deja fuera de servicio a los pilotos veteranos. Te asignan la ruta más exigente del sector. Con tu moto espacial de propulsión gravitacional, tienes que demostrar que estás a la altura del puesto con una cadena de entregas sin margen de error.

Al entregar el último pedido intacto y caliente en el destino final, aseguras la reputación de la franquicia y completas tu turno con éxito.

## Personajes

| Personaje | Quién es | Dónde aparece |
|---|---|---|
| **El repartidor** (nombre que escribe el jugador) | Novato de Órbita Express. Lo llaman de urgencia en su día libre. | Todo el juego |
| **El Jefe** | Dueño de la sucursal. Gruñón, presionado por las pérdidas: ya tuvo que regalar demasiadas pizzas por entregas tardías. Habla por mensajes al celular del repartidor. | Cinemática de salida, mensajes durante el nivel **(propuesta)** |
| **Sra. Luna** | Cliente | Nivel 1 |
| **Don Cometa** | Cliente | Nivel 1 |
| **Familia Estrella** | Cliente | Nivel 1 |

## Cómo se cuenta la historia en el juego

| Momento | Escena | Qué cuenta |
|---|---|---|
| 1. Intro | `CimaMontana` | El repartidor descansa en la cima de una montaña nevada. Lo llaman por la emergencia y se lanza del risco para llegar rápido al trabajo. |
| 2. Menú | `MenuPrincipal` | El jugador escribe su nombre: es su **credencial de repartidor**. |
| 3. Salida | `PruebaEntregas` (cinemática) | Llega a la pizzería. El Jefe le escribe: *"¡Apúrate, que la pizza se enfría y no podemos regalar más pizzas!"*. Recoge los pedidos, se sube a la moto y despega. |
| 4. Juego | `Nivel1` | Primera ruta de entregas en la colonia helada donde está la pizzería. |
| 5. Final | Pantalla de fin de ruta **(propuesta)** | Estrellas, propinas y un mensaje del Jefe según el resultado. |

## Mundos y mecánicas

Cada mundo de la historia tiene una mecánica que lo justifica:

| Mundo | Historia | Mecánica |
|---|---|---|
| **Planetas helados** | Antiguas bases científicas cubiertas de hielo. La sucursal de Órbita Express está en una de ellas. | Poca fricción: los giros bruscos hacen derrapar la moto y golpear la caja (Physic Material con poca fricción). |
| **Lunas de baja gravedad** | Satélites rocosos llenos de cráteres y rampas. | Cada rampa lanza la moto por los aires y hay que controlar la inclinación para aterrizar sin dañar la pizza (gravedad variable). |
| **Campos de asteroides** | Tramos de vacío entre planetas. | El tiempo corre y el frío del espacio enfría el pedido más rápido si pierdes impulso esquivando rocas. |

## Reglas del mundo (para que todo sea coherente)

- **La pizza se enfría con el tiempo** (temperatura) y **se daña con los golpes** (estado de la caja). Un pedido frío o golpeado se entrega igual, pero con menos estrellas y menos propina.
- **Las entregas se hacen a pie**: el repartidor se baja de la moto para entregar.
- **La moto flota**: no tiene ruedas, usa propulsión gravitacional.

## Tono

Humor ligero, estilo caricatura. La presión es cómica (el Jefe gruñón, el queso que "no se enfría"), no dramática.

## Ideas pendientes **(propuesta)**

- Nombre del planeta helado donde está la pizzería.
- Mensajes del Jefe según el resultado: 5 estrellas → *"¡Por fin alguien competente!"*; 1 estrella → *"Esa pizza la vas a pagar tú."*
- Frases cortas de cada cliente al recibir el pedido.
- Mostrar el lema en el menú, debajo del título.
