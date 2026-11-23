```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.52GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| ComparePositions | 0.4396 ns | 0.0539 ns | 0.0030 ns |  0.15 |      - |         - |        0.00 |
| CreatePosition   | 3.0200 ns | 0.2658 ns | 0.0146 ns |  1.00 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 3.3205 ns | 0.8980 ns | 0.0492 ns |  1.10 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 3.6234 ns | 0.1051 ns | 0.0058 ns |  1.20 | 0.0019 |      32 B |        1.33 |
