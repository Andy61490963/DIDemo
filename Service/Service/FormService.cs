using ClassLibrary;
using Dapper;
using DynamicForm.Models;
using DynamicForm.Service.Interface;
using Microsoft.Data.SqlClient;

namespace DynamicForm.Service.Service;

public class FormService : IFormService
{
    private readonly SqlConnection _con;

    public FormService(SqlConnection connection)
    {
        _con = connection;
    }

    /// <summary>
    /// 取得指定表單對應檢視表的所有資料
    /// 會將下拉選單欄位的值直接轉成對應顯示文字
    /// </summary>
    public FormListDataViewModel GetFormList()
    {
        // 1. 查詢該 SCHEMA_TYPE 下的主表設定（Master）
        var master = _con.QueryFirstOrDefault<FORM_FIELD_Master>(
            "/**/SELECT * FROM FORM_FIELD_Master WHERE SCHEMA_TYPE = @TYPE",
            new { TYPE = TableSchemaQueryType.All.ToInt() });

        // 找不到主表就回傳空結果
        if (master == null)
        {
            return new FormListDataViewModel();
        }

        // 2. 取得檢視表的所有欄位名稱
        var columns = _con.Query<string>(
            "/**/SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @table",
            new { table = master.VIEW_TABLE_NAME }).ToList();

        // 3. 查出所有欄位的設定（包含下拉選欄位型別）
        var fieldConfigs = _con.Query<(Guid Id, string Column, int Type)>(
            "/**/SELECT ID, COLUMN_NAME, CONTROL_TYPE FROM FORM_FIELD_CONFIG WHERE FORM_FIELD_Master_ID = @id",
            new { id = master.BASE_TABLE_ID }).ToList();

        // 4. 將所有 Dropdown 欄位整理成列表，方便後續查找
        var dropdownColumns = fieldConfigs
            .Where(f => (FormControlType)f.Type == FormControlType.Dropdown)
            .Select(f => (f.Column, f.Id))
            .ToList();

        // 5. 查詢檢視表全部資料，並存成 List<FormDataRow>
        var rows = new List<FormDataRow>();
        
        // 存每一筆 row 的主鍵 ID
        var rowIds = new List<Guid>();
        var rawRows = _con.Query($"SELECT * FROM [{master.VIEW_TABLE_NAME}]");
        foreach (IDictionary<string, object?> row in rawRows)
        {
            var vmRow = new FormDataRow();
            foreach (var kv in row)
            {
                // 主鍵欄位要存到 vmRow.Id，方便之後查下拉選答案，比對 主表主鍵 和 設定的主鍵 有沒有相同(目前限制Guid)
                if (string.Equals(kv.Key, master.PRIMARY_KEY, StringComparison.OrdinalIgnoreCase) && kv.Value is Guid id)
                {
                    vmRow.Id = id;
                    rowIds.Add(id);
                }
                // 其他欄位裝進 Cells（key-value）
                vmRow.Cells.Add(new FormDataCell { ColumnName = kv.Key, Value = kv.Value });
            }
            rows.Add(vmRow);
        }

        // 6. 若無資料，直接回傳（節省下方查詢）
        if (!rowIds.Any())
        {
            return new FormListDataViewModel { FormId = master.ID, Columns = columns, Rows = rows };
        }

        // 7. 查出所有資料列對應的下拉選答案（一次批次查）
        var dropdownAnswers = _con.Query<(string RowId, Guid FieldId, Guid OptionId)>(
            "/**/SELECT ROW_ID, FORM_FIELD_CONFIG_ID AS FieldId, FORM_FIELD_DROPDOWN_OPTIONS_ID AS OptionId " +
            "FROM FORM_FIELD_DROPDOWN_ANSWER WHERE ROW_ID IN @RowIds",
            new { RowIds = rowIds.Select(id => id.ToString()).ToList() }).ToList();

        // 8. 撈出所有 OptionId → OptionText 的對應表（只查一次）
        var optionIds = dropdownAnswers.Select(a => a.OptionId).Distinct().ToList();
        var optionTextMap = optionIds.Any()
            ? _con.Query<(Guid Id, string Text)>(
                "/**/SELECT ID, OPTION_TEXT AS Text FROM FORM_FIELD_DROPDOWN_OPTIONS WHERE ID IN @Ids",
                new { Ids = optionIds })
                .ToDictionary(x => x.Id, x => x.Text)
            : new Dictionary<Guid, string>();

        // 9. 建立 RowId → (ConfigId → OptionId) 的對照表，加速查找
        var answerGroupMap = dropdownAnswers
            .GroupBy(a => a.RowId)
            .ToDictionary(
                g => g.Key,
                g => g.ToDictionary(x => x.FieldId, x => x.OptionId),
                StringComparer.OrdinalIgnoreCase
            );

        // 10. 將所有下拉選欄位的值替換為對應顯示文字
        foreach (var row in rows)
        {
            // 找到這一列的所有 Dropdown 答案（如果沒有就略過）
            if (!answerGroupMap.TryGetValue(row.Id.ToString(), out var answers))
            {
                continue;
            }

            // 先把所有下拉欄位的答案 OptionId 存成查找表，
            // 再回頭把資料表裡原本的 id 值，一個一個查，
            // 如果是下拉欄位，就把它覆蓋成 OptionText（顯示用文字）。
            foreach (var (columnName, configId) in dropdownColumns)
            {
                // 取得該欄位的 OptionId
                if (answers.TryGetValue(configId, out var optionId) && optionTextMap.TryGetValue(optionId, out var optionText))
                {
                    // 找到該 Cell，直接用 OptionText 覆蓋原本值
                    var cell = row.Cells.FirstOrDefault(c => string.Equals(c.ColumnName, columnName, StringComparison.OrdinalIgnoreCase));
                    if (cell != null)
                    {
                        cell.Value = optionText;
                    }
                }
            }
        }

        // 11. 組成回傳 ViewModel
        return new FormListDataViewModel
        {
            FormId = master.ID,
            Columns = columns,
            Rows = rows
        };
    }
    
