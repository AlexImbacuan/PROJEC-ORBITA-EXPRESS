using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Cinemática "Salida de la pizzería":
/// el repartidor sale del local, recibe un mensaje del jefe (subtítulo tecleado abajo),
/// corre a la moto, se sube y despega volando entre los edificios nevados.
///
/// Todo se controla por código con una corrutina; los encuadres de cámara fijos y los
/// recorridos son objetos vacíos (marcadores) en la escena, así que se pueden ajustar
/// moviéndolos en la vista Scene sin tocar el código.
/// </summary>
public class CinematicaSalidaPizzeria : MonoBehaviour
{
    [Header("Actores")]
    [SerializeField] private Transform repartidor;
    [SerializeField] private ControladorMoto moto;
    [Tooltip("Si se deja vacío usa Camera.main.")]
    [SerializeField] private Camera camara;

    [Header("Marcadores del repartidor y la moto")]
    [Tooltip("En la puerta de la pizzería, con la flecha azul hacia la calle.")]
    [SerializeField] private Transform puntoSalida;
    [Tooltip("Donde se detiene a leer el mensaje. La flecha azul indica hacia dónde mira.")]
    [SerializeField] private Transform puntoCelular;
    [Tooltip("Junto a la moto, donde llega corriendo antes de subirse.")]
    [SerializeField] private Transform puntoMontaje;
    [Tooltip("Posición y rotación de la moto estacionada al empezar.")]
    [SerializeField] private Transform motoEstacionada;
    [Tooltip("Puntos por los que pasa la moto al despegar, en orden.")]
    [SerializeField] private Transform[] rutaVuelo;

    [Header("Marcadores de cámara")]
    [SerializeField] private Transform camaraPlano1Inicio;
    [SerializeField] private Transform camaraPlano1Fin;
    [Tooltip("Hacia dónde mira la cámara en el plano de apertura (el letrero).")]
    [SerializeField] private Transform miraPlano1;
    [Tooltip("Cámara fija en la calle que sigue al repartidor cuando corre a la moto.")]
    [SerializeField] private Transform camaraPlanoCarrera;
    [Tooltip("Cámara fija desde la que se ve a la moto alejarse por el cielo.")]
    [SerializeField] private Transform camaraPlanoFinal;

    [Header("Pizzería")]
    [Tooltip("Grupo de la puerta corrediza. Se desliza hacia su derecha y queda escondida dentro del muro.")]
    [SerializeField] private Transform puerta;
    [Tooltip("Cuántos metros se corre la puerta al abrirse.")]
    [SerializeField] private float desplazamientoPuerta = 1.3f;
    [Tooltip("Brillo y luz del interior que se ven al abrir la puerta.")]
    [SerializeField] private GameObject interiorIluminado;

    [Header("Utilería y efectos")]
    [SerializeField] private Transform celular;
    [Tooltip("Material aditivo para la estela de los propulsores.")]
    [SerializeField] private Material materialEstela;
    [Tooltip("Material de partícula para la nieve que levanta la moto al despegar.")]
    [SerializeField] private Material materialNieve;

    [Header("Interfaz")]
    [SerializeField] private SubtituloMensaje subtitulo;
    [SerializeField] private CanvasGroup fundido;
    [SerializeField] private CanvasGroup tarjetaFinal;
    [SerializeField] private RectTransform franjaSuperior;
    [SerializeField] private RectTransform franjaInferior;
    [SerializeField] private float altoFranjas = 100f;

    [Header("Mensaje del jefe")]
    [SerializeField] private string remitente = "EL JEFE · ÓRBITA EXPRESS";
    [TextArea(2, 4)]
    [SerializeField] private string mensaje = "¡Apúrate, que la pizza se enfría y no podemos regalar más pizzas!";
    [Tooltip("Segundos que se ve \"escribiendo...\" antes del mensaje.")]
    [SerializeField] private float tiempoEscribiendo = 1.3f;

