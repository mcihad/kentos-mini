using Microsoft.EntityFrameworkCore;
using KentOS.Kalem.Application.Models;

namespace KentOS.Kalem.Web.Extensions
{
    public static class BirimExtensions
    {
        public static IQueryable<Birim> GetDescendants(this IQueryable<Birim> birimler, long parentId,bool selfInclude=false)
        {
            var result = new List<Birim>();
            var queue = new Queue<long>();
            var self = birimler.FirstOrDefault(b => b.Id == parentId);
            queue.Enqueue(parentId);
            
            while (queue.Count > 0)
            {
                var currentId = queue.Dequeue();
                var children = birimler.Where(b => b.UstBirimId == currentId);

                foreach (var child in children)
                {
                    result.Add(child);
                    queue.Enqueue(child.Id);
                }
            }

            if (selfInclude && self != null)
            {
                result.Add(self);
            }

            return result.AsQueryable();
        }

        public static IQueryable<Birim> GetDescendantsWithLevel(this IQueryable<Birim> birimler, long parentId)
        {
            var parent = birimler.FirstOrDefault(b => b.Id == parentId);
            if (parent == null) return Enumerable.Empty<Birim>().AsQueryable();

            return birimler.Where(b =>
                EF.Functions.Like(b.GetFullName(), $"{parent.GetFullName()}/%"));
        }
    }
}
