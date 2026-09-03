using KentOS.Kalem.Application.Dto;
using System.Diagnostics;

namespace KentOS.Kalem.Application.Services
{
    public static class TreeBuilder
    {
        public static List<BirimDto> BuildTree(List<BirimDto> items)
        {
            var lookup = items.ToLookup(i => i.UstBirimId);
            var roots = items.Where(x => x.UstBirimId == null).ToList();
            roots.ForEach(x => Debug.WriteLine(x.Ad));
            void AddChildren(BirimDto parent)
            {
                parent.Children = lookup[parent.Id].ToList();
                foreach (var child in parent.Children)
                {
                    AddChildren(child);
                }
            }

            foreach (var root in roots)
            {
                AddChildren(root);
            }

            return roots;
        }
    }
}
