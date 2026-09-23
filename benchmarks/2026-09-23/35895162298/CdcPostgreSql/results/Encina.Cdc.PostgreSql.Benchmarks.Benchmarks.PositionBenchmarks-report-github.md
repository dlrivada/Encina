```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  1.326 ns | 0.2587 ns | 0.0142 ns |  0.13 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 10.293 ns | 2.6383 ns | 0.1446 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes        |  7.319 ns | 1.2828 ns | 0.0703 ns |  0.71 |    0.01 | 0.0014 |      24 B |        1.00 |
| ToBytes          |  8.219 ns | 1.3343 ns | 0.0731 ns |  0.80 |    0.01 | 0.0019 |      32 B |        1.33 |
