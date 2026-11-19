```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.01GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 137.925 ns | 19.610 ns | 1.0749 ns | 16.28 |    0.30 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   8.474 ns |  3.125 ns | 0.1713 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 608.769 ns | 61.901 ns | 3.3930 ns | 71.86 |    1.29 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 587.407 ns | 63.354 ns | 3.4727 ns | 69.34 |    1.25 | 0.0601 |    1008 B |       42.00 |
