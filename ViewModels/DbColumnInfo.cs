namespace DynamicForm.ViewModels;

public class DbColumnInfo
{
    public string COLUMN_NAME { get; set; } = "";
    public string DATA_TYPE { get; set; } = "";
    public int ORDINAL_POSITION { get; set; }

    /// <summary>
    /// 若為 View，表示此欄位對應的來源資料表名稱；
    /// 實體表則固定為自身表名。
    /// </summary>
    public string? SOURCE_TABLE { get; set; }
}
