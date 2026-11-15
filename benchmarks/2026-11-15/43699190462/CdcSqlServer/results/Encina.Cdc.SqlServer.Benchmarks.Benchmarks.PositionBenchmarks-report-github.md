```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.52GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.4481 ns | 0.2812 ns | 0.0154 ns |  0.11 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 3.9132 ns | 2.5445 ns | 0.1395 ns |  1.00 |    0.04 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 3.9007 ns | 3.2163 ns | 0.1763 ns |  1.00 |    0.05 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 3.5939 ns | 2.4433 ns | 0.1339 ns |  0.92 |    0.04 | 0.0019 |      32 B |        1.33 |
