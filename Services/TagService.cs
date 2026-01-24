using Microsoft.EntityFrameworkCore;
using Mero_Dainiki.Common;
using Mero_Dainiki.Data;
using Mero_Dainiki.Entities;
using Mero_Dainiki.Models;

namespace Mero_Dainiki.Services
{
    /// <summary>
    /// Interface for tag operations
    /// </summary>
    public interface ITagService
    {
        Task<ServiceResult<List<Tag>>> GetAllTagsAsync();
        Task<ServiceResult<Tag>> CreateTagAsync(TagViewModel model);
        Task<ServiceResult<Tag>> UpdateTagAsync(TagViewModel model);
        Task<ServiceResult> DeleteTagAsync(int id);
    }

    /// <summary>
    /// Tag service implementation
    /// </summary>
    public class TagService : ITagService
    {
        private readonly AppDbContext _context;

        public TagService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ServiceResult<List<Tag>>> GetAllTagsAsync()
        {
            try
            {
                var tags = await _context.Tags.OrderBy(t => t.Name).ToListAsync();
                return ServiceResult<List<Tag>>.Ok(tags);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<Tag>>.Fail($"Error retrieving tags: {ex.Message}");
            }
        }

        public async Task<ServiceResult<Tag>> CreateTagAsync(TagViewModel model)
        {
            try
            {
                var existingTag = await _context.Tags.FirstOrDefaultAsync(t => t.Name == model.Name);
                if (existingTag != null)
                {
                    return ServiceResult<Tag>.Fail("A tag with this name already exists.");
                }

                var tag = new Tag
                {
                    Name = model.Name,
                    Color = model.Color,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Tags.Add(tag);
                await _context.SaveChangesAsync();
                return ServiceResult<Tag>.Ok(tag);
            }
            catch (Exception ex)
            {
                return ServiceResult<Tag>.Fail($"Error creating tag: {ex.Message}");
            }
        }

        public async Task<ServiceResult<Tag>> UpdateTagAsync(TagViewModel model)
        {
            try
            {
                var tag = await _context.Tags.FindAsync(model.Id);
                if (tag == null)
                {
                    return ServiceResult<Tag>.Fail("Tag not found.");
                }

                tag.Name = model.Name;
                tag.Color = model.Color;
                await _context.SaveChangesAsync();
                return ServiceResult<Tag>.Ok(tag);
            }
            catch (Exception ex)
            {
                return ServiceResult<Tag>.Fail($"Error updating tag: {ex.Message}");
            }
        }

        public async Task<ServiceResult> DeleteTagAsync(int id)
        {
            try
            {
                var tag = await _context.Tags.FindAsync(id);
                if (tag == null)
                {
                    return ServiceResult.Fail("Tag not found.");
                }

                _context.Tags.Remove(tag);
                await _context.SaveChangesAsync();
                return ServiceResult.Ok();
            }
            catch (Exception ex)
            {
                return ServiceResult.Fail($"Error deleting tag: {ex.Message}");
            }
        }
    }
}
