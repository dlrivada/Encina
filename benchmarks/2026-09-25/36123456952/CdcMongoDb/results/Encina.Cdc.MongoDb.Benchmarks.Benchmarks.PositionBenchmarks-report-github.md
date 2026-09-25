```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 109.334 ns | 10.443 ns | 0.5724 ns | 18.20 |    0.29 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   6.008 ns |  1.983 ns | 0.1087 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 444.297 ns | 93.973 ns | 5.1510 ns | 73.97 |    1.37 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 430.808 ns | 25.383 ns | 1.3913 ns | 71.72 |    1.13 | 0.0601 |    1008 B |       42.00 |
