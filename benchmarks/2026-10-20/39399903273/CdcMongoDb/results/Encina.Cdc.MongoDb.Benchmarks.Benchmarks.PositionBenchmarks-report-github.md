```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error       | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|------------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 132.000 ns |  16.9431 ns | 0.9287 ns | 16.49 |    0.12 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   8.004 ns |   0.6065 ns | 0.0332 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 677.612 ns | 136.5178 ns | 7.4830 ns | 84.66 |    0.87 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 597.234 ns |  12.4690 ns | 0.6835 ns | 74.61 |    0.28 | 0.0601 |    1008 B |       42.00 |
