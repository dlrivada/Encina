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
| ComparePositions | 1.296 ns | 0.6722 ns | 0.0368 ns |  0.15 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 8.388 ns | 8.2767 ns | 0.4537 ns |  1.00 |    0.07 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 7.370 ns | 1.7538 ns | 0.0961 ns |  0.88 |    0.04 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 6.360 ns | 1.6277 ns | 0.0892 ns |  0.76 |    0.04 | 0.0019 |      32 B |        1.33 |
