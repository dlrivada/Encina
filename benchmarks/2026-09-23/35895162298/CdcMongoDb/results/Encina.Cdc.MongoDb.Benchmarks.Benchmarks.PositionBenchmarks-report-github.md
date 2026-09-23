```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error      | StdDev     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|-----------:|-----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 110.377 ns |  66.038 ns |  3.6197 ns | 17.00 |    0.63 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   6.496 ns |   3.263 ns |  0.1788 ns |  1.00 |    0.03 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 460.174 ns |  87.498 ns |  4.7961 ns | 70.88 |    1.83 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 439.882 ns | 200.440 ns | 10.9868 ns | 67.75 |    2.20 | 0.0601 |    1008 B |       42.00 |
