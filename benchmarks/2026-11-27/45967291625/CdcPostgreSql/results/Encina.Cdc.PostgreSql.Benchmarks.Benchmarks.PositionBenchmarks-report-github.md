```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.28GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.4821 ns | 0.3298 ns | 0.0181 ns |  0.13 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 3.5732 ns | 1.1396 ns | 0.0625 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 4.0917 ns | 0.5861 ns | 0.0321 ns |  1.15 |    0.02 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 4.1612 ns | 1.2301 ns | 0.0674 ns |  1.16 |    0.02 | 0.0019 |      32 B |        1.33 |
