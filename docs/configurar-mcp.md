# Configurar MCP for Unity

Guía para que cada integrante del equipo conecte una IA (Claude, Cursor, VS Code, etc.) al editor de Unity usando **MCP for Unity** ([CoplayDev/unity-mcp](https://github.com/CoplayDev/unity-mcp)).

> **¿Es obligatorio?** No. El paquete ya viene instalado en el proyecto, pero si no conectas ninguna IA simplemente no hace nada. Puedes trabajar en Unity normalmente sin seguir esta guía.

## ¿Qué es y quién paga?

- **MCP for Unity** es un "puente": permite que una app de IA cree objetos, cambie componentes, lea la Console, etc., dentro del editor.
- **No es una IA.** La IA es la app que tú conectes (Claude Desktop, Claude Code, Cursor...).
- **Cada persona usa su propia cuenta** de esa app. Nadie gasta los tokens o créditos de otro. En el repositorio no se guarda ninguna cuenta ni clave.

## 1. Requisitos (una sola vez por computador)

| Programa | Versión | Cómo instalarlo en Windows |
|---|---|---|
| Unity | 6000.5.8f1 (la del proyecto) | Unity Hub |
| Git | cualquiera reciente | [git-scm.com](https://git-scm.com/) |
| Python | **3.10 o superior** | Instalador oficial: [python.org/downloads/windows](https://www.python.org/downloads/windows/) (marca *Add Python to PATH*) |
| uv | cualquiera reciente | En PowerShell: `winget install --id=astral-sh.uv -e` |
| Una app de IA compatible con MCP | — | Por ejemplo Claude Desktop, Claude Code, Cursor o VS Code con Copilot |

Comprueba en una terminal nueva:

```bash
python --version   # debe decir 3.10 o más
uv --version       # debe mostrar "uv 0.x..."
git --version
```

## 2. Recibir el paquete

```bash
git checkout develop
git pull
```

Abre el proyecto en Unity y **espera a que termine de importar y compilar sin cancelar nada**. Unity descarga el paquete `com.coplaydev.unity-mcp` automáticamente desde `Packages/manifest.json`.

## 3. Abrir MCP for Unity

1. En Unity: **Window → MCP for Unity**.
2. La primera vez aparece un asistente de configuración que revisa Python y uv. Si algo sale en rojo, instálalo (paso 1) y vuelve a revisar. Cuando todo esté en verde, presiona **Done**.
3. Aparece la lista de apps de IA detectadas en tu computador.

## 4. Conectar tu app de IA

En la ventana **MCP for Unity**, selecciona tu app y presiona **Configure** (o **Configure All Detected Clients** / **Configure Selected**).

- **Claude Code:** usa el botón **Register**. Si dice *"Claude Not Found"*, instala Claude Code o indica dónde está con **Choose Claude Location**. Después de cambiar la configuración, reinicia Claude Code.
- **Claude Desktop:** se configura automáticamente desde la ventana. Reinicia Claude Desktop después.
- **Cursor:** además de configurar, activa MCP en los ajustes de Cursor.
- **VS Code (Copilot):** se conecta automáticamente después de configurar.

El servidor usa por defecto `http://localhost:8080/mcp`.

## 5. Verificar que funciona

1. En la ventana **MCP for Unity** el estado debe decir **Connected**.
2. Abre una escena de pruebas y pídele a tu IA:
   > *Crea un cubo rojo, uno azul y uno amarillo en la escena actual.*
3. Si aparecen los cubos, ya está. Bórralos (o no guardes la escena).

## Buenas prácticas en el equipo

- **Revisa lo que hizo la IA antes de cada commit:** `git status` y `git diff`. Si modificó escenas o archivos que no esperabas, revísalo antes de subirlo.
- **Trabaja en una escena de pruebas** o en la escena de tu rama, para no pisar escenas de otros.
- **No subas configuraciones personales.** Si aparecen archivos de configuración de tu app de IA o rutas de tu computador en `git status`, no los agregues.
- **No instales otros paquetes de IA** (como *Unity AI Assistant* o *Inference Engine*) en el proyecto sin hablarlo antes: en Unity 6.5 pueden hacer que el editor se congele al abrir.

## Solución de problemas

| Problema | Qué hacer |
|---|---|
| **"uv Not Found"** en rojo | Instala uv (paso 1). Si ya está, usa **Choose UV Install Location** y elige `%LOCALAPPDATA%\Microsoft\WinGet\Links\uv.exe` |
| **"No Unity Instances Found"** | Reinicia tu app de IA. Si sigue: **Window → MCP for Unity → Restart Server** |
| Package Manager: **"Error when executing git command"** | Instala Git y reinicia Unity |
| **"fatal: not in a git directory"** | En una terminal: `git config --global --add safe.directory "RUTA/DEL/PROYECTO"` y reinicia Unity |
| El puerto 8080 está ocupado | Cámbialo en la ventana **MCP for Unity** (por ejemplo a 8090) y vuelve a configurar tu app |
| El editor se congela al abrir el proyecto | Revisa que `Packages/manifest.json` **no** tenga `com.unity.ai.assistant` ni `com.unity.ai.inference` |

Documentación oficial: [coplaydev.github.io/unity-mcp](https://coplaydev.github.io/unity-mcp/)

## Versión del paquete

El proyecto usa una versión **fija** de MCP for Unity (`#v10.3.0` en `Packages/manifest.json`) para que a todos les funcione igual. Para actualizarla, hagan una rama `chore/...`, cambien la versión, prueben y suban un Pull Request.
