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
| ComparePositions |  1.312 ns | 0.1565 ns | 0.0086 ns |  0.12 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 10.573 ns | 7.6012 ns | 0.4166 ns |  1.00 |    0.05 | 0.0014 |      24 B |        1.00 |
| FromBytes        |  5.891 ns | 0.3515 ns | 0.0193 ns |  0.56 |    0.02 | 0.0014 |      24 B |        1.00 |
| ToBytes          |  6.702 ns | 1.3043 ns | 0.0715 ns |  0.63 |    0.02 | 0.0019 |      32 B |        1.33 |
