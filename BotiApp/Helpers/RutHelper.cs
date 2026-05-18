namespace BotiApp.Helpers;

public static class RutHelper
{
    /// <summary>
    /// Calcula el dígito verificador de un RUT chileno usando el algoritmo Mod11.
    /// Devuelve "0"–"9" o "K".
    /// </summary>
    public static string CalcularDv(int rut)
    {
        int suma  = 0;
        int mult  = 2;
        int n     = rut;

        while (n > 0)
        {
            suma += (n % 10) * mult;
            n    /= 10;
            mult  = mult == 7 ? 2 : mult + 1;
        }

        int resto = 11 - (suma % 11);
        return resto switch
        {
            11 => "0",
            10 => "K",
            _  => resto.ToString()
        };
    }

    /// <summary>
    /// Formatea un RUT como "12.345.678-9".
    /// </summary>
    public static string Formatear(int rut)
        => $"{rut:N0}-{CalcularDv(rut)}";
}
