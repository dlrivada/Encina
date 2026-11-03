```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.23GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.315 ns | 0.0696 ns | 0.0038 ns |  0.20 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 6.525 ns | 0.8357 ns | 0.0458 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 7.315 ns | 3.4998 ns | 0.1918 ns |  1.12 |    0.03 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 7.690 ns | 4.0157 ns | 0.2201 ns |  1.18 |    0.03 | 0.0019 |      32 B |        1.33 |
