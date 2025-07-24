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
    
    public FormSubmissionViewModel GetFormSubmission()
    {
        string formName = "TOL_MASTER_View";

        string langCode = "zh-TW";
        
        // 1. 查欄位設定 + 語系
        var sqlField = @"
    SELECT 
        C.ID AS FieldConfigId,
        C.COLUMN_NAME,
        C.CONTROL_TYPE,
        C.IS_VISIBLE,
        C.IS_EDITABLE,
        C.COLUMN_SPAN,
        C.IS_SECTION_START,
        C.DEFAULT_VALUE,
        L.LABEL,
        L.PLACEHOLDER,
        L.HELP_TEXT
    FROM FORM_FIELD_CONFIG C
    LEFT JOIN FORM_FIELD_LANG L
        ON C.ID = L.FIELD_CONFIG_ID
    WHERE C.FORM_NAME = @FormName
    ORDER BY C.FIELD_ORDER";

        var fields = _con.Query<FormFieldInputViewModel>(sqlField, new { FormName = formName, LangCode = langCode }).ToList();

        var fieldIds = fields.Select(f => f.FieldConfigId).ToList();

        // 2. 查驗證規則
        var validations = _con.Query<FormFieldValidationRuleDto>(@"
    SELECT * FROM FORM_FIELD_VALIDATION_RULE
    WHERE FIELD_CONFIG_ID IN @Ids
    ORDER BY VALIDATION_ORDER", new { Ids = fieldIds }).ToList();

        // 3. 查下拉選單來源
        var optionSources = _con.Query<FormFieldValidationRuleDto>(@"
    SELECT * FROM FORM_FIELD_OPTION_SOURCE
    WHERE FIELD_CONFIG_ID IN @Ids", new { Ids = fieldIds }).ToList();

        // 4. 綁定驗證、選項清單到欄位
        foreach (var field in fields)
        {
            // 綁定驗證規則
            field.ValidationRules = validations
                .Where(v => v.FIELD_CONFIG_ID == field.FieldConfigId)
                .ToList();

            // 綁定選項資料
            var opt = optionSources.FirstOrDefault(o => o.FIELD_CONFIG_ID == field.FieldConfigId);
            if (opt != null)
            {
                field.OptionList = ["test","test1"];
            }

            field.UserValue = ""; // 初始值
        }

        return new FormSubmissionViewModel
        {
            FormName = formName,
            Fields = fields
        };
    }

}