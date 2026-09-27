```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.61GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.283 ns | 0.2696 ns | 0.0148 ns |  0.20 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 6.349 ns | 2.8331 ns | 0.1553 ns |  1.00 |    0.03 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 6.472 ns | 2.9747 ns | 0.1631 ns |  1.02 |    0.03 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 6.537 ns | 3.2817 ns | 0.1799 ns |  1.03 |    0.03 | 0.0019 |      32 B |        1.33 |
