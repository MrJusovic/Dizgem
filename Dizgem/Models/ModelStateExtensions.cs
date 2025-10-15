using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Dizgem.Models
{
    public static class ModelStateExtensions
    {
        public static string ToHtmlErrorList(this ModelStateDictionary modelState)
        {
            if (modelState.IsValid)
            {
                return string.Empty;
            }

            var errorMessages = modelState.Values
                                          .SelectMany(v => v.Errors)
                                          .Select(e => e.ErrorMessage);

            return $"<ul>{string.Join("", errorMessages.Select(e => $"<li>{e}</li>"))}</ul>";
        }
    }
}
