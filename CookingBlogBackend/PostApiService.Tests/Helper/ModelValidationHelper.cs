using System.ComponentModel.DataAnnotations;
using DataAnnotationsValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;

namespace PostApiService.Tests.Helper
{
    public class ModelValidationHelper
    {
        public static void ValidateModel(object model, ControllerBase controller)
        {
            var validationResults = new List<DataAnnotationsValidationResult>();
            var validationContext = new ValidationContext(model);
            var isValid = Validator.TryValidateObject(model, validationContext, validationResults, true);

            if (!isValid)
            {
                foreach (var validationResult in validationResults)
                {
                    controller.ModelState.AddModelError(string.Join(",", validationResult.MemberNames), validationResult.ErrorMessage);
                }
            }
        }
    }
}
