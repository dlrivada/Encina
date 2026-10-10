```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 106.934 ns | 16.138 ns | 0.8846 ns | 17.32 |    0.27 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   6.173 ns |  1.801 ns | 0.0987 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 434.155 ns | 29.156 ns | 1.5981 ns | 70.34 |    0.99 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 419.101 ns | 15.493 ns | 0.8492 ns | 67.90 |    0.94 | 0.0601 |    1008 B |       42.00 |
