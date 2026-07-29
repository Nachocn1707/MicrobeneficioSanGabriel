using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace MicrobeneficioSanGabriel.Infrastructure
{
    /// <summary>
    /// Exige una dirección completa, con dominio y extensión válidos.
    /// Evita aceptar valores incompletos como "usuario@g".
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter,
        AllowMultiple = false)]
    public sealed class CompleteEmailAddressAttribute : RegularExpressionAttribute, IClientModelValidator
    {
        private const string PatronCorreoCompleto =
            @"^[A-Za-z0-9.!#$%&'*+/=?^_`{|}~-]+@[A-Za-z0-9](?:[A-Za-z0-9-]{0,61}[A-Za-z0-9])?(?:\.[A-Za-z]{2,})+$";

        public CompleteEmailAddressAttribute()
            : base(PatronCorreoCompleto)
        {
            ErrorMessage =
                "Ingrese un correo electrónico completo y válido, por ejemplo usuario@gmail.com o usuario@hotmail.com.";
        }

        public void AddValidation(ClientModelValidationContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            MergeAttribute(context.Attributes, "data-val", "true");
            MergeAttribute(context.Attributes, "data-val-completeemail", ErrorMessageString);
        }

        private static bool MergeAttribute(IDictionary<string, string> attributes, string key, string value)
        {
            if (attributes.ContainsKey(key))
            {
                return false;
            }

            attributes.Add(key, value);
            return true;
        }
    }
}
