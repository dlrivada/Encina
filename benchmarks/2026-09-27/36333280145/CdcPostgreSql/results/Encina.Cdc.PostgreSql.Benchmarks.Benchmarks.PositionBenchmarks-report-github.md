```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.06GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.365 ns | 0.0388 ns | 0.0021 ns |  0.19 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 7.311 ns | 4.3429 ns | 0.2380 ns |  1.00 |    0.04 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 6.798 ns | 1.2692 ns | 0.0696 ns |  0.93 |    0.03 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 7.509 ns | 3.6975 ns | 0.2027 ns |  1.03 |    0.04 | 0.0019 |      32 B |        1.33 |
