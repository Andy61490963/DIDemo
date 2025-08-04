using System.ComponentModel.DataAnnotations;
using System.Reflection;
using DynamicForm.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ClassLibrary;

public static class EnumExtensions
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="obj">列舉物件</param>
    /// <returns></returns>
    public static int ToInt( this Enum obj )
    {
        return Convert.ToInt32( obj );
    }
    
    public static List<SelectListItem> ToSelectList<TEnum>() where TEnum : Enum
    {
        return Enum.GetValues(typeof(TEnum))
            .Cast<TEnum>()
            .Select(e => new SelectListItem
            {
                Value = Convert.ToInt32(e).ToString(),
                Text = GetDisplayName(e)
            })
            .ToList();
    }

    public static List<ValidationTypeOptionDto> ToSelectList<TEnum>(IEnumerable<TEnum> values) where TEnum : Enum
    {
        return values
            .Select(e => new ValidationTypeOptionDto
            {
                Value = Convert.ToInt32(e).ToString(),
                Text = GetDisplayName(e)
            })
            .ToList();
    }
    
    public static string GetDisplayName<TEnum>(TEnum enumValue) where TEnum : Enum
    {
        var member = typeof(TEnum).GetMember(enumValue.ToString()).FirstOrDefault();
        if (member != null)
        {
            var displayAttr = member.GetCustomAttribute<DisplayAttribute>();
            if (displayAttr != null)
                return displayAttr.Name ?? enumValue.ToString();
        }
        return enumValue.ToString();
    }
}
