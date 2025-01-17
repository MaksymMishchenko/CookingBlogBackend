using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace PostApiService.Tests.Helper
{
    public class ModelValidationHelper
    {
        public static void ValidateModel(object comment, ControllerBase controller)
        {
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(comment);
            var isValid = Validator.TryValidateObject(comment, validationContext, validationResults, true);

            if (!isValid)
            {
                foreach (var validationResult in validationResults)
                {
                    controller.ModelState.AddModelError(string.Join(",", validationResult.MemberNames), validationResult.ErrorMessage);
                }
            }
        }

        public static IEnumerable<object[]> GetCommentTestData()
        {
            yield return new object[] { "Valid comment content that is sufficiently long", true };
            yield return new object[] { "Short", false };
            yield return new object[] { new string('a', 501), false };
        }

        public static IEnumerable<object[]> GetCommentTestDataWithAuthor()
        {
            yield return new object[] { "Valid Author", "Valid content that is sufficiently long", true };
            yield return new object[] { "", "Valid content that is sufficiently long", false };
            yield return new object[] { "Valid Author", "Short", false };
            yield return new object[] { "", new string('a', 501), false };
            yield return new object[] { "Valid Author", new string('a', 501), false };
        }
    }
}
