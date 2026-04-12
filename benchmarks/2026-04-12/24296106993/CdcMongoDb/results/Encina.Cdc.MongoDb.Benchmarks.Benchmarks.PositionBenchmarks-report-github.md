```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method           | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 134.446 ns | 0.7037 ns | 1.0092 ns | 16.65 |    0.22 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   8.075 ns | 0.0635 ns | 0.0891 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 577.742 ns | 0.9923 ns | 1.3583 ns | 71.56 |    0.81 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 542.594 ns | 6.7708 ns | 9.7105 ns | 67.21 |    1.39 | 0.0601 |    1008 B |       42.00 |
