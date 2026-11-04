```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 137.673 ns |  14.349 ns | 0.7865 ns | 16.22 |    0.13 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   8.489 ns |   1.112 ns | 0.0609 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 670.548 ns | 117.115 ns | 6.4194 ns | 79.00 |    0.82 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 592.614 ns |  73.230 ns | 4.0140 ns | 69.82 |    0.60 | 0.0601 |    1008 B |       42.00 |
