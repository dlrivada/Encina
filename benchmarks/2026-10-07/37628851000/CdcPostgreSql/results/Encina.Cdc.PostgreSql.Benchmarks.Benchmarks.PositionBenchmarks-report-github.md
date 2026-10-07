```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  1.320 ns | 0.1054 ns | 0.0058 ns |  0.12 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 10.611 ns | 4.0149 ns | 0.2201 ns |  1.00 |    0.03 | 0.0014 |      24 B |        1.00 |
| FromBytes        |  7.128 ns | 1.3860 ns | 0.0760 ns |  0.67 |    0.01 | 0.0014 |      24 B |        1.00 |
| ToBytes          |  7.710 ns | 1.0682 ns | 0.0586 ns |  0.73 |    0.01 | 0.0019 |      32 B |        1.33 |
