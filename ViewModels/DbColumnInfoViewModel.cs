﻿namespace DynamicForm.Models;

public class DbColumnInfo
{
    public string COLUMN_NAME { get; set; } = "";
    public string DATA_TYPE { get; set; } = "";
    public int ORDINAL_POSITION { get; set; }
}
