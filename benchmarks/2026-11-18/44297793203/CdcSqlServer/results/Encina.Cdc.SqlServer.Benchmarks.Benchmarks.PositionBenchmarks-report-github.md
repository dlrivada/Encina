```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.66GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.086 ns | 0.8709 ns | 0.0477 ns |  0.19 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 5.634 ns | 4.2641 ns | 0.2337 ns |  1.00 |    0.05 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 6.323 ns | 3.9863 ns | 0.2185 ns |  1.12 |    0.05 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 6.048 ns | 0.3207 ns | 0.0176 ns |  1.07 |    0.04 | 0.0019 |      32 B |        1.33 |
