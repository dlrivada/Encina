```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 109.130 ns | 38.261 ns | 2.0972 ns | 14.94 |    0.58 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   7.313 ns |  5.519 ns | 0.3025 ns |  1.00 |    0.05 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 482.335 ns | 35.975 ns | 1.9719 ns | 66.03 |    2.32 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 483.928 ns | 75.070 ns | 4.1148 ns | 66.25 |    2.37 | 0.0601 |    1008 B |       42.00 |
