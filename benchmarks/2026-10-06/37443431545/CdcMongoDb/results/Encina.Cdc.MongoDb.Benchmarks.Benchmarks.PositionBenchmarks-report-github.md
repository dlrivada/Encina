```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 146.196 ns |  1.420 ns | 0.0778 ns | 20.59 |    0.23 | 0.0010 |      96 B |        4.00 |
| CreatePosition   |   7.102 ns |  1.636 ns | 0.0897 ns |  1.00 |    0.02 | 0.0003 |      24 B |        1.00 |
| FromBytes        | 545.988 ns | 18.485 ns | 1.0132 ns | 76.89 |    0.85 | 0.0114 |    1024 B |       42.67 |
| ToBytes          | 518.109 ns | 62.887 ns | 3.4471 ns | 72.96 |    0.90 | 0.0114 |    1008 B |       42.00 |
