```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.075 ns | 0.4538 ns | 0.0249 ns |  0.21 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 5.208 ns | 0.5556 ns | 0.0305 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 6.723 ns | 2.0740 ns | 0.1137 ns |  1.29 |    0.02 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 5.623 ns | 4.4677 ns | 0.2449 ns |  1.08 |    0.04 | 0.0019 |      32 B |        1.33 |
