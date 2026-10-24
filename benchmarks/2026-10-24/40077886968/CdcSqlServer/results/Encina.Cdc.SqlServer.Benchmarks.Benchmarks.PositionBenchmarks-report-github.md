```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.322 ns | 0.5286 ns | 0.0290 ns |  0.14 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 9.148 ns | 7.5691 ns | 0.4149 ns |  1.00 |    0.06 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 8.316 ns | 3.6909 ns | 0.2023 ns |  0.91 |    0.04 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 7.376 ns | 2.2681 ns | 0.1243 ns |  0.81 |    0.03 | 0.0019 |      32 B |        1.33 |
