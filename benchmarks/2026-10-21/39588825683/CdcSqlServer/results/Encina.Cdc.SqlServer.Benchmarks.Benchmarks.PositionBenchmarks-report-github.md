```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.075 ns | 0.5404 ns | 0.0296 ns |  0.22 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 4.814 ns | 2.2998 ns | 0.1261 ns |  1.00 |    0.03 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 5.955 ns | 1.3657 ns | 0.0749 ns |  1.24 |    0.03 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 5.646 ns | 2.0479 ns | 0.1123 ns |  1.17 |    0.03 | 0.0019 |      32 B |        1.33 |
