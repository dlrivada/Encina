```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.75GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 134.583 ns | 15.253 ns | 0.8361 ns | 15.95 |    0.30 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   8.442 ns |  3.226 ns | 0.1769 ns |  1.00 |    0.03 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 617.885 ns | 63.608 ns | 3.4866 ns | 73.21 |    1.36 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 586.277 ns | 19.761 ns | 1.0831 ns | 69.46 |    1.25 | 0.0601 |    1008 B |       42.00 |
