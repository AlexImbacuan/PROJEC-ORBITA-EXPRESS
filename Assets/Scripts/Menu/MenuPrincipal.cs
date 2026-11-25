using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Menú principal: pide el nombre del repartidor, y los botones Jugar y Salir.
/// Va en el Canvas del menú.
/// </summary>
public class MenuPrincipal : MonoBehaviour
{
    [Header("Interfaz")]
    [SerializeField] private TMP_InputField campoNombre;
    [SerializeField] private Button botonJugar;
    [SerializeField] private Button botonSalir;

    [Header("Escena")]
    [Tooltip("Nombre exacto de la escena que se carga al presionar Jugar (debe estar en Build Profiles).")]
    [SerializeField] private string escenaJugar = "PruebaEntregas";

    private void Start()
    {
        if (campoNombre != null)
        {
            campoNombre.characterLimit = DatosJugador.LargoMaximoNombre;
            campoNombre.text = DatosJugador.Nombre; // recuerda el último nombre usado
            campoNombre.onValueChanged.AddListener(_ => ActualizarBotonJugar());
            campoNombre.onSubmit.AddListener(_ => Jugar()); // Enter = Jugar
        }

        if (botonJugar != null) botonJugar.onClick.AddListener(Jugar);
        if (botonSalir != null) botonSalir.onClick.AddListener(Salir);

        ActualizarBotonJugar();
    }

    /// <summary>Jugar solo se puede presionar si hay un nombre escrito.</summary>
    private void ActualizarBotonJugar()
    {
        if (botonJugar == null || campoNombre == null) return;
        botonJugar.interactable = DatosJugador.Limpiar(campoNombre.text).Length > 0;
    }

    public void Jugar()
    {
        string nombre = campoNombre != null ? DatosJugador.Limpiar(campoNombre.text) : "";
        if (nombre.Length == 0) return;

        DatosJugador.Nombre = nombre;
        SceneManager.LoadScene(escenaJugar);
    }

    public void Salir()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
