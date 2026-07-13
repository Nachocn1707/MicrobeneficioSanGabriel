using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace MicrobeneficioSanGabriel.Infrastructure;

/// <summary>
/// Permite recibir valores decimales escritos con punto o coma sin depender
/// de la configuración regional del equipo donde se ejecuta la aplicación.
/// </summary>
public sealed class FlexibleDecimalModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var valueResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (valueResult == ValueProviderResult.None)
        {
            return Task.CompletedTask;
        }

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueResult);
        var rawValue = valueResult.FirstValue;
        var isNullable = Nullable.GetUnderlyingType(bindingContext.ModelType) is not null;

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            if (isNullable)
            {
                bindingContext.Result = ModelBindingResult.Success(null);
            }

            return Task.CompletedTask;
        }

        if (TryParseFlexible(rawValue, out var decimalValue))
        {
            bindingContext.Result = ModelBindingResult.Success(decimalValue);
            return Task.CompletedTask;
        }

        bindingContext.ModelState.TryAddModelError(
            bindingContext.ModelName,
            "Ingrese un número válido. Puede utilizar coma o punto como separador decimal.");

        return Task.CompletedTask;
    }

    private static bool TryParseFlexible(string rawValue, out decimal result)
    {
        var normalized = rawValue
            .Trim()
            .Replace("\u00A0", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal);

        var lastComma = normalized.LastIndexOf(',');
        var lastDot = normalized.LastIndexOf('.');

        if (lastComma >= 0 && lastDot >= 0)
        {
            // El último separador se considera decimal; el otro, de miles.
            if (lastComma > lastDot)
            {
                normalized = normalized.Replace(".", string.Empty, StringComparison.Ordinal)
                                       .Replace(',', '.');
            }
            else
            {
                normalized = normalized.Replace(",", string.Empty, StringComparison.Ordinal);
            }
        }
        else if (lastComma >= 0)
        {
            normalized = normalized.Replace(',', '.');
        }

        return decimal.TryParse(
            normalized,
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture,
            out result);
    }
}

public sealed class FlexibleDecimalModelBinderProvider : IModelBinderProvider
{
    private static readonly IModelBinder Binder = new FlexibleDecimalModelBinder();

    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var modelType = Nullable.GetUnderlyingType(context.Metadata.ModelType)
                        ?? context.Metadata.ModelType;

        return modelType == typeof(decimal) ? Binder : null;
    }
}
