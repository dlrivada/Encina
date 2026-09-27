```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | PeriodCount | Mean        | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------ |------------:|-----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| GetOrCreateExistingKey | 12          |   161.31 ns |  20.828 ns |  1.142 ns |  1.00 |    0.01 |    2 | 0.0129 |      - |     328 B |        0.93 |
| IsKeyDestroyed         | 12          |    98.13 ns |   2.381 ns |  0.131 ns |  0.61 |    0.00 |    1 | 0.0044 |      - |     112 B |        0.32 |
| GetActiveKeysCount     | 12          | 3,000.89 ns | 490.168 ns | 26.868 ns | 18.63 |    0.18 |    3 | 0.0954 |      - |    2464 B |        7.00 |
| CreateNewKey           | 12          | 3,766.75 ns | 669.578 ns | 36.702 ns | 23.38 |    0.24 |    4 | 0.0305 | 0.0267 |     848 B |        2.41 |
| GetExistingKey         | 12          |   161.09 ns |  18.672 ns |  1.023 ns |  1.00 |    0.01 |    2 | 0.0138 |      - |     352 B |        1.00 |
