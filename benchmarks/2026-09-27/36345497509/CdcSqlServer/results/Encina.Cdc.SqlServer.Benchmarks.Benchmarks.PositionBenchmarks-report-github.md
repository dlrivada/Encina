```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.318 ns | 0.0247 ns | 0.0014 ns |  0.20 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 6.659 ns | 4.1908 ns | 0.2297 ns |  1.00 |    0.04 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 8.015 ns | 7.3977 ns | 0.4055 ns |  1.20 |    0.06 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 6.668 ns | 0.3605 ns | 0.0198 ns |  1.00 |    0.03 | 0.0019 |      32 B |        1.33 |
