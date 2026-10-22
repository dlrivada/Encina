```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  1.314 ns | 0.0060 ns | 0.0003 ns |  0.14 |    0.01 |      - |         - |        0.00 |
| CreatePosition   |  9.092 ns | 8.4214 ns | 0.4616 ns |  1.00 |    0.06 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 13.586 ns | 9.3346 ns | 0.5117 ns |  1.50 |    0.08 | 0.0014 |      24 B |        1.00 |
| ToBytes          |  7.351 ns | 3.1474 ns | 0.1725 ns |  0.81 |    0.04 | 0.0019 |      32 B |        1.33 |
