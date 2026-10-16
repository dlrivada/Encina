```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  76.982 ns |  9.908 ns | 0.5431 ns | 15.70 |    0.35 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   4.906 ns |  2.181 ns | 0.1196 ns |  1.00 |    0.03 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 344.717 ns | 70.834 ns | 3.8826 ns | 70.29 |    1.64 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 314.263 ns | 64.676 ns | 3.5451 ns | 64.08 |    1.50 | 0.0601 |    1008 B |       42.00 |
