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
    /// Tag service implementation with per-user isolation
    /// </summary>
    public class TagService : BaseService, ITagService
    {
        public TagService(AppDbContext context) : base(context) { }

        public async Task<ServiceResult<List<Tag>>> GetAllTagsAsync()
        {
            try
            {
                if (!IsUserAuthenticated) return ServiceResult<List<Tag>>.Fail("Unauthorized.");

                var tags = await _context.Tags
                    .Where(t => t.UserId == CurrentUserId)
                    .OrderBy(t => t.Name)
                    .ToListAsync();
                
                return ServiceResult<List<Tag>>.Ok(tags);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<Tag>>.Fail($"Error: {ex.Message}");
            }
        }

        public async Task<ServiceResult<Tag>> CreateTagAsync(TagViewModel model)
        {
            try
            {
                if (!IsUserAuthenticated) return ServiceResult<Tag>.Fail("Unauthorized.");

                if (await _context.Tags.AnyAsync(t => t.UserId == CurrentUserId && t.Name == model.Name))
                {
                    return ServiceResult<Tag>.Fail("Tag already exists.");
                }

                var tag = new Tag
                {
                    UserId = CurrentUserId,
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
                return ServiceResult<Tag>.Fail($"Error: {ex.Message}");
            }
        }

        public async Task<ServiceResult<Tag>> UpdateTagAsync(TagViewModel model)
        {
            try
            {
                if (!IsUserAuthenticated) return ServiceResult<Tag>.Fail("Unauthorized.");

                var tag = await _context.Tags.FirstOrDefaultAsync(t => t.Id == model.Id && t.UserId == CurrentUserId);
                if (tag == null) return ServiceResult<Tag>.Fail("Tag not found.");

                tag.Name = model.Name;
                tag.Color = model.Color;
                await _context.SaveChangesAsync();
                return ServiceResult<Tag>.Ok(tag);
            }
            catch (Exception ex)
            {
                return ServiceResult<Tag>.Fail($"Error: {ex.Message}");
            }
        }

        public async Task<ServiceResult> DeleteTagAsync(int id)
        {
            try
            {
                if (!IsUserAuthenticated) return ServiceResult.Fail("Unauthorized.");

                var tag = await _context.Tags.FirstOrDefaultAsync(t => t.Id == id && t.UserId == CurrentUserId);
                if (tag == null) return ServiceResult.Fail("Tag not found.");

                _context.Tags.Remove(tag);
                await _context.SaveChangesAsync();
                return ServiceResult.Ok();
            }
            catch (Exception ex)
            {
                return ServiceResult.Fail($"Error: {ex.Message}");
            }
        }
    }
}

