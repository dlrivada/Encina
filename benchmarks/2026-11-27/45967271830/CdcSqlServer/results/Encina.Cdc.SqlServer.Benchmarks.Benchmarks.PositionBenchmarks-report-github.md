```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.296 ns | 0.0597 ns | 0.0033 ns |  0.19 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 6.719 ns | 1.4537 ns | 0.0797 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 8.673 ns | 8.0092 ns | 0.4390 ns |  1.29 |    0.06 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 6.708 ns | 0.9441 ns | 0.0518 ns |  1.00 |    0.01 | 0.0019 |      32 B |        1.33 |
