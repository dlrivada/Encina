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
| ComparePositions | 1.281 ns | 0.0822 ns | 0.0045 ns |  0.16 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 7.941 ns | 2.5707 ns | 0.1409 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 7.764 ns | 3.4044 ns | 0.1866 ns |  0.98 |    0.03 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 6.906 ns | 9.4594 ns | 0.5185 ns |  0.87 |    0.06 | 0.0019 |      32 B |        1.33 |
