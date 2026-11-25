```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 107.632 ns |  5.100 ns | 0.2795 ns | 15.40 |    0.41 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   6.992 ns |  4.012 ns | 0.2199 ns |  1.00 |    0.04 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 476.773 ns | 47.761 ns | 2.6180 ns | 68.23 |    1.85 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 472.801 ns | 23.173 ns | 1.2702 ns | 67.66 |    1.82 | 0.0601 |    1008 B |       42.00 |