    [Header("Audio")]
    [Tooltip("Sonido de apertura (\"esto empieza\"). Su golpe grave coincide con el final del fundido.")]
    [SerializeField] private AudioClip sonidoInicio;
    [Range(0f, 1f)] [SerializeField] private float volumenInicio = 0.85f;
    [Tooltip("Ambiente en bucle durante toda la cinemática (viento de invierno).")]
    [SerializeField] private AudioClip ambiente;
    [Range(0f, 1f)] [SerializeField] private float volumenAmbiente = 0.3f;
    [SerializeField] private AudioClip sonidoMensaje;
    [SerializeField] private AudioClip sonidoDespegue;
    [SerializeField] private AudioClip musicaDespegue;
    [Range(0f, 1f)] [SerializeField] private float volumenMusica = 0.6f;

    [Header("Ritmo")]
    [SerializeField] private float velocidadCaminar = 1.8f;
    [SerializeField] private float velocidadCorrer = 5f;
    [SerializeField] private float velocidadVueloMaxima = 26f;
    [SerializeField] private float aceleracionVuelo = 11f;

    [Header("Opciones")]
    [SerializeField] private bool reproducirAlIniciar = true;

    // Parámetros del Animator del repartidor (deben coincidir exactamente con el controller).
    private static readonly int ParamVelocidad = Animator.StringToHash("Velocidad");
    private static readonly int ParamEnSuelo = Animator.StringToHash("EnSuelo");
    private static readonly int ParamManejando = Animator.StringToHash("Manejando");

    private Animator animator;
    private CharacterController controller;
    private ControladorRepartidor controlAPie;
    private ConductorMoto conductor;
    private IKCelular ikCelular;
    private Rigidbody rbMoto;
    private AudioSource fuenteEfectos;
    private AudioSource fuenteMusica;
    private AudioSource fuenteAmbiente;

    private System.Action<float> planoActual;
    private float inicioPlano;
    private Vector3 velocidadCamara;

    private Vector3 posicionPuerta;
    private readonly List<TrailRenderer> estelas = new List<TrailRenderer>();
    private Light luzPropulsor;

    /// <summary>True cuando terminó la cinemática.</summary>
    public bool Terminada { get; private set; }

    private void Start()
    {
        if (reproducirAlIniciar) StartCoroutine(Secuencia());
    }

    // =====================================================================
    // Secuencia
    // =====================================================================

