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
| ComparePositions | 1.635 ns | 0.0256 ns | 0.0014 ns |  0.18 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 9.208 ns | 8.4475 ns | 0.4630 ns |  1.00 |    0.06 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 6.970 ns | 1.4321 ns | 0.0785 ns |  0.76 |    0.03 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 6.247 ns | 1.7752 ns | 0.0973 ns |  0.68 |    0.03 | 0.0019 |      32 B |        1.33 |
