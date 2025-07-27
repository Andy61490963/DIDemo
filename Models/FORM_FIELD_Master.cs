using System.Collections.Generic;

namespace DynamicForm.Models;

public class FORM_FIELD_Master
{
    public Guid ID { get; set; } = Guid.NewGuid();
    public string FORM_NAME { get; set; }  
    public string BASE_TABLE_NAME { get; set; }  
    /// <summary>
    /// 前台展示用的 View 名稱
    /// </summary>
    public string VIEW_TABLE_NAME { get; set; }

    /// <summary>
    /// 對應主表的 FORM_FIELD_Master ID
    /// </summary>
    public Guid? BASE_TABLE_ID { get; set; }

    /// <summary>
    /// 對應 View 的 FORM_FIELD_Master ID
    /// </summary>
    public Guid? VIEW_TABLE_ID { get; set; }
    public string PRIMARY_KEY { get; set; }
    public int STATUS { get; set; }  
    public int SCHEMA_TYPE { get; set; }  
}
