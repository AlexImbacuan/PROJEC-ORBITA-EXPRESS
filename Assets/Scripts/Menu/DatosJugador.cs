using UnityEngine;

/// <summary>
/// Datos del jugador que se conservan entre escenas y entre partidas.
/// Se guardan en PlayerPrefs (solo en este computador). No guardar contraseñas aquí:
/// PlayerPrefs no está cifrado.
/// </summary>
public static class DatosJugador
{
    private const string ClaveNombre = "NombreRepartidor";
    public const int LargoMaximoNombre = 16;

    /// <summary>Nombre del repartidor. Vacío si nunca se ha escrito uno.</summary>
    public static string Nombre
    {
        get => PlayerPrefs.GetString(ClaveNombre, "");
        set
        {
            PlayerPrefs.SetString(ClaveNombre, Limpiar(value));
            PlayerPrefs.Save();
        }
    }

    /// <summary>Quita espacios sobrantes y recorta al largo máximo.</summary>
    public static string Limpiar(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre)) return "";
        nombre = nombre.Trim();
        return nombre.Length > LargoMaximoNombre ? nombre.Substring(0, LargoMaximoNombre) : nombre;
    }
}
