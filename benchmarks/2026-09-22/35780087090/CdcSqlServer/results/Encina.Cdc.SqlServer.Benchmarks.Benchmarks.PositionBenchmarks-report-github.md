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
| ComparePositions | 1.302 ns | 0.6345 ns | 0.0348 ns |  0.14 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 9.241 ns | 4.4615 ns | 0.2446 ns |  1.00 |    0.03 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 6.583 ns | 1.3115 ns | 0.0719 ns |  0.71 |    0.02 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 6.258 ns | 1.2555 ns | 0.0688 ns |  0.68 |    0.02 | 0.0019 |      32 B |        1.33 |