    private IEnumerator Secuencia()
    {
        Preparar();

        // --- Plano 1: la pizzería bajo la nieve; se abre la puerta y sale el repartidor.
        CambiarPlano(t =>
        {
            float k = Suavizar(t / 6.5f);
            Vector3 pos = Vector3.Lerp(camaraPlano1Inicio.position, camaraPlano1Fin.position, k);
            Vector3 mira = Vector3.Lerp(miraPlano1.position, repartidor.position + Vector3.up * 1.5f, Suavizar((t - 2.5f) / 3f));
            Colocar(pos, mira);
        }, 42f);
        Reproducir(sonidoInicio, volumenInicio);
        if (ambiente != null)
        {
            fuenteAmbiente.clip = ambiente;
            fuenteAmbiente.Play();
            StartCoroutine(CambiarVolumen(fuenteAmbiente, 0f, volumenAmbiente, 2.5f, false));
        }
        StartCoroutine(Fundir(fundido, 1f, 0f, 1.5f));
        StartCoroutine(Franjas(0f, altoFranjas, 1.2f));
        yield return new WaitForSeconds(1.6f);

        yield return AbrirPuerta(0.7f);
        yield return CaminarHacia(puntoCelular.position, velocidadCaminar);
        StartCoroutine(CerrarPuerta(0.7f));
        yield return GirarHacia(puntoCelular.forward, 0.35f);

        // --- Llega el mensaje: saca el celular.
        Reproducir(sonidoMensaje, 0.9f);
        StartCoroutine(AnimarPesoCelular(1f, 0.55f));

        // --- Plano 2: plano medio frontal.
        CambiarPlano(t =>
        {
            float k = Suavizar(t / 4f);
            Vector3 pos = Relativo(repartidor, Vector3.Lerp(new Vector3(0.45f, 1.35f, 2.6f), new Vector3(0.35f, 1.4f, 2.1f), k));
            Colocar(pos, Relativo(repartidor, new Vector3(0f, 1.35f, 0f)));
        }, 40f);
        yield return new WaitForSeconds(0.6f);

        Coroutine tecleo = StartCoroutine(subtitulo.Mostrar(remitente, mensaje, tiempoEscribiendo));
        yield return new WaitForSeconds(2.2f);

        // --- Plano 3: de espaldas; se nota que mira el celular (su brillo) sin ver las manos.
        CambiarPlano(t =>
        {
            // Cerca y a un lado: detrás de él está la fachada, la cámara no debe meterse en el muro.
            Vector3 pos = Relativo(repartidor, new Vector3(0.6f, 1.62f, -1.05f + t * 0.04f));
            Colocar(pos, Relativo(repartidor, new Vector3(0.12f, 1.3f, 0.45f)));
        }, 40f);
        yield return new WaitForSeconds(2.4f);

        // --- Plano 4: primer plano de la cara desde abajo, iluminada por la pantalla.
        //     En el mismo plano guarda el celular y mira hacia la moto.
        CambiarPlano(t =>
        {
            float k = Suavizar(t / 5f);
            // Bajo y cerca: ve la cara por debajo de la visera de la gorra.
            Vector3 pos = Relativo(repartidor, Vector3.Lerp(new Vector3(0.18f, 1.0f, 0.7f), new Vector3(0.15f, 1.04f, 0.62f), k));
            Colocar(pos, Relativo(repartidor, new Vector3(0f, 1.47f, 0.05f)));
        }, 36f);
        yield return tecleo;
        yield return new WaitForSeconds(0.6f);
        StartCoroutine(AnimarPesoCelular(0f, 0.45f));
        StartCoroutine(MirarHacia(moto.transform.position + Vector3.up * 1.2f, 0.6f));
        yield return new WaitForSeconds(1.3f);
        StartCoroutine(subtitulo.Ocultar(0.6f));

        // --- Plano 5: corre hacia la moto (cámara fija en la calle que lo sigue).
        Vector3 miraSuave = repartidor.position + Vector3.up * 1.1f;
        CambiarPlano(t =>
        {
            miraSuave = Vector3.Lerp(miraSuave, repartidor.position + Vector3.up * 1.1f, 1f - Mathf.Exp(-8f * Time.deltaTime));
            Colocar(camaraPlanoCarrera.position, miraSuave);
        }, 48f);
        yield return GirarHacia(puntoMontaje.position - repartidor.position, 0.25f);
        // Corta apenas llega al costado de la moto (sin frenar ni quedarse trotando).
        yield return CaminarHacia(puntoMontaje.position, velocidadCorrer, 0.35f, false);

        // --- Corte: ya está montado (el corte oculta el salto al asiento).
        Montar();
        CambiarPlano(t =>
        {
            float k = Suavizar(t / 3f);
            Vector3 pos = Relativo(moto.transform, Vector3.Lerp(new Vector3(-2.3f, 0.45f, 3.4f), new Vector3(-1.5f, 0.55f, 3.0f), k));
            Colocar(pos, moto.Asiento.position + Vector3.up * 0.55f);
        }, 46f);
        yield return new WaitForSeconds(0.7f);
        yield return Elevar(1.5f);

        // --- Plano 6: persecución mientras acelera por la calle y empieza a subir.
        Reproducir(sonidoDespegue, 1f);
        if (musicaDespegue != null) { fuenteMusica.clip = musicaDespegue; fuenteMusica.volume = volumenMusica; fuenteMusica.Play(); }
        bool primerCuadro = true;
        CambiarPlano(t =>
        {
            Transform m = moto.transform;
            Vector3 deseada = m.position - m.forward * 6.5f + Vector3.up * 1.7f + m.right * 2.2f;
            if (primerCuadro) { camara.transform.position = deseada; velocidadCamara = Vector3.zero; primerCuadro = false; }
            Vector3 pos = Vector3.SmoothDamp(camara.transform.position, deseada, ref velocidadCamara, 0.25f);
            Colocar(pos, m.position + m.forward * 3f + Vector3.up * 0.5f);
        }, 55f);
        StartCoroutine(Volar());
        yield return new WaitForSeconds(3.4f);

        // --- Plano 7: desde la calle, la moto se pierde entre la nieve y los edificios.
        Quaternion rotSuave = Quaternion.LookRotation(moto.transform.position - camaraPlanoFinal.position);
        CambiarPlano(t =>
        {
            Quaternion objetivo = Quaternion.LookRotation(moto.transform.position - camaraPlanoFinal.position);
            rotSuave = Quaternion.Slerp(rotSuave, objetivo, 1f - Mathf.Exp(-5f * Time.deltaTime));
            camara.transform.SetPositionAndRotation(camaraPlanoFinal.position, rotSuave);
        }, 50f);
        yield return new WaitForSeconds(4.2f);

        // --- Cierre: fundido a negro y tarjeta con la marca.
        StartCoroutine(CambiarVolumen(fuenteMusica, fuenteMusica.volume, 0f, 3.5f, true));
        StartCoroutine(CambiarVolumen(fuenteAmbiente, fuenteAmbiente.volume, 0f, 4f, true));
        yield return Fundir(fundido, 0f, 1f, 1.4f);
        yield return Fundir(tarjetaFinal, 0f, 1f, 0.8f);
        yield return new WaitForSeconds(3f);

        Terminada = true;
    }

