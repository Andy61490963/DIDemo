﻿using System.ComponentModel.DataAnnotations;
using ClassLibrary;

namespace DynamicForm.Models;

public class FormDesignerIndexViewModel
{
    public FormHeaderViewModel FormHeader { get; set; }
    public FormFieldListViewModel FormField { get; set; }
    public FormFieldViewModel FieldSetting { get; set; }
}

public class FormHeaderViewModel
{
    /// <summary>
    /// FORM_FIELD_Master 主檔 ID
    /// </summary>
    public Guid ID { get; set; }
    /// <summary>
    /// 主檔名稱
    /// </summary>
    public string FORM_NAME { get; set; }
    
    /// <summary>
    /// 表名稱
    /// </summary>
    public string TABLE_NAME { get; set; }
    
    /// <summary>
    /// 前台顯示表名稱
    /// </summary>
    public string VIEW_TABLE_NAME { get; set; }

    /// <summary>
    /// 主要表單 Master ID
    /// </summary>
    public Guid? BASE_TABLE_ID { get; set; }

    /// <summary>
    /// View 表單 Master ID
    /// </summary>
    public Guid? VIEW_TABLE_ID { get; set; }
}

public class FormFieldListViewModel
{
    /// <summary>
    /// FORM_FIELD_Master
    /// </summary>
    public Guid ID { get; set; }
    public string TableName { get; set; } = string.Empty;
    
    public TableSchemaQueryType type { get; set; }
    public List<FormFieldViewModel> Fields { get; set; } = new ();
}

public class FormFieldViewModel
{
    /// <summary>
    /// PK
    /// </summary>
    public Guid ID { get; set; }
    
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
    /// 是否顯示
    /// </summary>
    public bool IS_VISIBLE { get; set; } = true;
    
    /// <summary>
    /// 是否可以編輯
    /// </summary>
    public bool IS_EDITABLE { get; set; } = true;
    
    /// <summary>
    /// 是否有輸入限制條件
    /// </summary>
    public bool IS_VALIDATION_RULE { get; set; }
    
    /// <summary>
    /// 語系
    /// </summary>
    public List<string> LANG_CODES { get; set; } = new(); // e.g. ["zh-TW", "en-US"]
    
    /// <summary>
    /// 寬度
    /// </summary>
    public int EDITOR_WIDTH { get; set; }
    
    /// <summary>
    /// 控制類別
    /// </summary>
    public FormControlType? CONTROL_TYPE { get; set; }
    
    /// <summary>
    /// 控制選擇白名單
    /// </summary>
    public List<FormControlType> CONTROL_TYPE_WHITELIST { get; set; } = new();
}