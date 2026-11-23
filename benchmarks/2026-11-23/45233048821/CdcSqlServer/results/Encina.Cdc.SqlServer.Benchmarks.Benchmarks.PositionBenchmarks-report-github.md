```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.72GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  1.343 ns | 0.0290 ns | 0.0016 ns |  0.21 |    0.00 |      - |         - |        0.00 |
| CreatePosition   |  6.252 ns | 0.8778 ns | 0.0481 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 13.662 ns | 3.0519 ns | 0.1673 ns |  2.19 |    0.03 | 0.0014 |      24 B |        1.00 |
| ToBytes          |  6.682 ns | 2.6495 ns | 0.1452 ns |  1.07 |    0.02 | 0.0019 |      32 B |        1.33 |
