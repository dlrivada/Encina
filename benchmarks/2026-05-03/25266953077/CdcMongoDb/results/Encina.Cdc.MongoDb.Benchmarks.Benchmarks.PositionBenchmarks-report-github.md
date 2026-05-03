```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.203
  [Host]    : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method           | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 135.414 ns | 0.2979 ns | 0.4176 ns | 15.47 |    1.45 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   8.838 ns | 0.6177 ns | 0.9245 ns |  1.01 |    0.14 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 610.441 ns | 4.1967 ns | 6.2815 ns | 69.74 |    6.57 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 542.021 ns | 2.9854 ns | 4.2816 ns | 61.92 |    5.82 | 0.0601 |    1008 B |       42.00 |
