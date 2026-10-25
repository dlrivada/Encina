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
| ComparePositions | 1.283 ns | 0.1830 ns | 0.0100 ns |  0.15 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 8.579 ns | 6.1740 ns | 0.3384 ns |  1.00 |    0.05 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 7.854 ns | 2.0255 ns | 0.1110 ns |  0.92 |    0.03 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 7.052 ns | 1.5366 ns | 0.0842 ns |  0.82 |    0.03 | 0.0019 |      32 B |        1.33 |
