```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.75GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 135.246 ns |  7.167 ns | 0.3928 ns | 15.15 |    0.22 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   8.932 ns |  2.763 ns | 0.1515 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 624.810 ns | 94.682 ns | 5.1898 ns | 69.97 |    1.14 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 571.793 ns | 75.004 ns | 4.1112 ns | 64.03 |    1.01 | 0.0601 |    1008 B |       42.00 |