    // =====================================================================
    // Preparación
    // =====================================================================

    private void Preparar()
    {
        if (camara == null) camara = Camera.main;

        animator = repartidor.GetComponent<Animator>();
        controller = repartidor.GetComponent<CharacterController>();
        controlAPie = repartidor.GetComponent<ControladorRepartidor>();
        conductor = repartidor.GetComponent<ConductorMoto>();

        // El jugador no controla nada durante la cinemática.
        if (controlAPie != null) controlAPie.enabled = false;
        if (conductor != null) { conductor.EntradaBloqueada = true; conductor.enabled = false; }
        animator.applyRootMotion = false;

        controller.enabled = false;
        repartidor.SetPositionAndRotation(puntoSalida.position, puntoSalida.rotation);
        controller.enabled = true;
        animator.SetBool(ParamManejando, false);
        animator.SetBool(ParamEnSuelo, true);
        animator.SetFloat(ParamVelocidad, 0f);

        ikCelular = repartidor.GetComponent<IKCelular>();
        if (ikCelular == null) ikCelular = repartidor.gameObject.AddComponent<IKCelular>();
        ikCelular.Celular = celular;
        ikCelular.Peso = 0f;
        if (celular != null) celular.gameObject.SetActive(false);

        // Moto estacionada flotando junto a la acera.
        rbMoto = moto.GetComponent<Rigidbody>();
        rbMoto.isKinematic = false;
        rbMoto.linearVelocity = Vector3.zero;
        rbMoto.angularVelocity = Vector3.zero;
        moto.transform.SetPositionAndRotation(motoEstacionada.position, motoEstacionada.rotation);
        rbMoto.position = motoEstacionada.position;
        rbMoto.rotation = motoEstacionada.rotation;
        CrearPropulsores();

        if (puerta != null) posicionPuerta = puerta.localPosition;
        if (interiorIluminado != null) interiorIluminado.SetActive(false);

        fundido.alpha = 1f;
        tarjetaFinal.alpha = 0f;
        AjustarFranjas(0f);

        fuenteEfectos = gameObject.AddComponent<AudioSource>();
        fuenteEfectos.playOnAwake = false;
        fuenteMusica = gameObject.AddComponent<AudioSource>();
        fuenteMusica.playOnAwake = false;
        fuenteAmbiente = gameObject.AddComponent<AudioSource>();
        fuenteAmbiente.playOnAwake = false;
        fuenteAmbiente.loop = true;
    }

