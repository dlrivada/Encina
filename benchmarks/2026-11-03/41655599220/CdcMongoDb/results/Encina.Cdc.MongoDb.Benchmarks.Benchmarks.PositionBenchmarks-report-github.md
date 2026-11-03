```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.47GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error       | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|------------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  73.738 ns |  12.3268 ns | 0.6757 ns | 16.27 |    0.17 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   4.533 ns |   0.6230 ns | 0.0342 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 328.094 ns | 115.7269 ns | 6.3434 ns | 72.38 |    1.30 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 308.038 ns |  57.6213 ns | 3.1584 ns | 67.96 |    0.75 | 0.0601 |    1008 B |       42.00 |
