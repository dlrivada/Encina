```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.013 ns | 0.0911 ns | 0.0050 ns |  0.15 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 6.616 ns | 3.4849 ns | 0.1910 ns |  1.00 |    0.04 | 0.0003 |      24 B |        1.00 |
| FromBytes        | 8.434 ns | 2.1952 ns | 0.1203 ns |  1.28 |    0.04 | 0.0003 |      24 B |        1.00 |
| ToBytes          | 8.200 ns | 2.6051 ns | 0.1428 ns |  1.24 |    0.04 | 0.0004 |      32 B |        1.33 |
