using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.ComponentModel.DataAnnotations
{
    public static class ValidationExceptionHelper
    {
        public static ValidationException CreateValidationException(ICollection<ValidationResult>[] validationResultCollections)
        {
            if (validationResultCollections.Length > 0)
            {
                ValidationResult first = validationResultCollections[0].First();
                return new ValidationException(first, null, validationResultCollections);
            }
            return null;
        }


    }
}
