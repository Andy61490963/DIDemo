using System.Security.Cryptography;

namespace DynamicForm.Helper;

public static class RandomHelper
{
    // 雪花簡化版：32 bit timestamp(ms) << 22 | 22 bit 亂數
    private static readonly Random _rnd = new();
    private static readonly object _lock = new();

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
    
    public static long NextSnowflakeId()
    {
        lock (_lock)
        {
            var ms = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var rand = _rnd.Next(0, 1 << 22);      
            return (ms << 22) | (uint)rand;
        }
    }
}
