using System;

namespace DynamicForm.Models;

public class DATA_SOURCE_MASTER
{
    public Guid ID { get; set; } = Guid.NewGuid();
    public string SOURCE_NAME { get; set; } = string.Empty;
    public byte SOURCE_TYPE { get; set; }
    public string? PRIMARY_KEY { get; set; }
}
