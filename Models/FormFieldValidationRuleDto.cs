namespace DynamicForm.Models;

public class FormFieldValidationRuleDto
{
    public Guid ID { get; set; }
    public Guid FIELD_CONFIG_ID { get; set; }
    public string VALIDATION_TYPE { get; set; }
    public string VALIDATION_VALUE { get; set; }
    public string MESSAGE_ZH { get; set; }
    public string MESSAGE_EN { get; set; }
    public int VALIDATION_ORDER { get; set; }
}