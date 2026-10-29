```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.321 ns | 0.0706 ns | 0.0039 ns |  0.13 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 9.830 ns | 2.5379 ns | 0.1391 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 6.871 ns | 0.9236 ns | 0.0506 ns |  0.70 |    0.01 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 6.856 ns | 0.5416 ns | 0.0297 ns |  0.70 |    0.01 | 0.0019 |      32 B |        1.33 |
