```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.27GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.5556 ns | 0.0919 ns | 0.0050 ns |  0.16 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 3.5833 ns | 4.4696 ns | 0.2450 ns |  1.00 |    0.08 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 3.8063 ns | 0.9453 ns | 0.0518 ns |  1.07 |    0.06 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 3.6233 ns | 0.5145 ns | 0.0282 ns |  1.01 |    0.06 | 0.0019 |      32 B |        1.33 |
