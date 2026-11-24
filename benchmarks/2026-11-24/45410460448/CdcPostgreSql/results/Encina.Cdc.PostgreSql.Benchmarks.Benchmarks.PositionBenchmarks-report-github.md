```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 2.99GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.040 ns | 0.5531 ns | 0.0303 ns |  0.19 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 5.594 ns | 2.8383 ns | 0.1556 ns |  1.00 |    0.03 | 0.0003 |      24 B |        1.00 |
| FromBytes        | 7.124 ns | 6.2331 ns | 0.3417 ns |  1.27 |    0.06 | 0.0003 |      24 B |        1.00 |
| ToBytes          | 7.711 ns | 0.6586 ns | 0.0361 ns |  1.38 |    0.03 | 0.0004 |      32 B |        1.33 |
