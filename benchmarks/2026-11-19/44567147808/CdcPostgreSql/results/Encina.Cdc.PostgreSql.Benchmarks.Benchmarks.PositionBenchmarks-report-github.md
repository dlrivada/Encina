```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.025 ns | 0.0989 ns | 0.0054 ns |  0.19 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 5.318 ns | 3.9005 ns | 0.2138 ns |  1.00 |    0.05 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 6.315 ns | 8.3077 ns | 0.4554 ns |  1.19 |    0.09 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 6.607 ns | 3.8534 ns | 0.2112 ns |  1.24 |    0.06 | 0.0019 |      32 B |        1.33 |
