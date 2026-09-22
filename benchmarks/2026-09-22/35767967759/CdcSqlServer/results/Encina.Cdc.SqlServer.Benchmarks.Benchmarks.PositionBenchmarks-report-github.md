```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.298 ns | 0.4839 ns | 0.0265 ns |  0.15 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 8.894 ns | 6.8868 ns | 0.3775 ns |  1.00 |    0.05 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 6.592 ns | 1.7875 ns | 0.0980 ns |  0.74 |    0.03 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 6.076 ns | 1.5519 ns | 0.0851 ns |  0.68 |    0.03 | 0.0019 |      32 B |        1.33 |
