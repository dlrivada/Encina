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
| ComparePositions | 1.278 ns | 0.1319 ns | 0.0072 ns |  0.17 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 7.566 ns | 0.4517 ns | 0.0248 ns |  1.00 |    0.00 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 7.680 ns | 1.5735 ns | 0.0862 ns |  1.02 |    0.01 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 6.875 ns | 3.0006 ns | 0.1645 ns |  0.91 |    0.02 | 0.0019 |      32 B |        1.33 |
