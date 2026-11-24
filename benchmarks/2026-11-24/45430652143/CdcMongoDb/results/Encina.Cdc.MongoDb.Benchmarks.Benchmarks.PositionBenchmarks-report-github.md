```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 2.86GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error      | StdDev     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|-----------:|-----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 136.145 ns |  25.163 ns |  1.3793 ns | 15.81 |    0.43 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   8.618 ns |   4.760 ns |  0.2609 ns |  1.00 |    0.04 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 604.859 ns |  42.135 ns |  2.3096 ns | 70.23 |    1.84 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 591.426 ns | 342.892 ns | 18.7951 ns | 68.67 |    2.60 | 0.0601 |    1008 B |       42.00 |
