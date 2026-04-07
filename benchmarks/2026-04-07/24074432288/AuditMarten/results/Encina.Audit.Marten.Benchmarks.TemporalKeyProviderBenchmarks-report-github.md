```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                 | PeriodCount | Mean       | Error    | StdDev    | Median     | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------ |-----------:|---------:|----------:|-----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| GetOrCreateExistingKey | 12          |   162.2 ns |  1.14 ns |   1.67 ns |   162.4 ns |  1.07 |    0.05 |    2 | 0.0196 |      - |     328 B |        0.93 |
| IsKeyDestroyed         | 12          |   113.9 ns |  2.26 ns |   3.39 ns |   113.9 ns |  0.75 |    0.04 |    1 | 0.0067 |      - |     112 B |        0.32 |
| GetActiveKeysCount     | 12          | 2,981.3 ns | 83.43 ns | 116.96 ns | 3,069.1 ns | 19.63 |    1.24 |    3 | 0.1450 |      - |    2464 B |        7.00 |
| CreateNewKey           | 12          | 4,267.8 ns | 92.26 ns | 132.32 ns | 4,253.7 ns | 28.10 |    1.64 |    4 | 0.0496 | 0.0458 |     848 B |        2.41 |
| GetExistingKey         | 12          |   152.3 ns |  5.17 ns |   7.74 ns |   151.6 ns |  1.00 |    0.07 |    2 | 0.0210 |      - |     352 B |        1.00 |
