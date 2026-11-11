# Project Órbita Express

Proyecto de Unity desarrollado para el **Taller 2**.

## Descripción

**Órbita Express** es un videojuego 3D en tercera persona desarrollado en Unity.
El jugador es un repartidor de la empresa Órbita Express que debe completar una
ruta de entregas por distintos planetas, viajando en su moto espacial tanto por
la superficie de cada planeta como por el espacio entre ellos.

Cada destino tiene condiciones diferentes: planetas helados con suelo
resbaladizo, lunas de baja gravedad, campos de asteroides y otros entornos que
cambian la forma de manejar. El jugador debe entregar cada pedido a tiempo y en
buen estado antes de que se enfríe o se dañe. El nivel termina cuando se
completa la última entrega de la ruta.

**Temas aplicados:** físicas (Rigidbody, colliders, Physic Materials, gravedad
variable), personaje animado con Animator, prefabs, sistemas de partículas,
iluminación, audio dinámico y control de versiones con Git.

## Requisitos

| Herramienta | Versión |
|---|---|
| Unity Editor | **6000.5.8f1** (Unity 6) |
| Render Pipeline | Universal Render Pipeline (URP) 17.5.0 |
| IDE (opcional) | Visual Studio o JetBrains Rider |
| Git | Cualquier versión reciente |

> Se recomienda instalar Unity a través de **Unity Hub** usando exactamente la versión indicada para evitar reimportaciones o conflictos.

## Instalación y ejecución

1. Clonar el repositorio:
   ```bash
   git clone https://github.com/AlexImbacuan/PROJEC-ORBITA-EXPRESS.git
   ```
2. Abrir **Unity Hub** → **Add** → **Add project from disk** y seleccionar la carpeta clonada (`PROJEC-ORBITA-EXPRESS`).
3. Abrir el proyecto con la versión **6000.5.8f1**. La primera apertura puede tardar mientras Unity genera la carpeta `Library/`.
4. Abrir la escena `Assets/Scenes/SampleScene.unity` y presionar **Play**.

## Estructura del proyecto

```
project-orbita-express/
├── Assets/
│   ├── Scenes/                       # Escenas del juego
│   ├── Settings/                     # Configuración de URP (PC y Mobile), Volume Profiles
│   ├── TutorialInfo/                 # Contenido de la plantilla de Unity (se puede eliminar)
│   └── InputSystem_Actions.inputactions  # Acciones del nuevo Input System
├── Packages/                         # Manifest de paquetes de Unity
├── ProjectSettings/                  # Configuración del proyecto
└── README.md
```

Las carpetas `Library/`, `Temp/`, `Logs/` y `UserSettings/` son generadas localmente por Unity y están excluidas mediante `.gitignore`.

## Paquetes principales

- **Universal Render Pipeline** (`com.unity.render-pipelines.universal`)
- **Input System** (`com.unity.inputsystem`)
- **AI Navigation** (`com.unity.ai.navigation`)
- **Timeline** (`com.unity.timeline`)
- **uGUI** (`com.unity.ugui`)
- **Visual Scripting** (`com.unity.visualscripting`)
- **Test Framework** (`com.unity.test-framework`)

## Convenciones de trabajo

- **Ramas:**
  - `main`: versión estable/entregable. No se trabaja directamente en ella.
  - `develop`: rama de integración donde se une el trabajo del equipo.
  - `feature/<nombre>`: una rama por tarea, creada desde `develop` y fusionada de vuelta a `develop` mediante Pull Request.
  - Cuando `develop` está estable, se fusiona a `main`.
- **Commits:** mensajes cortos y descriptivos (p. ej. `Agrega movimiento del jugador`).
- **Archivos `.meta`:** siempre se deben subir junto con su asset correspondiente.
- **Escenas:** evitar que dos personas editen la misma escena al mismo tiempo para prevenir conflictos de merge.

## Autores

- Alex Franco Imbacuan Culchac
- Pedro Luis Parrales Moscoso
- Olman Alexander Silva Zúñiga

## Licencia

Proyecto académico — uso educativo.