    private void CrearPropulsores()
    {
        BoxCollider caja = moto.GetComponent<BoxCollider>();
        Vector3 atras = caja.center - Vector3.forward * (caja.size.z * 0.5f);

        var colores = new[] { new Color(0.2f, 0.95f, 1f), new Color(1f, 0.55f, 0.12f) };
        for (int i = 0; i < 2; i++)
        {
            var go = new GameObject("Estela_" + (i + 1));
            go.transform.SetParent(moto.transform, false);
            go.transform.localPosition = atras + Vector3.right * (i == 0 ? -0.18f : 0.18f);

            var estela = go.AddComponent<TrailRenderer>();
            estela.sharedMaterial = materialEstela;
            estela.time = 0.9f;
            estela.minVertexDistance = 0.1f;
            estela.widthCurve = new AnimationCurve(new Keyframe(0f, 0.22f), new Keyframe(1f, 0f));
            var gradiente = new Gradient();
            gradiente.SetKeys(
                new[] { new GradientColorKey(colores[i] * 1.4f, 0f), new GradientColorKey(colores[1 - i], 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            estela.colorGradient = gradiente;
            estela.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            estela.emitting = false;
            estelas.Add(estela);
        }

        var luz = new GameObject("Luz_Propulsor");
        luz.transform.SetParent(moto.transform, false);
        luz.transform.localPosition = atras - Vector3.forward * 0.3f;
        luzPropulsor = luz.AddComponent<Light>();
        luzPropulsor.type = LightType.Point;
        luzPropulsor.color = new Color(1f, 0.55f, 0.2f);
        luzPropulsor.range = 5f;
        luzPropulsor.intensity = 0f;
    }

    // =====================================================================
    // Acciones
    // =====================================================================

    /// <summary>Puerta corrediza: se desliza de lado y se mete dentro del muro.</summary>
    private IEnumerator AbrirPuerta(float duracion)
    {
        if (interiorIluminado != null) interiorIluminado.SetActive(true);
        if (puerta == null) yield break;
        Vector3 abierta = posicionPuerta + Vector3.right * desplazamientoPuerta;
        for (float t = 0f; t < duracion; t += Time.deltaTime)
        {
            puerta.localPosition = Vector3.Lerp(posicionPuerta, abierta, Suavizar(t / duracion));
            yield return null;
        }
        puerta.localPosition = abierta;
    }

    private IEnumerator CerrarPuerta(float duracion)
    {
        yield return new WaitForSeconds(0.5f);
        if (puerta != null)
        {
            Vector3 abierta = posicionPuerta + Vector3.right * desplazamientoPuerta;
            for (float t = 0f; t < duracion; t += Time.deltaTime)
            {
                puerta.localPosition = Vector3.Lerp(abierta, posicionPuerta, Suavizar(t / duracion));
                yield return null;
            }
            puerta.localPosition = posicionPuerta;
        }
        if (interiorIluminado != null) interiorIluminado.SetActive(false);
    }

    /// <summary>
    /// Camina (o corre) en línea recta hasta el destino usando el CharacterController.
    /// Si algo lo frena (por ejemplo el colisionador de la moto) deja de avanzar en vez de
    /// quedarse corriendo en el sitio.
    /// </summary>
    private IEnumerator CaminarHacia(Vector3 destino, float velocidad, float tolerancia = 0.08f, bool frenar = true)
    {
        // Velocidad normalizada que espera el Blend Tree: 0.5 = caminar, 1 = correr.
        float paramAnimacion = velocidad <= 2f ? 0.5f * velocidad / 2f : 0.5f + 0.5f * Mathf.InverseLerp(2f, 5.5f, velocidad);

        Vector3 ultimaPosicion = repartidor.position;
        float tiempoSinAvanzar = 0f;

        while (true)
        {
            Vector3 delta = destino - repartidor.position;
            delta.y = 0f;
            float distancia = delta.magnitude;
            if (distancia < tolerancia || distancia < velocidad * Time.deltaTime) break;

            // ¿Está trabado? Si casi no avanza durante 0.2 s, se da por llegado.
            Vector3 avance = repartidor.position - ultimaPosicion;
            avance.y = 0f;
            tiempoSinAvanzar = avance.magnitude < velocidad * Time.deltaTime * 0.3f ? tiempoSinAvanzar + Time.deltaTime : 0f;
            ultimaPosicion = repartidor.position;
            if (tiempoSinAvanzar > 0.2f)
            {
                Debug.LogWarning($"Cinemática: el repartidor se trabó a {distancia:0.00} m de su destino. Revisa el marcador o los colisionadores cercanos.", this);
                break;
            }

            Vector3 direccion = delta / distancia;
            repartidor.rotation = Quaternion.RotateTowards(repartidor.rotation, Quaternion.LookRotation(direccion), 600f * Time.deltaTime);
            controller.Move((direccion * velocidad + Vector3.down * 2f) * Time.deltaTime);
            animator.SetFloat(ParamVelocidad, paramAnimacion, 0.1f, Time.deltaTime);
            yield return null;
        }

        if (!frenar) yield break;
        for (float t = 0f; t < 0.25f; t += Time.deltaTime)
        {
            animator.SetFloat(ParamVelocidad, 0f, 0.1f, Time.deltaTime);
            yield return null;
        }
    }

    private IEnumerator GirarHacia(Vector3 direccion, float duracion)
    {
        direccion.y = 0f;
        if (direccion.sqrMagnitude < 0.0001f) yield break;
        Quaternion inicio = repartidor.rotation;
        Quaternion fin = Quaternion.LookRotation(direccion);
        for (float t = 0f; t < duracion; t += Time.deltaTime)
        {
            repartidor.rotation = Quaternion.Slerp(inicio, fin, Suavizar(t / duracion));
            animator.SetFloat(ParamVelocidad, 0f, 0.1f, Time.deltaTime);
            yield return null;
        }
        repartidor.rotation = fin;
    }

    private IEnumerator AnimarPesoCelular(float destino, float duracion)
    {
        float inicio = ikCelular.Peso;
        for (float t = 0f; t < duracion; t += Time.deltaTime)
        {
            ikCelular.Peso = Mathf.Lerp(inicio, destino, Suavizar(t / duracion));
            yield return null;
        }
        ikCelular.Peso = destino;
    }

    /// <summary>Gira la cabeza del repartidor hacia un punto (por IK).</summary>
    private IEnumerator MirarHacia(Vector3 punto, float duracion)
    {
        ikCelular.PuntoMirada = punto;
        for (float t = 0f; t < duracion; t += Time.deltaTime)
        {
            if (ikCelular == null) yield break;
            ikCelular.PesoMirada = Suavizar(t / duracion);
            yield return null;
        }
        if (ikCelular != null) ikCelular.PesoMirada = 1f;
    }

    private void Montar()
    {
        // La moto deja de usar su física: a partir de aquí la mueve la cinemática.
        moto.enabled = false;
        rbMoto.isKinematic = true;
        rbMoto.interpolation = RigidbodyInterpolation.None;

        if (ikCelular != null) Destroy(ikCelular);
        if (celular != null) celular.gameObject.SetActive(false);

        conductor.enabled = true; // sus IK llevan manos y pies a la moto
        conductor.EntradaBloqueada = true;
        if (!conductor.SubirA(moto))
            Debug.LogWarning("La cinemática no pudo subir al repartidor a la moto (revisa el Asiento).", this);
    }

    /// <summary>La moto se eleva, levanta nieve y enciende los propulsores.</summary>
    private IEnumerator Elevar(float duracion)
    {
        Transform m = moto.transform;
        Vector3 inicio = m.position;
        Quaternion rumbo = Quaternion.LookRotation(Vector3.ProjectOnPlane(m.forward, Vector3.up));

        LevantarNieve(inicio);
        foreach (var e in estelas) e.emitting = true;

        for (float t = 0f; t < duracion; t += Time.deltaTime)
        {
            float k = t / duracion;
            float altura = 0.8f * Suavizar(k);
            float cabeceo = -10f * Mathf.Sin(k * Mathf.PI) - 3f * k; // nariz arriba
            float temblor = Mathf.Sin(t * 47f) * 0.015f * (1f - k);
            m.SetPositionAndRotation(inicio + Vector3.up * (altura + temblor), rumbo * Quaternion.Euler(cabeceo, 0f, 0f));
            luzPropulsor.intensity = Mathf.Lerp(0f, 4f, k);
            yield return null;
        }
    }

    /// <summary>Recorre la ruta de vuelo acelerando, mirando siempre hacia donde avanza.</summary>
    private IEnumerator Volar()
    {
        var puntos = new List<Vector3> { moto.transform.position };
        foreach (Transform p in rutaVuelo) if (p != null) puntos.Add(p.position);

        // Muestrea una curva suave (Catmull-Rom) y guarda la distancia acumulada.
        var muestras = new List<Vector3>();
        for (int i = 0; i < puntos.Count - 1; i++)
        {
            Vector3 p0 = puntos[Mathf.Max(i - 1, 0)], p1 = puntos[i], p2 = puntos[i + 1], p3 = puntos[Mathf.Min(i + 2, puntos.Count - 1)];
            for (int s = 0; s < 40; s++) muestras.Add(CatmullRom(p0, p1, p2, p3, s / 40f));
        }
        muestras.Add(puntos[puntos.Count - 1]);

        var distancias = new float[muestras.Count];
        for (int i = 1; i < muestras.Count; i++) distancias[i] = distancias[i - 1] + Vector3.Distance(muestras[i - 1], muestras[i]);
        float total = distancias[distancias.Length - 1];

        Transform m = moto.transform;
        float recorrido = 0f, velocidad = 2f, alabeo = 0f;
        Vector3 direccionAnterior = m.forward;

        while (true)
        {
            velocidad = Mathf.Min(velocidadVueloMaxima, velocidad + aceleracionVuelo * Time.deltaTime);
            recorrido += velocidad * Time.deltaTime;

            Vector3 pos, dir;
            if (recorrido < total)
            {
                pos = Muestra(muestras, distancias, recorrido);
                dir = (Muestra(muestras, distancias, Mathf.Min(recorrido + 1.5f, total)) - pos).normalized;
                if (dir.sqrMagnitude < 0.001f) dir = direccionAnterior;
            }
            else
            {
                // Pasado el último punto sigue recto.
                dir = direccionAnterior;
                pos = m.position + dir * velocidad * Time.deltaTime;
            }

            // Se inclina hacia el lado al que gira, como una moto de verdad.
            float giro = Vector3.SignedAngle(Vector3.ProjectOnPlane(direccionAnterior, Vector3.up), Vector3.ProjectOnPlane(dir, Vector3.up), Vector3.up);
            alabeo = Mathf.Lerp(alabeo, Mathf.Clamp(-giro * 25f, -30f, 30f), 1f - Mathf.Exp(-4f * Time.deltaTime));

            m.SetPositionAndRotation(pos, Quaternion.LookRotation(dir) * Quaternion.Euler(0f, 0f, alabeo));
            direccionAnterior = dir;
            yield return null;
        }
    }

    private void LevantarNieve(Vector3 posicion)
    {
        var go = new GameObject("Nieve_Despegue");
        go.transform.position = posicion + Vector3.down * 0.3f;
        go.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var principal = ps.main;
        principal.loop = false;
        principal.duration = 1f;
        principal.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.4f);
        principal.startSpeed = new ParticleSystem.MinMaxCurve(2f, 6f);
        principal.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.35f);
        principal.gravityModifier = 0.5f;
        principal.simulationSpace = ParticleSystemSimulationSpace.World;
        var emision = ps.emission;
        emision.rateOverTime = 0f;
        emision.SetBursts(new[] { new ParticleSystem.Burst(0f, 220), new ParticleSystem.Burst(0.25f, 120) });
        var forma = ps.shape;
        forma.shapeType = ParticleSystemShapeType.Cone;
        forma.angle = 70f;
        forma.radius = 1.1f;
        var tam = ps.sizeOverLifetime;
        tam.enabled = true;
        tam.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 2.5f));
        var color = ps.colorOverLifetime;
        color.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) });
        color.color = g;
        go.GetComponent<ParticleSystemRenderer>().sharedMaterial = materialNieve;
        ps.Play();
        Destroy(go, 4f);
    }

    // =====================================================================
    // Cámara
    // =====================================================================

    private void CambiarPlano(System.Action<float> plano, float campoVision)
    {
        planoActual = plano;
        inicioPlano = Time.time;
        camara.fieldOfView = campoVision;
        plano(0f); // corte inmediato, sin un cuadro con el encuadre anterior
    }

    private void LateUpdate()
    {
        planoActual?.Invoke(Time.time - inicioPlano);
    }

    private void Colocar(Vector3 posicion, Vector3 mirarA)
    {
        Vector3 dir = mirarA - posicion;
        if (dir.sqrMagnitude < 0.0001f) return;
        camara.transform.SetPositionAndRotation(posicion, Quaternion.LookRotation(dir));
    }

    /// <summary>Punto relativo a un objeto: x = derecha, y = altura sobre sus pies, z = adelante.</summary>
    private static Vector3 Relativo(Transform t, Vector3 local)
    {
        Vector3 adelante = Vector3.ProjectOnPlane(t.forward, Vector3.up).normalized;
        Vector3 derecha = Vector3.Cross(Vector3.up, adelante);
        return t.position + derecha * local.x + Vector3.up * local.y + adelante * local.z;
    }

    // =====================================================================
    // Interfaz y audio
    // =====================================================================

    private static IEnumerator Fundir(CanvasGroup grupo, float desde, float hasta, float duracion)
    {
        for (float t = 0f; t < duracion; t += Time.deltaTime)
        {
            grupo.alpha = Mathf.Lerp(desde, hasta, t / duracion);
            yield return null;
        }
        grupo.alpha = hasta;
    }

    private IEnumerator Franjas(float desde, float hasta, float duracion)
    {
        for (float t = 0f; t < duracion; t += Time.deltaTime)
        {
            AjustarFranjas(Mathf.Lerp(desde, hasta, Suavizar(t / duracion)));
            yield return null;
        }
        AjustarFranjas(hasta);
    }

    private void AjustarFranjas(float alto)
    {
        if (franjaSuperior != null) franjaSuperior.sizeDelta = new Vector2(franjaSuperior.sizeDelta.x, alto);
        if (franjaInferior != null) franjaInferior.sizeDelta = new Vector2(franjaInferior.sizeDelta.x, alto);
    }

    private void Reproducir(AudioClip clip, float volumen)
    {
        if (clip != null) fuenteEfectos.PlayOneShot(clip, volumen);
    }

    private static IEnumerator CambiarVolumen(AudioSource fuente, float desde, float hasta, float duracion, bool detenerAlFinal)
    {
        for (float t = 0f; t < duracion; t += Time.deltaTime)
        {
            fuente.volume = Mathf.Lerp(desde, hasta, t / duracion);
            yield return null;
        }
        fuente.volume = hasta;
        if (detenerAlFinal) fuente.Stop();
    }

    // =====================================================================
    // Utilidades
    // =====================================================================

    private static float Suavizar(float x)
    {
        x = Mathf.Clamp01(x);
        return x * x * (3f - 2f * x);
    }

    private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t, t3 = t2 * t;
        return 0.5f * (2f * p1 + (p2 - p0) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (3f * p1 - p0 - 3f * p2 + p3) * t3);
    }

    private static Vector3 Muestra(List<Vector3> muestras, float[] distancias, float d)
    {
        int i = System.Array.BinarySearch(distancias, d);
        if (i >= 0) return muestras[i];
        i = ~i;
        if (i <= 0) return muestras[0];
        if (i >= muestras.Count) return muestras[muestras.Count - 1];
        float k = Mathf.InverseLerp(distancias[i - 1], distancias[i], d);
        return Vector3.Lerp(muestras[i - 1], muestras[i], k);
    }

    private void OnDrawGizmos()
    {
        if (rutaVuelo == null || rutaVuelo.Length == 0) return;
        Gizmos.color = new Color(0.2f, 0.95f, 1f);
        Vector3 anterior = motoEstacionada != null ? motoEstacionada.position : rutaVuelo[0].position;
        foreach (Transform p in rutaVuelo)
        {
            if (p == null) continue;
            Gizmos.DrawLine(anterior, p.position);
            Gizmos.DrawWireSphere(p.position, 0.6f);
            anterior = p.position;
        }
    }
}
