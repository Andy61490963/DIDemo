using ClassLibrary;
using Dapper;
using DynamicForm.Helper;
using DynamicForm.Models;
using DynamicForm.Service.Interface;
using DynamicForm.Service.Interface.FormLogicInterface;
using DynamicForm.Service.Interface.TransactionInterface;
using DynamicForm.ViewModels;
using Microsoft.Data.SqlClient;

namespace DynamicForm.Service.Service;

public class FormService : IFormService
{
    private readonly SqlConnection _con;
    private readonly ITransactionService _txService;
    private readonly IConfiguration _configuration;
    private readonly IFormFieldMasterService _formFieldMasterService;
    private readonly ISchemaService _schemaService;
    private readonly IFormFieldConfigService _formFieldConfigService;
    private readonly IFormDataService _formDataService;
    private readonly IDropdownService _dropdownService;
    
    public FormService(SqlConnection connection, ITransactionService txService, IFormFieldMasterService formFieldMasterService, ISchemaService schemaService, IFormFieldConfigService formFieldConfigService, IDropdownService dropdownService, IFormDataService formDataService, IConfiguration configuration)
    {
        _con = connection;
        _txService = txService;
        _configuration = configuration;
        _formFieldMasterService = formFieldMasterService;
        _schemaService = schemaService;
        _formFieldConfigService = formFieldConfigService;
        _formDataService = formDataService;
        _dropdownService = dropdownService;
        _excludeColumns = _configuration.GetSection("DropdownSqlSettings:ExcludeColumns").Get<List<string>>() ?? new();
    }
    
    private readonly List<string> _excludeColumns;

    /// <summary>
    /// 取得指定 SCHEMA_TYPE 下的表單資料清單，
    /// 已自動將下拉選欄位的值轉為顯示文字（OptionText）
    /// </summary>
    public FormListDataViewModel GetFormList()
    {
        // 1. 查詢主表（Form 設定 Master），根據 SCHEMA_TYPE 取得表單主設定
        // var master = _formFieldMasterService.GetFormFieldMaster(TableSchemaQueryType.All);
        //
        // // [防呆] 找不到主表就直接回傳空的 ViewModel，避免後續 NullReference
        // if (master == null) 
        //     return new FormListDataViewModel();
        //
        // // 2. 取得檢視表的所有欄位名稱（資料表的 Schema）
        // var columns = _schemaService.GetFormFieldMaster(master.VIEW_TABLE_NAME);
        //
        // // 3. 取得該表單的所有欄位設定（包含型別、控制項型態等）
        // var fieldConfigs = _formFieldConfigService.GetFormFieldConfig(master.BASE_TABLE_ID);

        var (master, columns, fieldConfigs) = _formFieldMasterService.GetFormMetaAggregate(TableSchemaQueryType.All);
        
        // 4. 取得檢視表的所有原始資料（rawRows 為每列 Dictionary<string, object?>）
        var rawRows = _formDataService.GetRows(master.VIEW_TABLE_NAME);

        var pk = _schemaService.GetPrimaryKeyColumn(master.BASE_TABLE_NAME);
        
        if (pk == null)
            throw new InvalidOperationException("No primary key column found");
        
        // 5. 將 rawRows 轉換為 FormDataRow（每列帶主鍵 Id 與所有欄位 Cell）
        //    同時收集所有資料列的主鍵 rowIds
        var rows = _dropdownService.ToFormDataRows(rawRows, pk, out var rowIds);

        // 6. 若無任何資料列，直接回傳結果，省略後面下拉選查詢
        if (!rowIds.Any())
            return new FormListDataViewModel { FormId = master.ID, Columns = columns, Rows = rows };
        
        // 7. 取得所有資料列的下拉選答案（一次查全部，不 N+1）
        var dropdownAnswers = _dropdownService.GetAnswers(rowIds);
        
        // 8. 取得所有 OptionId → OptionText 的對照表
        var optionTextMap = _dropdownService.GetOptionTextMap(dropdownAnswers);
        
        // 9. 將 rows 裡所有下拉選欄位的值由 OptionId 轉換為 OptionText（顯示文字）
        _dropdownService.ReplaceDropdownIdsWithTexts(rows, fieldConfigs, dropdownAnswers, optionTextMap);

        // 10. 組裝並回傳最終的 ViewModel
        return new FormListDataViewModel
        {
            FormId = master.ID,
            Columns = columns,
            Rows = rows
        };
    }

