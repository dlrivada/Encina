```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.51GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  71.293 ns |  2.497 ns | 0.1369 ns | 16.66 |    0.45 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   4.282 ns |  2.461 ns | 0.1349 ns |  1.00 |    0.04 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 310.220 ns | 30.139 ns | 1.6520 ns | 72.50 |    1.98 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 300.020 ns | 47.579 ns | 2.6080 ns | 70.12 |    1.96 | 0.0601 |    1008 B |       42.00 |
