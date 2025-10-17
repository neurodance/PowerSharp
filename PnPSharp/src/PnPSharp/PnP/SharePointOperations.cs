using PnP.Core.Model.SharePoint;
using PnP.Core.Services;
using System.Threading.Tasks;

namespace PnPSharp.PnP
{
    public static class SharePointOperations
    {
        public static async Task<IWeb> GetWebAsync(PnPContext ctx)
        {
            return await ctx.Web.GetAsync(p => p.Title, p => p.Url);
        }

        public static async Task<IList> EnsureListAsync(PnPContext ctx, string title)
        {
            var list = await ctx.Web.Lists.AddAsync(
                title, 
                ListTemplateType.GenericList);
            return list;
        }
    }
}