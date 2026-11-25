using UnityEngine;

/// <summary>
/// Pose de "mirar el celular" usando IK: sube las dos manos delante del pecho con las palmas
/// hacia arriba, inclina la cabeza hacia el teléfono y apoya el objeto del celular sobre las palmas.
/// Va en el mismo objeto que el Animator (Unity solo llama OnAnimatorIK ahí)
/// y necesita que la capa del Animator tenga activado "IK Pass".
/// La cinemática lo agrega en tiempo de ejecución y controla <see cref="Peso"/>.
/// </summary>
[RequireComponent(typeof(Animator))]
public class IKCelular : MonoBehaviour
{
    [Tooltip("0 = brazos normales, 1 = sosteniendo el celular frente al pecho.")]
    [Range(0f, 1f)] public float Peso;

    [Tooltip("Objeto del celular. Se mueve solo entre las manos; no hace falta que sea hijo del personaje.")]
    public Transform Celular;

    [Tooltip("0 = mira el celular o al frente, 1 = gira la cabeza hacia PuntoMirada.")]
    [Range(0f, 1f)] public float PesoMirada;

    [Tooltip("Punto hacia el que gira la cabeza cuando PesoMirada es mayor que Peso.")]
    public Vector3 PuntoMirada;

    [Header("Posición del celular respecto al pecho")]
    [SerializeField] private float distanciaAdelante = 0.3f;
    [SerializeField] private float alturaRespectoPecho = -0.12f;
    [SerializeField] private float separacionManos = 0.045f;

    [Header("Ajuste del celular en las palmas")]
    [Tooltip("0 = en la muñeca, 1 = en los nudillos. Las palmas quedan más o menos en el medio.")]
    [Range(0f, 1f)] [SerializeField] private float puntoDePalma = 0.55f;
    [Tooltip("Cuánto sobresale el celular de la palma hacia la cara (metros).")]
    [SerializeField] private float elevacionSobrePalma = 0.02f;
    [Tooltip("Gira las manos para que las palmas queden debajo del celular.")]
    [SerializeField] private bool rotarManos = true;
    [Tooltip("Cuánto se cierran las palmas una hacia la otra (0 = paralelas, 1 = enfrentadas).")]
    [Range(0f, 1f)] [SerializeField] private float cierrePalmas = 0.15f;
    [Tooltip("El celular va apoyado en la palma derecha (la izquierda solo acompaña). Si se desactiva, queda entre las dos manos.")]
    [SerializeField] private bool celularEnManoDerecha = true;
    [Tooltip("Grosor del celular: lo separa de la palma para que los dedos no lo atraviesen.")]
    [SerializeField] private float grosorCelular = 0.012f;
    [Tooltip("Separación extra entre la palma (o el guante) y el celular, en metros.")]
    [SerializeField] private float separacionPalma = 0.03f;
    [Tooltip("Cuánto sobresale el celular hacia las puntas de los dedos, en metros.")]
    [SerializeField] private float avanceHaciaDedos = 0.025f;

    [Header("Dedos")]
    [Tooltip("Cuánto se doblan los dedos para rodear el celular (0 = como en la animación, 1 = el máximo de gradosFalanges).")]
    [Range(0f, 1f)] [SerializeField] private float cierreDedos = 0.85f;
    [Tooltip("Grados que se dobla cada falange con cierreDedos = 1: base, media y punta. Suaves: abrazan los bordes del celular sin cerrar el puño.")]
    [SerializeField] private Vector3 gradosFalanges = new Vector3(28f, 32f, 18f);
    [Tooltip("Grados que se dobla el pulgar hacia la palma con cierreDedos = 1.")]
    [SerializeField] private float gradosPulgar = 15f;

    private Animator animator;
    private Transform pecho, cabeza, caderas;
    private Transform manoIzquierda, manoDerecha;
    private Transform nudilloIzquierdo, nudilloDerecho;
    private Transform pulgarIzquierdo, pulgarDerecho;
    // [dedo, falange]: índice, medio, anular y meñique; base, media y punta.
    private readonly Transform[,] dedosIzq = new Transform[4, 3];
    private readonly Transform[,] dedosDer = new Transform[4, 3];
    private Transform[] pulgarIzq, pulgarDer;

