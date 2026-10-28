```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.85GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 137.121 ns | 36.826 ns | 2.0185 ns | 15.55 |    1.09 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   8.856 ns | 13.017 ns | 0.7135 ns |  1.00 |    0.10 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 641.488 ns | 67.630 ns | 3.7070 ns | 72.75 |    5.04 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 606.845 ns | 95.231 ns | 5.2200 ns | 68.82 |    4.78 | 0.0601 |    1008 B |       42.00 |
