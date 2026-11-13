```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 2.86GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error    | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|---------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.366 ns | 1.110 ns | 0.0609 ns |  0.21 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 6.571 ns | 4.249 ns | 0.2329 ns |  1.00 |    0.04 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 8.030 ns | 7.868 ns | 0.4313 ns |  1.22 |    0.07 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 6.693 ns | 2.061 ns | 0.1130 ns |  1.02 |    0.03 | 0.0019 |      32 B |        1.33 |
