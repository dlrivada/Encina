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
| ComparePositions |  1.321 ns | 0.0366 ns | 0.0020 ns |  0.12 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 10.676 ns | 5.6865 ns | 0.3117 ns |  1.00 |    0.04 | 0.0014 |      24 B |        1.00 |
| FromBytes        |  7.135 ns | 0.5545 ns | 0.0304 ns |  0.67 |    0.02 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 17.445 ns | 0.9641 ns | 0.0528 ns |  1.64 |    0.04 | 0.0019 |      32 B |        1.33 |