    // Calibración: en qué ejes locales del hueso de cada mano quedan los dedos y la palma.
    private bool calibrado;
    private Vector3 dedosLocIzq, dedosLocDer, palmaLocIzq, palmaLocDer;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        pecho = animator.GetBoneTransform(HumanBodyBones.Chest);
        if (pecho == null) pecho = animator.GetBoneTransform(HumanBodyBones.Spine);
        cabeza = animator.GetBoneTransform(HumanBodyBones.Head);
        caderas = animator.GetBoneTransform(HumanBodyBones.Hips);
        manoIzquierda = animator.GetBoneTransform(HumanBodyBones.LeftHand);
        manoDerecha = animator.GetBoneTransform(HumanBodyBones.RightHand);
        // Nudillos (base del dedo medio); si el modelo no los tiene, se usa la muñeca.
        nudilloIzquierdo = animator.GetBoneTransform(HumanBodyBones.LeftMiddleProximal);
        nudilloDerecho = animator.GetBoneTransform(HumanBodyBones.RightMiddleProximal);
        pulgarIzquierdo = animator.GetBoneTransform(HumanBodyBones.LeftThumbIntermediate);
        pulgarDerecho = animator.GetBoneTransform(HumanBodyBones.RightThumbIntermediate);
        if (pulgarIzquierdo == null) pulgarIzquierdo = animator.GetBoneTransform(HumanBodyBones.LeftThumbProximal);
        if (pulgarDerecho == null) pulgarDerecho = animator.GetBoneTransform(HumanBodyBones.RightThumbProximal);

