using System.Security.Cryptography;

namespace DynamicForm.Helper;

public static class RandomDecimalHelper
{
    public static decimal GenerateRandomDecimal()
    {
        var bytes = new byte[8];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }

        long value = BitConverter.ToInt64(bytes, 0) & long.MaxValue;
        return new decimal(value % 1_000_000_000_000_000_000L);
    }
}
