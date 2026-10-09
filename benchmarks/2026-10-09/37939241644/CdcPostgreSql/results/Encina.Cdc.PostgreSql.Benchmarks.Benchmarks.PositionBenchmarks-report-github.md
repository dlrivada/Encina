```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.340 ns | 0.1082 ns | 0.0059 ns |  0.22 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 5.992 ns | 3.4119 ns | 0.1870 ns |  1.00 |    0.04 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 6.275 ns | 0.4680 ns | 0.0257 ns |  1.05 |    0.03 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 7.354 ns | 4.3861 ns | 0.2404 ns |  1.23 |    0.05 | 0.0019 |      32 B |        1.33 |
