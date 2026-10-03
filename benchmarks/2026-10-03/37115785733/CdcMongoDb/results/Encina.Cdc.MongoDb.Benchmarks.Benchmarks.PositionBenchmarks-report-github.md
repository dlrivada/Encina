```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error       | StdDev     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|------------:|-----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  67.847 ns |   8.7325 ns |  0.4787 ns | 16.86 |    0.12 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   4.024 ns |   0.2766 ns |  0.0152 ns |  1.00 |    0.00 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 333.466 ns | 232.2134 ns | 12.7284 ns | 82.88 |    2.75 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 289.849 ns | 188.2574 ns | 10.3190 ns | 72.04 |    2.23 | 0.0601 |    1008 B |       42.00 |