    /// <summary>
    /// 根據表單設定抓取主表欄位與現有資料（編輯時用）
    /// 只對主表進行欄位組裝，Dropdown 顯示選項答案
    /// </summary>
    public FormSubmissionViewModel GetFormSubmission(Guid formMasterId, string? pk = null)
    {
        // 1. 查主設定
        var master = _formFieldMasterService.GetFormFieldMasterFromId(formMasterId);
        if (master == null)
            throw new InvalidOperationException($"FORM_FIELD_Master {formMasterId} not found");
        if (master.BASE_TABLE_ID is null)
            throw new InvalidOperationException("主表設定不完整");

        // 2. 取得主表欄位（只抓主表，不抓 view）
        var fields = GetFields(master.BASE_TABLE_ID.Value, TableSchemaQueryType.OnlyTable, master.BASE_TABLE_NAME);

        // 3. 撈主表實際資料（如果是編輯模式）
        IDictionary<string, object?>? dataRow = null;
        Dictionary<Guid, Guid>? dropdownAnswers = null;

        if (!string.IsNullOrWhiteSpace(pk))
        {
            // 3.1 取得主表主鍵名稱/型別/值
            var (pkName, pkType, pkValue) = _schemaService.ResolvePk(master.BASE_TABLE_NAME, pk);

            // 3.2 查詢主表資料（參數化防注入）
            var sql = $"SELECT * FROM [{master.BASE_TABLE_NAME}] WHERE [{pkName}] = @id";
            dataRow = _con.QueryFirstOrDefault(sql, new { id = pkValue }) as IDictionary<string, object?>;

            // 3.3 如果有Dropdown欄位，再查一次答案
            if (fields.Any(f => f.CONTROL_TYPE == FormControlType.Dropdown))
            {
                dropdownAnswers = _con.Query<(Guid FieldId, Guid OptionId)>(
                    @"/**/SELECT FORM_FIELD_CONFIG_ID AS FieldId, FORM_FIELD_DROPDOWN_OPTIONS_ID AS OptionId 
                      FROM FORM_FIELD_DROPDOWN_ANSWER WHERE ROW_ID = @Pk",
                    new { Pk = pk })
                    .ToDictionary(x => x.FieldId, x => x.OptionId);
            }
        }

        // 4. 組裝欄位現值
        foreach (var field in fields)
        {
            if (field.CONTROL_TYPE == FormControlType.Dropdown && dropdownAnswers?.TryGetValue(field.FieldConfigId, out var optId) == true)
            {
                field.CurrentValue = optId;
            }
            else if (dataRow?.TryGetValue(field.Column, out var val) == true)
            {
                field.CurrentValue = val;
            }
            // else 預設 null（新增模式或沒有資料）
        }

        // 5. 回傳組裝後 ViewModel
        return new FormSubmissionViewModel
        {
            FormId = master.ID,
            Pk = pk,
            TargetTableToUpsert = master.BASE_TABLE_NAME,
            FormName = master.FORM_NAME,
            Fields = fields
        };
    }
    
    /// <summary>
    /// 取得 欄位
    /// </summary>
    /// <param name="masterId"></param>
    /// <returns></returns>
   private List<FormFieldInputViewModel> GetFields(Guid masterId, TableSchemaQueryType schemaType, string tableName)
    {
        var columnTypes = _formDataService.LoadColumnTypes(tableName);
        var configData = _formFieldConfigService.LoadFieldConfigData(masterId);

        // 只保留可編輯欄位，將不可編輯欄位直接過濾掉以避免出現在前端
        var editableConfigs = configData.FieldConfigs
            .Where(cfg => cfg.IS_EDITABLE)
            .ToList();

        return editableConfigs
            .Select(cfg => BuildFieldViewModel(cfg, configData, columnTypes, schemaType))
            .ToList();
    }

