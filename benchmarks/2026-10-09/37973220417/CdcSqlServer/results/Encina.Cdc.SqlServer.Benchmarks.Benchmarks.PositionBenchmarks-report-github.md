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
| ComparePositions | 1.275 ns | 0.0031 ns | 0.0002 ns |  0.14 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 9.046 ns | 6.9941 ns | 0.3834 ns |  1.00 |    0.05 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 7.232 ns | 0.7628 ns | 0.0418 ns |  0.80 |    0.03 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 6.722 ns | 2.7615 ns | 0.1514 ns |  0.74 |    0.03 | 0.0019 |      32 B |        1.33 |
