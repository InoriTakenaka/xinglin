using System.Collections.Generic;

namespace xinglin.Models
{
    public class TemplateTreeItem
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public List<TemplateTreeItem> Children { get; set; }

        public TemplateTreeItem()
        {
            Children = new List<TemplateTreeItem>();
        }
    }
}
