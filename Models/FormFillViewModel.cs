namespace DynamicForm.Models;

public class FormFillViewModel
{
    public FormMaster Form { get; set; }

    public List<FormField> Fields { get; set; } = new();
}
