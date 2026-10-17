```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 136.403 ns | 60.804 ns | 3.3329 ns | 15.29 |    0.60 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   8.928 ns |  6.405 ns | 0.3511 ns |  1.00 |    0.05 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 656.183 ns | 46.222 ns | 2.5336 ns | 73.57 |    2.47 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 583.477 ns | 44.998 ns | 2.4665 ns | 65.42 |    2.20 | 0.0601 |    1008 B |       42.00 |
