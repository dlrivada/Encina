```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.62GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method           | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 133.548 ns | 0.6188 ns | 0.9070 ns | 14.29 |    1.56 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   9.462 ns | 0.7696 ns | 1.1280 ns |  1.01 |    0.16 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 629.722 ns | 3.1685 ns | 4.7425 ns | 67.40 |    7.35 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 588.814 ns | 4.5161 ns | 6.4769 ns | 63.02 |    6.89 | 0.0601 |    1008 B |       42.00 |
