using System;

namespace DynamicForm.Models;

public class FORM_FIELD_Master
{
    public Guid ID { get; set; } = Guid.NewGuid();
    public string FORM_NAME { get; set; } = string.Empty;
    public Guid BASE_TABLE_ID { get; set; }
    public Guid? VIEW_ID { get; set; }
    public int STATUS { get; set; }
    public int SCHEMA_TYPE { get; set; }

    // 以下欄位僅供顯示使用，透過 JOIN DATA_SOURCE_MASTER 取得
    public string? BASE_TABLE_NAME { get; set; }
    public string? VIEW_NAME { get; set; }
}
