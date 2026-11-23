```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.67GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.063 ns | 0.2170 ns | 0.0119 ns |  0.20 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 5.239 ns | 1.5418 ns | 0.0845 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 6.037 ns | 1.8716 ns | 0.1026 ns |  1.15 |    0.02 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 5.956 ns | 0.9054 ns | 0.0496 ns |  1.14 |    0.02 | 0.0019 |      32 B |        1.33 |
