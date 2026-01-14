using PostApiService.Repositories;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PostApiService.Models
{
    public class Comment : IEntity
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(500)]
        public string Content { get; set; } = default!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int PostId { get; set; }

        public string UserId { get; set; } = default!;

        public virtual IdentityUser User { get; set; } = default!;

        [JsonIgnore]
        public Post? Post { get; set; }

        public bool IsEditedByAdmin { get; set; } = false;
    }
}
