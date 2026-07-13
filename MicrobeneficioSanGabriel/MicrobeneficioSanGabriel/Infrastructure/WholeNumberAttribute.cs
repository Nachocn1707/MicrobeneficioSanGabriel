using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace MicrobeneficioSanGabriel.Infrastructure;

/// <summary>
/// Valida que un valor numérico no contenga decimales.
/// Los modelos conservan decimal para compatibilidad con la base de datos,
/// pero los formularios de cantidades trabajan únicamente con enteros.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class WholeNumberAttribute : ValidationAttribute
{
    public WholeNumberAttribute()
        : base("Ingrese un número entero, sin comas ni decimales.")
    {
    }

    public override bool IsValid(object? value)
    {
        if (value is null)
        {
            return true;
        }

        try
        {
            var number = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
            return number == decimal.Truncate(number);
        }
        catch (Exception) when (value is IConvertible)
        {
            return false;
        }
    }
}
