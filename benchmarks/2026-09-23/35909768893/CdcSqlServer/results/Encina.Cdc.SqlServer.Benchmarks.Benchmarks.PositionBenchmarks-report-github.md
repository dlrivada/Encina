```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.04GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.273 ns | 0.0076 ns | 0.0004 ns |  0.18 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 7.049 ns | 1.6220 ns | 0.0889 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 7.224 ns | 1.5549 ns | 0.0852 ns |  1.02 |    0.02 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 6.760 ns | 3.4715 ns | 0.1903 ns |  0.96 |    0.03 | 0.0019 |      32 B |        1.33 |
