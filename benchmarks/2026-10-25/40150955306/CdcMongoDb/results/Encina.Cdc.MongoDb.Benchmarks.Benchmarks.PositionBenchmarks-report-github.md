```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method           | Mean       | Error      | StdDev     | Median     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|-----------:|-----------:|-----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 134.220 ns |  0.3948 ns |  0.5787 ns | 134.213 ns | 14.01 |    0.95 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   9.626 ns |  0.4576 ns |  0.6849 ns |   9.562 ns |  1.00 |    0.10 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 653.099 ns |  8.7056 ns | 13.0302 ns | 649.627 ns | 68.17 |    4.79 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 634.387 ns | 26.1993 ns | 38.4026 ns | 605.928 ns | 66.22 |    5.97 | 0.0601 |    1008 B |       42.00 |
