using ClassLibrary;

namespace DynamicForm.ViewModels;

public class FormFieldViewModel
{
    /// <summary>
    /// PK
    /// </summary>
    public Guid ID { get; set; }

    /// <summary>
    /// 這個欄位是否為Pk
    /// </summary>
    public bool IS_PK { get; set; }

    /// <summary>
    /// parent
    /// </summary>
    public Guid FORM_FIELD_Master_ID { get; set; }

    /// <summary>
    /// 表名稱
    /// </summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// 欄位名稱
    /// </summary>
    public string COLUMN_NAME { get; set; } = string.Empty;

    /// <summary>
    /// 欄位資料結構類型
    /// </summary>
    public string DATA_TYPE { get; set; } = string.Empty;

    /// <summary>
    /// 預設值
    /// </summary>
    public string DEFAULT_VALUE { get; set; } = string.Empty;

    /// <summary>
    /// 是否可以編輯
    /// </summary>
    public bool IS_EDITABLE { get; set; } = true;

    /// <summary>
    /// 是否必填
    /// </summary>
    public bool IS_REQUIRED { get; set; } = true;
    
    /// <summary>
    /// 排序
    /// </summary>
    public int FIELD_ORDER { get; set; }

    /// <summary>
    /// 是否有輸入限制條件
    /// </summary>
    public bool IS_VALIDATION_RULE { get; set; }

    /// <summary>
    /// 控制類別
    /// </summary>
    public FormControlType? CONTROL_TYPE { get; set; }

    /// <summary>
    /// 控制選擇白名單
    /// </summary>
    public List<FormControlType> CONTROL_TYPE_WHITELIST { get; set; } = new();

    /// <summary>
    /// 資料表查詢類型，更新欄位設定後重新載入清單時使用
    /// </summary>
    public TableSchemaQueryType SchemaType { get; set; }
}

