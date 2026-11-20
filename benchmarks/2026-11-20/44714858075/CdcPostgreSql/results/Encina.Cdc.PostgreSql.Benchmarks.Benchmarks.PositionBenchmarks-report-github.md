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
| ComparePositions | 1.023 ns | 0.0620 ns | 0.0034 ns |  0.20 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 5.011 ns | 2.3560 ns | 0.1291 ns |  1.00 |    0.03 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 6.413 ns | 4.9506 ns | 0.2714 ns |  1.28 |    0.05 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 5.970 ns | 0.3669 ns | 0.0201 ns |  1.19 |    0.03 | 0.0019 |      32 B |        1.33 |
