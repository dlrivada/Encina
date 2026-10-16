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
| ComparePositions | 1.285 ns | 0.1052 ns | 0.0058 ns |  0.14 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 9.460 ns | 9.5733 ns | 0.5247 ns |  1.00 |    0.07 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 7.068 ns | 0.8197 ns | 0.0449 ns |  0.75 |    0.04 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 6.882 ns | 0.9110 ns | 0.0499 ns |  0.73 |    0.03 | 0.0019 |      32 B |        1.33 |