    private FormFieldInputViewModel BuildFieldViewModel(
        FormFieldConfigDto field,
        FieldConfigData data,
        Dictionary<string, string> columnTypes,
        TableSchemaQueryType schemaType)
    {
        var dropdown = data.DropdownConfigs.FirstOrDefault(d => d.FORM_FIELD_CONFIG_ID == field.ID);
        var isUseSql = dropdown?.ISUSESQL ?? false;
        var dropdownId = dropdown?.ID ?? Guid.Empty;

        var options = data.DropdownOptions.Where(o => o.FORM_FIELD_DROPDOWN_ID == dropdownId).ToList();
        var finalOptions = isUseSql
            ? options.Where(x => !string.IsNullOrWhiteSpace(x.OPTION_TABLE)).ToList()
            : options.Where(x => string.IsNullOrWhiteSpace(x.OPTION_TABLE)).ToList();

        var rules = data.ValidationRules
            .Where(r => r.FIELD_CONFIG_ID == field.ID)
            .OrderBy(r => r.VALIDATION_ORDER)
            .ToList();

        var dataType = columnTypes.TryGetValue(field.COLUMN_NAME, out var dtype)
            ? dtype
            : "nvarchar";

        return new FormFieldInputViewModel
        {
            FieldConfigId = field.ID,
            Column = field.COLUMN_NAME,
            CONTROL_TYPE = field.CONTROL_TYPE,
            DefaultValue = field.DEFAULT_VALUE,
            IS_REQUIRED = field.IS_REQUIRED,
            IS_EDITABLE = field.IS_EDITABLE,
            ValidationRules = rules,
            OptionList = finalOptions,
            ISUSESQL = isUseSql,
            DROPDOWNSQL = dropdown?.DROPDOWNSQL ?? string.Empty,
            DATA_TYPE = dataType,
            SOURCE_TABLE = TableSchemaQueryType.OnlyTable
        };
    }

    /// <summary>
    /// 儲存或更新表單資料（含下拉選項答案）
    /// </summary>
    /// <param name="input">前端送出的表單資料</param>
    public void SubmitForm(FormSubmissionInputModel input)
    {
        _txService.WithTransaction(tx =>
        {
            var formId = input.FormId;
            var pk = input.Pk;

            // 查表單主設定
            var master = _formFieldMasterService.GetFormFieldMasterFromId(formId, tx);

            // 查欄位設定
            // 取得欄位設定並帶出 IS_EDITABLE 欄位，後續用於權限檢查
            var configs = _con.Query<FormFieldConfigDto>(
                "SELECT ID, COLUMN_NAME, CONTROL_TYPE, DATA_TYPE, IS_EDITABLE FROM FORM_FIELD_CONFIG WHERE FORM_FIELD_Master_ID = @Id",
                new { Id = master.BASE_TABLE_ID },
                transaction: tx).ToDictionary(x => x.ID);

            // 1. 欄位 mapping & 型別處理
            var (normalFields, dropdownAnswers) = MapInputFields(input.InputFields, configs);

            // 2. Insert/Update 決策
            var (pkName, pkType, typedRowId) = _schemaService.ResolvePk(master.BASE_TABLE_NAME, pk, tx);
            bool isInsert = string.IsNullOrEmpty(pk);
            bool isIdentity = _schemaService.IsIdentityColumn(master.BASE_TABLE_NAME, pkName, tx);
            object? realRowId = typedRowId;

            if (isInsert)
                realRowId = InsertRow(master, pkName, pkType, isIdentity, normalFields, tx);
            else
                UpdateRow(master, pkName, normalFields, realRowId, tx);

            // 3. Dropdown Upsert
            foreach (var (configId, optionId) in dropdownAnswers)
            {
                _con.Execute(Sql.UpsertDropdownAnswer, new { configId, RowId = realRowId, optionId }, transaction: tx);
            }
        });
    }

    private (List<(string Column, object? Value)> NormalFields,
        List<(Guid ConfigId, Guid OptionId)> DropdownAnswers)
        MapInputFields(IEnumerable<FormInputField> inputFields,
            IReadOnlyDictionary<Guid, FormFieldConfigDto> configs)
    {
        var normal = new List<(string Column, object? Value)>();
        var ddAns  = new List<(Guid ConfigId, Guid OptionId)>();

        foreach (var field in inputFields)
        {
            if (!configs.TryGetValue(field.FieldConfigId, out var cfg))
                continue;                               // 找不到設定直接忽略

            // 欄位若設定為不可編輯，直接忽略以防止未授權修改
            if (!cfg.IS_EDITABLE)
                continue;

            // // --- 必填檢查 ---
            // if (cfg.IS_REQUIRED && string.IsNullOrWhiteSpace(field.Value))
            //     throw new ValidationException($"欄位「{cfg.COLUMN_NAME}」為必填。");
            //
            // if (string.IsNullOrEmpty(field.Value))
            //     continue;

            if (cfg.CONTROL_TYPE == FormControlType.Dropdown)
            {
                if (Guid.TryParse(field.Value, out var optId))
                    ddAns.Add((cfg.ID, optId));
            }
            else
            {
                var val = ConvertToColumnTypeHelper.Convert(cfg.DATA_TYPE, field.Value);
                normal.Add((cfg.COLUMN_NAME, val));
            }
        }
        return (normal, ddAns);
    }

