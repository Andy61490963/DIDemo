using System;
using System.Collections.Generic;

namespace DynamicForm.Areas.Permission.ViewModels.Menu
{
    /// <summary>
    /// 表示側邊選單的節點，包含子節點資訊。
    /// </summary>
    public class MenuTreeItem
    {
        /// <summary>唯一識別碼。</summary>
        public Guid Id { get; set; }

        /// <summary>父節點 ID，根節點為 null。</summary>
        public Guid? ParentId { get; set; }

        /// <summary>功能 ID。</summary>
        public Guid? SysFunctionId { get; set; }

        /// <summary>選單名稱。</summary>
        public string? Name { get; set; }

        /// <summary>排序。</summary>
        public int Sort { get; set; }

        /// <summary>是否為共用選單。</summary>
        public bool IsShare { get; set; }

        /// <summary>Area 名稱。</summary>
        public string? Area { get; set; }

        /// <summary>Controller 名稱。</summary>
        public string? Controller { get; set; }

        /// <summary>子選單節點集合。</summary>
        public List<MenuTreeItem> Children { get; set; } = new();
    }
}
