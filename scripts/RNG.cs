using System;

/// <summary>统一随机数生成器，避免 new Random() / RandomNumberGenerator / Random.Shared 混用。</summary>
public static class RNG
{
    private static readonly Random _rng = new();

    /// <summary>[0~1) 浮点数</summary>
    public static double NextDouble() => _rng.NextDouble();

    /// <summary>[min, max) 整数</summary>
    public static int NextInt(int min, int max) => _rng.Next(min, max);

    /// <summary>[0, max) 整数</summary>
    public static int NextInt(int max) => _rng.Next(max);

    /// <summary>min~max 浮点数</summary>
    public static float NextFloat(float min = 0f, float max = 1f) => (float)(_rng.NextDouble() * (max - min) + min);

    /// <summary>百分概率判断</summary>
    public static bool Chance(float percent) => _rng.NextDouble() * 100 < percent;

    /// <summary>从数组中随机选一个</summary>
    public static T Pick<T>(T[] items) => items[_rng.Next(items.Length)];

    /// <summary>从列表中随机选一个</summary>
    public static T Pick<T>(System.Collections.Generic.List<T> items) => items[_rng.Next(items.Count)];
}
