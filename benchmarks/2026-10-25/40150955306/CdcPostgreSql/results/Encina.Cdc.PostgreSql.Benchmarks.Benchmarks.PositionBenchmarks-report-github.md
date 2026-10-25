```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.297 ns | 0.0026 ns | 0.0038 ns |  0.13 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 9.851 ns | 0.5996 ns | 0.8789 ns |  1.01 |    0.12 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 7.624 ns | 0.0932 ns | 0.1395 ns |  0.78 |    0.07 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 7.639 ns | 0.0728 ns | 0.1089 ns |  0.78 |    0.07 | 0.0019 |      32 B |        1.33 |
