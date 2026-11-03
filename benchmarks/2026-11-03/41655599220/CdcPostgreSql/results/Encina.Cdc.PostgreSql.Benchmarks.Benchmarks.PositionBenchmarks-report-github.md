```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.67GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.032 ns | 0.1222 ns | 0.0067 ns |  0.20 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 5.045 ns | 3.8085 ns | 0.2088 ns |  1.00 |    0.05 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 5.964 ns | 5.7294 ns | 0.3140 ns |  1.18 |    0.07 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 6.421 ns | 2.5101 ns | 0.1376 ns |  1.27 |    0.05 | 0.0019 |      32 B |        1.33 |
