namespace DynamicForm.Models;

public class DbColumnInfo
{
    public string COLUMN_NAME { get; set; } = "";
    public string DATA_TYPE { get; set; } = "";
    public int ORDINAL_POSITION { get; set; }
    // 來源資料表，用於判斷欄位實際來自哪一張表
    public string SOURCE_TABLE { get; set; } = "";
}
