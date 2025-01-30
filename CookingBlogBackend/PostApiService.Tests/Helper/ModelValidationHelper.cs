﻿using Microsoft.AspNetCore.Mvc;
using PostApiService.Models;
using System.ComponentModel.DataAnnotations;
using DataAnnotationsValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;

namespace PostApiService.Tests.Helper
{
    public class ModelValidationHelper
    {
        public static void ValidateModel(object comment, ControllerBase controller)
        {
            var validationResults = new List<DataAnnotationsValidationResult>();
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

        public static IEnumerable<object[]> GetPostTestData()
        {
            yield return new object[] { new Post
            {
                Title = "Valid Title",
                Description = "Valid description lorem ipsum dolor",
                Content = "Valid content lorem ipsum dolor",
                Author = "Valid Author",
                ImageUrl = "http://validimageurl.com",
                Slug = "valid-slug"
            }, true };

            yield return new object[] { new Post
            {
                Title = "",
                Description = "Valid description lorem ipsum dolor",
                Content = "Valid content lorem ipsum dolor",
                Author = "Valid Author",
                ImageUrl = "http://validimageurl.com",
                Slug = "valid-slug"
            }, false };

            yield return new object[] { new Post
            {
                Title = "Valid Title",
                Description = "",
                Content = "Valid content lorem ipsum dolor",
                Author = "Valid Author",
                ImageUrl = "http://validimageurl.com",
                Slug = "valid-slug"
            }, false };

            yield return new object[] { new Post
            {
                Title = "Valid Title",
                Description = "Valid description lorem ipsum dolor",
                Content = "",
                Author = "Valid Author",
                ImageUrl = "InvalidUrl",
                Slug = "valid-slug"
            }, false };

            yield return new object[] { new Post
            {
                Title = "Valid Title",
                Description = "Valid description lorem ipsum dolor",
                Content = "Valid content lorem ipsum dolor",
                Author = "",
                ImageUrl = "InvalidUrl",
                Slug = "valid-slug"
            }, false };

            yield return new object[] { new Post
            {
                Title = "Valid Title",
                Description = "Valid description lorem ipsum dolor",
                Content = "Valid content lorem ipsum dolor",
                Author = "Valid Author",
                ImageUrl = "",
                Slug = "valid-slug"
            }, false };

            yield return new object[] { new Post
            {
                Title = "Valid Title",
                Description = "Valid description lorem ipsum dolor",
                Content = "Valid content lorem ipsum dolor",
                Author = "Valid Author",
                ImageUrl = "http://validimageurl.com",
                Slug = ""
            }, false };
        }

        public static IEnumerable<object[]> UpdatePostTestData()
        {
            yield return new object[] { new Post
            {
                Title = "Valid Title",
                Description = "Valid description lorem ipsum dolor",
                Content = "Valid content lorem ipsum dolor",
                Author = "Valid Author",
                ImageUrl = "http://validimageurl.com",
                Slug = "valid-slug"
            }, true };

            yield return new object[] { new Post
            {
                Title = "",
                Description = "Valid description lorem ipsum dolor",
                Content = "Valid content lorem ipsum dolor",
                Author = "Valid Author",
                ImageUrl = "http://validimageurl.com",
                Slug = "valid-slug"
            }, false };

            yield return new object[] { new Post
            {
                Title = "Valid Title",
                Description = "",
                Content = "Valid content lorem ipsum dolor",
                Author = "Valid Author",
                ImageUrl = "http://validimageurl.com",
                Slug = "valid-slug"
            }, false };

            yield return new object[] { new Post
            {
                Title = "Valid Title",
                Description = "Valid description lorem ipsum dolor",
                Content = "",
                Author = "Valid Author",
                ImageUrl = "InvalidUrl",
                Slug = "valid-slug"
            }, false };

            yield return new object[] { new Post
            {
                Title = "Valid Title",
                Description = "Valid description lorem ipsum dolor",
                Content = "Valid content lorem ipsum dolor",
                Author = "",
                ImageUrl = "InvalidUrl",
                Slug = "valid-slug"
            }, false };

            yield return new object[] { new Post
            {
                Title = "Valid Title",
                Description = "Valid description lorem ipsum dolor",
                Content = "Valid content lorem ipsum dolor",
                Author = "Valid Author",
                ImageUrl = "",
                Slug = "valid-slug"
            }, false };

            yield return new object[] { new Post
            {
                Title = "Valid Title",
                Description = "Valid description lorem ipsum dolor",
                Content = "Valid content lorem ipsum dolor",
                Author = "Valid Author",
                ImageUrl = "http://validimageurl.com",
                Slug = ""
            }, false };
        }
    }
}
