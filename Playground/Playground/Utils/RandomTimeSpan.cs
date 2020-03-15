using System;

namespace Playground.Utils
{
    public class RandomTimeSpan
    {
        private static Random _seed = new Random();

        public static TimeSpan Between(int minSecond, int maxSecond) => TimeSpan.FromMilliseconds(_seed.Next(minSecond, maxSecond) * 1000);
    }
}
