```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error       | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|------------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  71.454 ns |  25.9364 ns | 1.4217 ns | 15.24 |    0.30 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   4.688 ns |   0.9570 ns | 0.0525 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 333.952 ns | 134.2884 ns | 7.3608 ns | 71.24 |    1.52 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 312.045 ns | 135.9349 ns | 7.4510 ns | 66.56 |    1.52 | 0.0601 |    1008 B |       42.00 |
