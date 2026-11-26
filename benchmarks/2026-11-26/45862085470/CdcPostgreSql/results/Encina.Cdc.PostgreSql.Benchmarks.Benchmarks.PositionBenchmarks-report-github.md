```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.16GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.4838 ns | 0.2247 ns | 0.0123 ns |  0.13 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 3.6508 ns | 8.5025 ns | 0.4661 ns |  1.01 |    0.15 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 3.7382 ns | 1.7802 ns | 0.0976 ns |  1.03 |    0.11 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 3.9673 ns | 1.7119 ns | 0.0938 ns |  1.10 |    0.12 | 0.0019 |      32 B |        1.33 |
