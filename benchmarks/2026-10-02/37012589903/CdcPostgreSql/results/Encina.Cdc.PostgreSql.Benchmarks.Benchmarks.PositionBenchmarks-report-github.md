```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| ComparePositions | 1.317 ns | 0.0539 ns | 0.0030 ns |  0.13 |      - |         - |        0.00 |
| CreatePosition   | 9.932 ns | 1.6688 ns | 0.0915 ns |  1.00 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 6.992 ns | 0.8834 ns | 0.0484 ns |  0.70 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 7.990 ns | 2.7862 ns | 0.1527 ns |  0.80 | 0.0019 |      32 B |        1.33 |