    /// <summary>
    /// 實作 INSERT 資料邏輯，支援 Identity 與非 Identity 主鍵模式
    /// </summary>
    private object InsertRow(
        FORM_FIELD_Master master,
        string pkName,
        string pkType,
        bool isIdentity,
        List<(string Column, object? Value)> normalFields,
        SqlTransaction tx 
    )
    {
        const string RowIdParamName = "ROWID";
        var columns = new List<string>();
        var values = new List<string>();
        var paramDict = new Dictionary<string, object>();

        // 若主鍵非 Identity，手動產生主鍵值
        if (!isIdentity)
        {
            var newId = GeneratePkValueHelper.GeneratePkValue(pkType); // 支援 Guid / int / string 等
            columns.Add($"[{pkName}]");
            values.Add($"@{RowIdParamName}");
            paramDict[RowIdParamName] = newId!;
        }

        int i = 0;
        foreach (var field in normalFields)
        {
            if (string.Equals(field.Column, pkName, StringComparison.OrdinalIgnoreCase))
                continue;

            var paramName = $"VAL{i++}";
            columns.Add($"[{field.Column}]");
            values.Add($"@{paramName}");
            paramDict[paramName] = field.Value;
        }

        string sql;
        object? resultId;

        if (isIdentity && !normalFields.Any())
        {
            sql = $@"
                INSERT INTO [{master.BASE_TABLE_NAME}] DEFAULT VALUES;
                SELECT CAST(SCOPE_IDENTITY() AS {pkType});";

            resultId = _con.ExecuteScalar(sql, transaction: tx);
        }
        else if (isIdentity)
        {
            sql = $@"
                INSERT INTO [{master.BASE_TABLE_NAME}]
                    ({string.Join(", ", columns)})
                OUTPUT INSERTED.[{pkName}]
                VALUES ({string.Join(", ", values)})";

            resultId = _con.ExecuteScalar(sql, paramDict, tx); 
        }
        else
        {
            sql = $@"
                INSERT INTO [{master.BASE_TABLE_NAME}]
                    ({string.Join(", ", columns)})
                VALUES ({string.Join(", ", values)})";

            _con.Execute(sql, paramDict, tx); 
            resultId = paramDict[RowIdParamName];
        }

        return resultId!;
    }
    
    /// <summary>
    /// 動態產生 update
    /// </summary>
    /// <summary>
    /// 動態產生並執行 UPDATE 語法，用於更新資料表中的指定主鍵資料列。
    /// </summary>
    /// <param name="master">表單主設定資料（包含 Base Table 與主鍵欄位名稱）</param>
    /// <param name="pkName">表單 pkName </param>
    /// <param name="normalFields">需要更新的欄位集合（欄位名與新值）</param>
    /// <param name="realRowId">實際的主鍵值（用於 WHERE 條件）</param>
    /// <param name="tx">交易條件</param>
    private void UpdateRow(
        FORM_FIELD_Master master,
        string pkName,
        List<(string Column, object? Value)> normalFields,
        object realRowId,
        SqlTransaction tx)
    {
        // 若無更新欄位，直接結束，不執行 SQL
        if (!normalFields.Any()) return;

        // 動態產生 SET 子句，並準備對應參數字典
        var setList = new List<string>();
        var paramDict = new Dictionary<string, object> { ["ROWID"] = realRowId! };

        int i = 0;
        foreach (var field in normalFields)
        {
            // 每一個欄位會對應一組參數：VAL0、VAL1、... 以避免參數衝突
            var paramName = $"VAL{i}";
            setList.Add($"[{field.Column}] = @{paramName}");         // 欄位名用中括號包起來避免保留字
            paramDict[paramName] = field.Value ?? null;              // 允許欄位值為 null
            i++;
        }

        // 組合最終 SQL 語句：UPDATE 表 SET 欄位1 = @, 欄位2 = @ ... WHERE 主鍵 = @ROWID
        var sql = $@"
        UPDATE [{master.BASE_TABLE_NAME}] 
        SET {string.Join(", ", setList)} 
        WHERE [{pkName}] = @ROWID";

        _con.Execute(sql, paramDict, transaction: tx);
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
