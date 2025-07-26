using System.Collections.Generic;

namespace DynamicForm.Models;

public class FORM_FIELD_Master
{
    public Guid ID { get; set; } = Guid.NewGuid();
    public string FORM_NAME { get; set; }  
    public string BASE_TABLE_NAME { get; set; }  
    public string VIEW_NAME { get; set; }
    public string PRIMARY_KEY { get; set; }
    public int STATUS { get; set; }  
    public int SCHEMA_TYPE { get; set; }  
}
