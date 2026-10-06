```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  66.019 ns |  3.7258 ns | 0.2042 ns | 17.02 |    0.07 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   3.879 ns |  0.2811 ns | 0.0154 ns |  1.00 |    0.00 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 305.219 ns | 19.3595 ns | 1.0612 ns | 78.69 |    0.36 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 271.646 ns | 30.0327 ns | 1.6462 ns | 70.04 |    0.44 | 0.0601 |    1008 B |       42.00 |
