```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.66GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.311 ns | 0.0659 ns | 0.0036 ns |  0.20 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 6.479 ns | 2.4859 ns | 0.1363 ns |  1.00 |    0.03 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 7.011 ns | 2.0453 ns | 0.1121 ns |  1.08 |    0.02 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 7.639 ns | 4.8265 ns | 0.2646 ns |  1.18 |    0.04 | 0.0019 |      32 B |        1.33 |
