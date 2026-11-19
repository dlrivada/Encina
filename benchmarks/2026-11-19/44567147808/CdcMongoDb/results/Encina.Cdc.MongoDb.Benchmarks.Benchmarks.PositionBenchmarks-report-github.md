```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.21GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error      | StdDev     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|-----------:|-----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  80.019 ns |  36.280 ns |  1.9886 ns | 16.28 |    0.68 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   4.920 ns |   3.646 ns |  0.1998 ns |  1.00 |    0.05 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 376.147 ns | 143.427 ns |  7.8617 ns | 76.54 |    3.05 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 387.013 ns | 577.933 ns | 31.6784 ns | 78.75 |    6.25 | 0.0601 |    1008 B |       42.00 |