    /// <summary>
    /// 取得 單一
    /// </summary>
    /// <param name="id"></param>
    /// <param name="fromId"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public FormSubmissionViewModel GetFormSubmission(Guid id, Guid? fromId = null)
    {
        // 1. 查 Master 設定
        var master = _con.QueryFirstOrDefault<FORM_FIELD_Master>(
            "SELECT * FROM FORM_FIELD_Master WHERE ID = @id", new { id });
        if (master == null)
            throw new InvalidOperationException($"FORM_FIELD_Master {id} not found");

        // 2. 取得欄位設定
        List<FormFieldInputViewModel> fields;
        if (master.SCHEMA_TYPE != (int)TableSchemaQueryType.All)
        {
            fields = GetFields(master.ID);
            return new FormSubmissionViewModel
            {
                FormId = master.ID,
                RowId = fromId,
                FormName = master.FORM_NAME,
                Fields = fields
            };
        }

        if (master.BASE_TABLE_ID is null || master.VIEW_TABLE_ID is null)
            throw new InvalidOperationException("主表與檢視表 ID 不完整");

        var baseFields = GetFields(master.BASE_TABLE_ID.Value);
        var viewFields = GetFields(master.VIEW_TABLE_ID.Value);

        var baseMap = baseFields.ToDictionary(f => f.COLUMN_NAME, f => f, StringComparer.OrdinalIgnoreCase);

        var merged = new List<FormFieldInputViewModel>();
        
        // 組合起來，判斷誰可以編輯(主檔欄位的可以編輯)
        foreach (var viewField in viewFields)
        {
            if (baseMap.TryGetValue(viewField.COLUMN_NAME, out var baseField))
            {
                baseField.SOURCE = TableSchemaQueryType.OnlyTable;
                merged.Add(baseField);
            }
            else
            {
                viewField.IS_EDITABLE = false;
                viewField.SOURCE = TableSchemaQueryType.OnlyView;
                merged.Add(viewField);
            }
        }
        
        if (!string.IsNullOrWhiteSpace(master.PRIMARY_KEY)
            && !string.IsNullOrWhiteSpace(master.VIEW_TABLE_NAME)
            && fromId != null)
        {
            var sql = $"SELECT * FROM [{master.VIEW_TABLE_NAME}] WHERE [{master.PRIMARY_KEY}] = @id";
            var dataRow = _con.QueryFirstOrDefault(sql, new { id = fromId });

            IDictionary<string, object?>? dict = null;
            if (dataRow is not null)
            {
                dict = (IDictionary<string, object?>)dataRow;
            }

            var dropdownAnswers = _con.Query<(Guid FieldId, Guid OptionId)>(
                "SELECT FORM_FIELD_CONFIG_ID AS FieldId, FORM_FIELD_DROPDOWN_OPTIONS_ID AS OptionId " +
                "FROM FORM_FIELD_DROPDOWN_ANSWER WHERE ROW_ID = @RowId",
                new { RowId = fromId })
                .ToDictionary(a => a.FieldId, a => a.OptionId);

            foreach (var field in merged)
            {
                if (field.CONTROL_TYPE == FormControlType.Dropdown)
                {
                    if (dropdownAnswers.TryGetValue(field.FieldConfigId, out var optionId))
                        field.CurrentValue = optionId;
                }
                else if (dict != null && dict.TryGetValue(field.COLUMN_NAME, out var val))
                {
                    field.CurrentValue = val;
                }
            }
        }

        return new FormSubmissionViewModel
        {
            FormId = master.ID,
            RowId = fromId,
            FormName = master.FORM_NAME,
            Fields = merged
        };
    }

