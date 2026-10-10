```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error      | StdDev     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|-----------:|-----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  72.667 ns |  65.600 ns |  3.5958 ns | 16.32 |    0.93 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   4.459 ns |   3.484 ns |  0.1910 ns |  1.00 |    0.05 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 340.210 ns |  78.132 ns |  4.2827 ns | 76.39 |    3.02 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 312.639 ns | 325.777 ns | 17.8569 ns | 70.20 |    4.38 | 0.0601 |    1008 B |       42.00 |
