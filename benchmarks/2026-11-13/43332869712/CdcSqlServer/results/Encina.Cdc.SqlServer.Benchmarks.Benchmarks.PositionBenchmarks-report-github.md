```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.26GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.4823 ns | 0.0463 ns | 0.0025 ns |  0.13 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 3.7465 ns | 1.9494 ns | 0.1069 ns |  1.00 |    0.03 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 3.6685 ns | 0.2334 ns | 0.0128 ns |  0.98 |    0.02 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 3.8686 ns | 3.2960 ns | 0.1807 ns |  1.03 |    0.05 | 0.0019 |      32 B |        1.33 |
