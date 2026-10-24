```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error       | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|------------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 111.328 ns |  17.9569 ns | 0.9843 ns | 16.73 |    0.16 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   6.656 ns |   0.8734 ns | 0.0479 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 485.787 ns |  26.0943 ns | 1.4303 ns | 72.99 |    0.49 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 475.568 ns | 158.9466 ns | 8.7124 ns | 71.45 |    1.22 | 0.0601 |    1008 B |       42.00 |
