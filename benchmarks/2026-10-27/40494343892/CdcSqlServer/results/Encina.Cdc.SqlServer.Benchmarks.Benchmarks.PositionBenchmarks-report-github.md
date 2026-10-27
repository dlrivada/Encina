```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.066 ns | 0.2731 ns | 0.0150 ns |  0.21 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 5.175 ns | 0.1406 ns | 0.0077 ns |  1.00 |    0.00 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 6.491 ns | 2.3401 ns | 0.1283 ns |  1.25 |    0.02 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 5.208 ns | 6.7811 ns | 0.3717 ns |  1.01 |    0.06 | 0.0019 |      32 B |        1.33 |
