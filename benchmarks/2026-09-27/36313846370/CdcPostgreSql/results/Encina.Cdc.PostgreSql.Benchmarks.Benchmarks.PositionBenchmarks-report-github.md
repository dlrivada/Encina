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
| ComparePositions |  1.323 ns | 0.0827 ns | 0.0045 ns |  0.13 |    0.00 |      - |         - |        0.00 |
| CreatePosition   |  9.840 ns | 2.6948 ns | 0.1477 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 18.587 ns | 8.7375 ns | 0.4789 ns |  1.89 |    0.05 | 0.0014 |      24 B |        1.00 |
| ToBytes          |  7.662 ns | 4.0913 ns | 0.2243 ns |  0.78 |    0.02 | 0.0019 |      32 B |        1.33 |
