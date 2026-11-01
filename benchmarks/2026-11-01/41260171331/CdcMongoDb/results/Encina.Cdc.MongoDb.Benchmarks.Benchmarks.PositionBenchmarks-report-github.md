```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 3.55GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method           | Mean       | Error     | StdDev    | Median     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|----------:|----------:|-----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 115.599 ns | 1.8889 ns | 2.6479 ns | 114.178 ns | 13.86 |    1.16 | 0.0011 |      96 B |        4.00 |
| CreatePosition   |   8.395 ns | 0.4647 ns | 0.6811 ns |   8.971 ns |  1.01 |    0.11 | 0.0003 |      24 B |        1.00 |
| FromBytes        | 514.805 ns | 0.9440 ns | 1.3539 ns | 514.483 ns | 61.72 |    4.96 | 0.0114 |    1024 B |       42.67 |
| ToBytes          | 498.113 ns | 1.3124 ns | 1.9643 ns | 498.138 ns | 59.72 |    4.80 | 0.0114 |    1008 B |       42.00 |
