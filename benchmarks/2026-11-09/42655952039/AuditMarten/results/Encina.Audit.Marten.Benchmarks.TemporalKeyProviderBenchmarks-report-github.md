```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.70GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | PeriodCount | Mean       | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------ |-----------:|------------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| GetOrCreateExistingKey | 12          |   166.4 ns |     3.48 ns |   0.19 ns |  1.07 |    0.00 |    2 | 0.0129 |      - |     328 B |        0.93 |
| IsKeyDestroyed         | 12          |   101.4 ns |     3.38 ns |   0.19 ns |  0.65 |    0.00 |    1 | 0.0044 |      - |     112 B |        0.32 |
| GetActiveKeysCount     | 12          | 2,991.7 ns |   204.31 ns |  11.20 ns | 19.18 |    0.06 |    3 | 0.0954 |      - |    2464 B |        7.00 |
| CreateNewKey           | 12          | 3,609.3 ns | 2,215.04 ns | 121.41 ns | 23.14 |    0.67 |    4 | 0.0305 | 0.0267 |     848 B |        2.41 |
| GetExistingKey         | 12          |   155.9 ns |     1.67 ns |   0.09 ns |  1.00 |    0.00 |    2 | 0.0138 |      - |     352 B |        1.00 |
