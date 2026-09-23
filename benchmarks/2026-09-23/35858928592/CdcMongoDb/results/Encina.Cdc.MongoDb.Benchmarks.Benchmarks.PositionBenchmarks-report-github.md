```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  73.283 ns | 24.113 ns | 1.3217 ns | 16.64 |    0.33 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   4.405 ns |  1.093 ns | 0.0599 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 348.297 ns | 76.224 ns | 4.1781 ns | 79.08 |    1.25 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 296.499 ns | 81.746 ns | 4.4807 ns | 67.32 |    1.19 | 0.0601 |    1008 B |       42.00 |
