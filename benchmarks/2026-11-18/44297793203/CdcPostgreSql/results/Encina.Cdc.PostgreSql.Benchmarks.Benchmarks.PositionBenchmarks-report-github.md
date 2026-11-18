```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.48GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.8831 ns | 0.0935 ns | 0.0051 ns |  0.16 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 5.6391 ns | 1.2437 ns | 0.0682 ns |  1.00 |    0.01 | 0.0003 |      24 B |        1.00 |
| FromBytes        | 7.1396 ns | 1.3328 ns | 0.0731 ns |  1.27 |    0.02 | 0.0003 |      24 B |        1.00 |
| ToBytes          | 7.9175 ns | 2.9593 ns | 0.1622 ns |  1.40 |    0.03 | 0.0004 |      32 B |        1.33 |
