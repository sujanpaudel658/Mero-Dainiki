using Microsoft.EntityFrameworkCore;
using Mero_Dainiki.Common;
using Mero_Dainiki.Data;
using Mero_Dainiki.Entities;
using Mero_Dainiki.Models;

namespace Mero_Dainiki.Services
{
    public interface ITagService
    {
        Task<ServiceResult<List<Tag>>> GetAllTagsAsync();
        Task<ServiceResult<Tag>> CreateTagAsync(TagViewModel model);
        Task<ServiceResult<Tag>> UpdateTagAsync(TagViewModel model);
        Task<ServiceResult> DeleteTagAsync(int id);
    }

    public class TagService : BaseService, ITagService
    {
        public TagService(AppDbContext context) : base(context) { }

        public Task<ServiceResult<List<Tag>>> GetAllTagsAsync() =>
            ExecuteAsync(async () => {
                if (!IsUserAuthenticated) throw new Exception("Unauthorized.");
                return await _context.Tags.Where(t => t.UserId == CurrentUserId).OrderBy(t => t.Name).ToListAsync();
            });

        public Task<ServiceResult<Tag>> CreateTagAsync(TagViewModel model) =>
            ExecuteAsync(async () => {
                if (!IsUserAuthenticated) throw new Exception("Unauthorized.");
                if (await _context.Tags.AnyAsync(t => t.UserId == CurrentUserId && t.Name == model.Name)) throw new Exception("Tag already exists.");

                var tag = new Tag { UserId = CurrentUserId, Name = model.Name, Color = model.Color, CreatedAt = DateTime.UtcNow };
                _context.Tags.Add(tag);
                await _context.SaveChangesAsync();
                return tag;
            });

        public Task<ServiceResult<Tag>> UpdateTagAsync(TagViewModel model) =>
            ExecuteAsync(async () => {
                if (!IsUserAuthenticated) throw new Exception("Unauthorized.");
                var tag = await _context.Tags.FirstOrDefaultAsync(t => t.Id == model.Id && t.UserId == CurrentUserId) ?? throw new Exception("Tag not found.");
                tag.Name = model.Name; tag.Color = model.Color;
                await _context.SaveChangesAsync();
                return tag;
            });

        public Task<ServiceResult> DeleteTagAsync(int id) =>
            ExecuteVoidAsync(async () => {
                if (!IsUserAuthenticated) throw new Exception("Unauthorized.");
                var tag = await _context.Tags.FirstOrDefaultAsync(t => t.Id == id && t.UserId == CurrentUserId) ?? throw new Exception("Tag not found.");
                _context.Tags.Remove(tag);
                await _context.SaveChangesAsync();
            });
    }
}