        // Huesos de los dedos, en orden base → punta.
        var izq = new[]
        {
            HumanBodyBones.LeftIndexProximal, HumanBodyBones.LeftIndexIntermediate, HumanBodyBones.LeftIndexDistal,
            HumanBodyBones.LeftMiddleProximal, HumanBodyBones.LeftMiddleIntermediate, HumanBodyBones.LeftMiddleDistal,
            HumanBodyBones.LeftRingProximal, HumanBodyBones.LeftRingIntermediate, HumanBodyBones.LeftRingDistal,
            HumanBodyBones.LeftLittleProximal, HumanBodyBones.LeftLittleIntermediate, HumanBodyBones.LeftLittleDistal
        };
        var der = new[]
        {
            HumanBodyBones.RightIndexProximal, HumanBodyBones.RightIndexIntermediate, HumanBodyBones.RightIndexDistal,
            HumanBodyBones.RightMiddleProximal, HumanBodyBones.RightMiddleIntermediate, HumanBodyBones.RightMiddleDistal,
            HumanBodyBones.RightRingProximal, HumanBodyBones.RightRingIntermediate, HumanBodyBones.RightRingDistal,
            HumanBodyBones.RightLittleProximal, HumanBodyBones.RightLittleIntermediate, HumanBodyBones.RightLittleDistal
        };
        for (int i = 0; i < 12; i++)
        {
            dedosIzq[i / 3, i % 3] = animator.GetBoneTransform(izq[i]);
            dedosDer[i / 3, i % 3] = animator.GetBoneTransform(der[i]);
        }
        pulgarIzq = new[] { animator.GetBoneTransform(HumanBodyBones.LeftThumbProximal), animator.GetBoneTransform(HumanBodyBones.LeftThumbIntermediate), animator.GetBoneTransform(HumanBodyBones.LeftThumbDistal) };
        pulgarDer = new[] { animator.GetBoneTransform(HumanBodyBones.RightThumbProximal), animator.GetBoneTransform(HumanBodyBones.RightThumbIntermediate), animator.GetBoneTransform(HumanBodyBones.RightThumbDistal) };
    }

    /// <summary>Punto frente al pecho donde se sostiene el celular.</summary>
    private Vector3 PuntoCelular()
    {
        return pecho.position
               + transform.forward * distanciaAdelante
               + Vector3.up * alturaRespectoPecho;
    }

    private Vector3 Palma(Transform muneca, Transform nudillo)
    {
        return nudillo != null ? Vector3.Lerp(muneca.position, nudillo.position, puntoDePalma) : muneca.position;
    }

    private void OnAnimatorIK(int capa)
    {
        if (pecho == null) return;

        float peso = Mathf.Clamp01(Peso);
        Vector3 punto = PuntoCelular();
        Vector3 derecha = transform.right;

        animator.SetIKPositionWeight(AvatarIKGoal.RightHand, peso);
        animator.SetIKPosition(AvatarIKGoal.RightHand, punto + derecha * separacionManos);
        animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, peso * 0.85f);
        animator.SetIKPosition(AvatarIKGoal.LeftHand, punto - derecha * separacionManos);

        // La rotación de las manos se corrige después en LateUpdate, directo sobre los huesos.
        animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0f);
        animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f);

        // Cabeza mirando la pantalla o, si se pide, hacia otro punto (por ejemplo la moto).
        if (PesoMirada > peso)
        {
            animator.SetLookAtWeight(PesoMirada, 0.1f, 1f, 0f, 0.5f);
            animator.SetLookAtPosition(PuntoMirada);
        }
        else
        {
            animator.SetLookAtWeight(peso, 0.15f, 1f, 0f, 0.6f);
            animator.SetLookAtPosition(punto);
        }
    }

    private void LateUpdate()
    {
        if (manoIzquierda == null || manoDerecha == null) return;

        // Con los brazos sueltos se (re)calibra; al subir el celular se usa lo último calibrado.
        if (Peso <= 0.001f) Calibrar();

        bool visible = Peso > 0.05f;
        if (Celular != null && Celular.gameObject.activeSelf != visible) Celular.gameObject.SetActive(visible);
        if (!visible) return;

        if (rotarManos && calibrado && cabeza != null) RotarManos();
        if (Celular == null) return;

        if (celularEnManoDerecha && calibrado && nudilloDerecho != null)
        {
            // Apoyado sobre la palma derecha: pantalla hacia donde mira la palma (la cara)
            // y la parte de arriba del celular hacia donde apuntan los dedos.
            Vector3 palma = (manoDerecha.rotation * palmaLocDer).normalized;
            Vector3 haciaDedosDer = Vector3.ProjectOnPlane(nudilloDerecho.position - manoDerecha.position, palma).normalized;
            Vector3 centro = Palma(manoDerecha, nudilloDerecho)
                             + palma * (grosorCelular * 0.5f + separacionPalma)
                             + haciaDedosDer * avanceHaciaDedos;
            Celular.SetPositionAndRotation(centro, Quaternion.LookRotation(palma, haciaDedosDer));
            return;
        }

        // Apoyado sobre las dos palmas, con la pantalla (eje Z del celular) mirando a la cara
        // y la parte de arriba del celular apuntando hacia donde apuntan los dedos.
        Vector3 palmas = (Palma(manoIzquierda, nudilloIzquierdo) + Palma(manoDerecha, nudilloDerecho)) * 0.5f;
        Vector3 haciaCara = (cabeza != null ? cabeza.position - palmas : -transform.forward).normalized;

        Vector3 munecas = (manoIzquierda.position + manoDerecha.position) * 0.5f;
        Vector3 nudillos = nudilloIzquierdo != null && nudilloDerecho != null
            ? (nudilloIzquierdo.position + nudilloDerecho.position) * 0.5f
            : munecas + transform.forward * 0.1f;
        Vector3 haciaDedos = Vector3.ProjectOnPlane(nudillos - munecas, haciaCara);
        if (haciaDedos.sqrMagnitude < 0.0001f) haciaDedos = Vector3.ProjectOnPlane(transform.forward, haciaCara);

        Celular.SetPositionAndRotation(palmas + haciaCara * elevacionSobrePalma, Quaternion.LookRotation(haciaCara, haciaDedos.normalized));
    }

    /// <summary>
    /// Averigua qué eje local de cada hueso de la mano es "los dedos" (muñeca → nudillos)
    /// y cuál "la palma", usando la posición del pulgar (vale para cualquier pose).
    /// </summary>
    private void Calibrar()
    {
        if (nudilloIzquierdo == null || nudilloDerecho == null || pulgarIzquierdo == null || pulgarDerecho == null)
        {
            rotarManos = false;
            return;
        }

        CalibrarMano(manoDerecha, nudilloDerecho, pulgarDerecho, true, out dedosLocDer, out palmaLocDer);
        CalibrarMano(manoIzquierda, nudilloIzquierdo, pulgarIzquierdo, false, out dedosLocIzq, out palmaLocIzq);
        calibrado = true;
    }

    private static void CalibrarMano(Transform mano, Transform nudillo, Transform pulgar, bool esDerecha, out Vector3 dedosLoc, out Vector3 palmaLoc)
    {
        Quaternion inversa = Quaternion.Inverse(mano.rotation);
        Vector3 dedos = (nudillo.position - mano.position).normalized;
        Vector3 haciaPulgar = Vector3.ProjectOnPlane(pulgar.position - mano.position, dedos).normalized;
        // Mano derecha con la palma abajo y los dedos al frente: el pulgar queda a la izquierda,
        // así que dedos × pulgar apunta hacia donde mira la palma (en la izquierda es al revés).
        Vector3 palma = esDerecha ? Vector3.Cross(dedos, haciaPulgar) : Vector3.Cross(haciaPulgar, dedos);
        dedosLoc = inversa * dedos;
        palmaLoc = inversa * palma.normalized;
    }

    /// <summary>
    /// Gira los huesos de las manos (después de la animación y el IK) para que
    /// las palmas queden hacia arriba, debajo del celular, y los dedos hacia adelante.
    /// </summary>
    private void RotarManos()
    {
        float peso = Mathf.Clamp01(Peso);
        Vector3 centro = (manoIzquierda.position + manoDerecha.position) * 0.5f;
        Vector3 haciaCara = (cabeza.position - centro).normalized;
        Vector3 dedos = Vector3.ProjectOnPlane(transform.forward, haciaCara).normalized;
        Vector3 derecha = transform.right;

        GirarMano(manoDerecha, dedos, (haciaCara - derecha * cierrePalmas).normalized, dedosLocDer, palmaLocDer, peso);
        GirarMano(manoIzquierda, dedos, (haciaCara + derecha * cierrePalmas).normalized, dedosLocIzq, palmaLocIzq, peso * 0.85f);

        CerrarDedos(manoDerecha, nudilloDerecho, palmaLocDer, dedosDer, pulgarDer, peso * cierreDedos);
        CerrarDedos(manoIzquierda, nudilloIzquierdo, palmaLocIzq, dedosIzq, pulgarIzq, peso * 0.85f * cierreDedos);
    }

    /// <summary>
    /// Dobla cada falange hacia la palma (sobre el eje lateral de la mano) para que los dedos
    /// rodeen el celular. Se suma a la pose de la animación.
    /// </summary>
    private void CerrarDedos(Transform mano, Transform nudillo, Vector3 palmaLoc, Transform[,] dedos, Transform[] pulgar, float peso)
    {
        if (peso <= 0.001f) return;

        Vector3 palma = mano.rotation * palmaLoc;
        Vector3 haciaDedos = (nudillo.position - mano.position).normalized;
        // Girar sobre (dedos × palma) lleva las puntas de los dedos hacia la palma.
        Vector3 ejeLateral = Vector3.Cross(haciaDedos, palma).normalized;

        for (int d = 0; d < 4; d++)
            for (int f = 0; f < 3; f++)
            {
                Transform hueso = dedos[d, f];
                if (hueso == null) continue;
                // El meñique y el anular cierran un poco más, como en un agarre real.
                float extra = 1f + d * 0.08f;
                hueso.rotation = Quaternion.AngleAxis(gradosFalanges[f] * extra * peso, ejeLateral) * hueso.rotation;
            }

        // El pulgar se acerca a la palma y se apoya sobre el borde del celular.
        for (int f = 0; f < pulgar.Length; f++)
        {
            Transform hueso = pulgar[f];
            if (hueso == null) continue;
            Transform siguiente = f + 1 < pulgar.Length ? pulgar[f + 1] : null;
            Vector3 dir = siguiente != null ? (siguiente.position - hueso.position).normalized : haciaDedos;
            Vector3 eje = Vector3.Cross(dir, palma);
            if (eje.sqrMagnitude < 0.0001f) continue;
            hueso.rotation = Quaternion.AngleAxis(gradosPulgar * (f == 0 ? 0.6f : 1f) * peso, eje.normalized) * hueso.rotation;
        }
    }

    private static void GirarMano(Transform mano, Vector3 dedos, Vector3 palma, Vector3 dedosLoc, Vector3 palmaLoc, float peso)
    {
        Quaternion objetivo = Quaternion.LookRotation(dedos, palma) * Quaternion.Inverse(Quaternion.LookRotation(dedosLoc, palmaLoc));
        mano.rotation = Quaternion.Slerp(mano.rotation, objetivo, peso);
    }
}
