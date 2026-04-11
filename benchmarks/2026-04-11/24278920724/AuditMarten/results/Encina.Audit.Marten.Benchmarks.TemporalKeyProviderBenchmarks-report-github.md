```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                 | PeriodCount | Mean        | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------ |------------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| GetOrCreateExistingKey | 12          |   155.54 ns |  2.208 ns |  3.237 ns |  1.08 |    0.03 |    3 | 0.0129 |      - |     328 B |        0.93 |
| IsKeyDestroyed         | 12          |    95.04 ns |  0.366 ns |  0.537 ns |  0.66 |    0.01 |    1 | 0.0044 |      - |     112 B |        0.32 |
| GetActiveKeysCount     | 12          | 2,980.31 ns | 12.580 ns | 18.439 ns | 20.70 |    0.39 |    4 | 0.0954 |      - |    2464 B |        7.00 |
| CreateNewKey           | 12          | 3,841.98 ns | 61.999 ns | 84.865 ns | 26.68 |    0.74 |    5 | 0.0305 | 0.0267 |     848 B |        2.41 |
| GetExistingKey         | 12          |   144.05 ns |  1.750 ns |  2.620 ns |  1.00 |    0.03 |    2 | 0.0138 |      - |     352 B |        1.00 |
