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
| ComparePositions |  1.274 ns | 0.0227 ns | 0.0012 ns |  0.14 |    0.01 |      - |         - |        0.00 |
| CreatePosition   |  9.213 ns | 9.7889 ns | 0.5366 ns |  1.00 |    0.07 | 0.0014 |      24 B |        1.00 |
| FromBytes        |  7.084 ns | 2.6679 ns | 0.1462 ns |  0.77 |    0.04 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 19.635 ns | 8.3351 ns | 0.4569 ns |  2.14 |    0.12 | 0.0019 |      32 B |        1.33 |
