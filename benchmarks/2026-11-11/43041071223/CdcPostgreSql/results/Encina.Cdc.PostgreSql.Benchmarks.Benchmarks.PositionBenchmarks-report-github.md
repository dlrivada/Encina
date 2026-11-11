```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 2.99GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  1.069 ns | 1.5360 ns | 0.0842 ns |  0.14 |    0.01 |      - |         - |        0.00 |
| CreatePosition   |  7.675 ns | 2.0311 ns | 0.1113 ns |  1.00 |    0.02 | 0.0003 |      24 B |        1.00 |
| FromBytes        |  9.026 ns | 0.8102 ns | 0.0444 ns |  1.18 |    0.02 | 0.0003 |      24 B |        1.00 |
| ToBytes          | 10.050 ns | 1.4568 ns | 0.0799 ns |  1.31 |    0.02 | 0.0004 |      32 B |        1.33 |
