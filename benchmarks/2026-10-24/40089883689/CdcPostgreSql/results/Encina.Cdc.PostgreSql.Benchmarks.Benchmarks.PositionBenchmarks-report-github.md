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
| ComparePositions | 1.317 ns | 0.0807 ns | 0.0044 ns |  0.15 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 8.682 ns | 3.4125 ns | 0.1870 ns |  1.00 |    0.03 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 7.388 ns | 1.0039 ns | 0.0550 ns |  0.85 |    0.02 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 7.757 ns | 0.8679 ns | 0.0476 ns |  0.89 |    0.02 | 0.0019 |      32 B |        1.33 |
