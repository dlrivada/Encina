```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.65GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.017 ns | 0.0391 ns | 0.0021 ns |  0.21 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 4.765 ns | 0.5288 ns | 0.0290 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 5.880 ns | 4.9234 ns | 0.2699 ns |  1.23 |    0.05 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 5.713 ns | 1.1579 ns | 0.0635 ns |  1.20 |    0.01 | 0.0019 |      32 B |        1.33 |