    /// <summary>
    /// 取得 欄位
    /// </summary>
    /// <param name="masterId"></param>
    /// <returns></returns>
   private List<FormFieldInputViewModel> GetFields(Guid masterId)
    {
        var sql = @"SELECT FFC.*, FFM.FORM_NAME
                    FROM FORM_FIELD_CONFIG FFC
                    JOIN FORM_FIELD_Master FFM ON FFM.ID = FFC.FORM_FIELD_Master_ID
                    WHERE FFM.ID = @ID
                    ORDER BY FIELD_ORDER;

                    SELECT R.*
                    FROM FORM_FIELD_VALIDATION_RULE R
                    JOIN FORM_FIELD_CONFIG C ON R.FIELD_CONFIG_ID = C.ID
                    WHERE C.FORM_FIELD_Master_ID = @ID;

                    SELECT D.*
                    FROM FORM_FIELD_DROPDOWN D
                    JOIN FORM_FIELD_CONFIG C ON D.FORM_FIELD_CONFIG_ID = C.ID
                    WHERE C.FORM_FIELD_Master_ID = @ID;

                    SELECT O.*
                    FROM FORM_FIELD_DROPDOWN_OPTIONS O
                    JOIN FORM_FIELD_DROPDOWN D ON O.FORM_FIELD_DROPDOWN_ID = D.ID
                    JOIN FORM_FIELD_CONFIG C ON D.FORM_FIELD_CONFIG_ID = C.ID
                    WHERE C.FORM_FIELD_Master_ID = @ID;";

        using var multi = _con.QueryMultiple(sql, new { ID = masterId });

        var fieldConfigs = multi.Read<FormFieldConfigDto>().ToList();
        var validationRules = multi.Read<FormFieldValidationRuleDto>().ToList();
        var dropdownConfigs = multi.Read<FORM_FIELD_DROPDOWN>().ToList();
        var dropdownOptions = multi.Read<FORM_FIELD_DROPDOWN_OPTIONS>().ToList();

        // 用 LINQ 聚合，不需要 Dictionary
        var fieldViewModels = fieldConfigs
            .Select(field =>
            {
                // 1. 找出對應的下拉選單設定
                var dropdown = dropdownConfigs.FirstOrDefault(d => d.FORM_FIELD_CONFIG_ID == field.ID);
                var isUseSql = dropdown?.ISUSESQL ?? false;
                var dropdownId = dropdown?.ID ?? Guid.Empty;

                // 2. 找出此 dropdown 下的所有 options
                var options = dropdownOptions.Where(o => o.FORM_FIELD_DROPDOWN_ID == dropdownId).ToList();

                // 3. 根據 isUseSql 決定 option 顯示邏輯
                var finalOptions = isUseSql
                    ? options.Where(x => !string.IsNullOrWhiteSpace(x.OPTION_TABLE)).ToList()
                    : options.Where(x => string.IsNullOrWhiteSpace(x.OPTION_TABLE)).ToList();

                // 4. 找出 validation rules
                var rules = validationRules
                    .Where(r => r.FIELD_CONFIG_ID == field.ID)
                    .OrderBy(r => r.VALIDATION_ORDER)
                    .ToList();

                return new FormFieldInputViewModel
                {
                    FieldConfigId = field.ID,
                    COLUMN_NAME = field.COLUMN_NAME,
                    CONTROL_TYPE = field.CONTROL_TYPE,
                    DefaultValue = field.DEFAULT_VALUE,
                    IS_VISIBLE = field.IS_VISIBLE,
                    IS_EDITABLE = field.IS_EDITABLE,
                    ValidationRules = rules,
                    OptionList = finalOptions,
                    ISUSESQL = isUseSql,
                    DROPDOWNSQL = dropdown?.DROPDOWNSQL ?? string.Empty,
                    SOURCE = TableSchemaQueryType.OnlyTable
                };
            })
            .ToList();

        return fieldViewModels;
    }

