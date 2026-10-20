```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.25GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  77.601 ns |  98.516 ns | 5.4000 ns | 16.28 |    1.02 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   4.769 ns |   1.782 ns | 0.0977 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 335.465 ns |  90.088 ns | 4.9380 ns | 70.36 |    1.53 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 330.050 ns | 129.867 ns | 7.1184 ns | 69.22 |    1.77 | 0.0601 |    1008 B |       42.00 |