    /// <summary>
    /// 儲存或更新表單資料（含下拉選項答案）
    /// </summary>
    /// <param name="input">前端送出的表單資料</param>
    public void SubmitForm(FormSubmissionInputModel input)
    {
        var formId = input.FormId;
        var rowId = input.RowId;

        // 1. 查詢表單主設定，確認表單是否存在，以及相關設定是否完整
        var master = _con.QueryFirstOrDefault<FORM_FIELD_Master>(
            "SELECT * FROM FORM_FIELD_Master WHERE ID = @id", new { id = formId });

        // 2. 查詢該表單所有欄位設定（FORM_FIELD_CONFIG）
        var configs = _con.Query<(Guid Id, string Column, int ControlType)>(
            "SELECT ID, COLUMN_NAME, CONTROL_TYPE FROM FORM_FIELD_CONFIG WHERE FORM_FIELD_Master_ID = @id",
            new { id = master.BASE_TABLE_ID }).ToDictionary(c => c.Id);

        // 3. 分類欄位答案（依照型態拆成兩組）
        // - normalFields: 一般型態（非下拉選）欄位，用於直接寫入主資料表
        // - dropdownAnswers: 下拉選單答案，需額外存到 FORM_FIELD_DROPDOWN_ANSWER 關聯表
        var normalFields = new List<(string Column, object? Value)>();
        var dropdownAnswers = new List<(Guid ConfigId, Guid OptionId)>();

        // 逐一處理前端傳來的欄位答案
        foreach (var field in input.InputFields)
        {
            // 欄位 ID 在設定中找不到就跳過（可能是無效欄位或設定異常）
            if (!configs.TryGetValue(field.FieldConfigId, out var cfg))
                continue;

            // 如果是 Dropdown 型態
            if ((FormControlType)cfg.ControlType == FormControlType.Dropdown)
            {
                // Dropdown 值應該是 OptionId（Guid 字串），需驗證合法性
                if (Guid.TryParse(field.Value, out var optionId))
                    dropdownAnswers.Add((cfg.Id, optionId));
            }
            else
            {
                // 其他型態直接存（如 Text, Number, Date ...）
                normalFields.Add((cfg.Column, field.Value));
            }
        }

        // 4. 判斷要 Insert 還是 Update
        // - rowId 為 null 代表新增，否則代表修改現有資料
        // - 實際 rowId 需給定（新增時產生新 Guid）
        var isInsert = rowId == null;
        var realRowId = rowId ?? Guid.NewGuid();

        // === Insert 寫入流程 ===
        if (isInsert)
        {
            // 組欄位名稱與對應參數名稱
            // - 主鍵欄位一定要寫入（Guid）
            var columns = new List<string> { $"[{master.PRIMARY_KEY}]" };
            var values = new List<string> { "@ROWID" };
            var paramDict = new Dictionary<string, object> { ["ROWID"] = realRowId };

            // 一般欄位依序帶入，組成參數字典與 SQL 字串
            int i = 0;
            foreach (var field in normalFields)
            {
                var paramName = $"VAL{i}";
                columns.Add($"[{field.Column}]");
                values.Add($"@{paramName}");
                paramDict[paramName] = field.Value;
                i++;
            }

            // 組成完整 INSERT SQL
            var sql = $"INSERT INTO [{master.BASE_TABLE_NAME}] ({string.Join(", ", columns)}) VALUES ({string.Join(", ", values)})";
            // 執行寫入
            _con.Execute(sql, paramDict);
        }
        else
        {
            // === Update 更新流程 ===
            // - 若有一般欄位才需要進行 UPDATE
            if (normalFields.Any())
            {
                var setList = new List<string>();
                var paramDict = new Dictionary<string, object> { ["ROWID"] = realRowId };
                int i = 0;
                foreach (var field in normalFields)
                {
                    var paramName = $"VAL{i}";
                    setList.Add($"[{field.Column}] = @{paramName}");
                    paramDict[paramName] = field.Value;
                    i++;
                }
                // 組成完整 UPDATE SQL
                var sql = $"UPDATE [{master.BASE_TABLE_NAME}] SET {string.Join(", ", setList)} WHERE [{master.PRIMARY_KEY}] = @ROWID";
                // 執行更新
                _con.Execute(sql, paramDict);
            }
        }

        // 5. 寫入/更新所有 Dropdown 選項答案（Upsert 到 FORM_FIELD_DROPDOWN_ANSWER）
        foreach (var (ConfigId, OptionId) in dropdownAnswers)
        {
            _con.Execute(Sql.UpsertDropdownAnswer, new { ConfigId, RowId = realRowId, OptionId });
        }
    }


private static class Sql
    {
        public const string UpsertDropdownAnswer = @"
MERGE FORM_FIELD_DROPDOWN_ANSWER AS target
USING (SELECT @ConfigId AS FORM_FIELD_CONFIG_ID, @RowId AS ROW_ID) AS src
    ON target.FORM_FIELD_CONFIG_ID = src.FORM_FIELD_CONFIG_ID AND target.ROW_ID = src.ROW_ID
WHEN MATCHED THEN
    UPDATE SET FORM_FIELD_DROPDOWN_OPTIONS_ID = @OptionId
WHEN NOT MATCHED THEN
    INSERT (ID, FORM_FIELD_CONFIG_ID, FORM_FIELD_DROPDOWN_OPTIONS_ID, ROW_ID)
    VALUES (NEWID(), src.FORM_FIELD_CONFIG_ID, @OptionId, src.ROW_ID);";
    }
}