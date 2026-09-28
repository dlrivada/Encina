```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 113.374 ns | 13.990 ns | 0.7668 ns | 17.66 |    0.19 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   6.422 ns |  1.201 ns | 0.0658 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 474.327 ns | 49.988 ns | 2.7400 ns | 73.87 |    0.75 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 431.883 ns | 20.957 ns | 1.1487 ns | 67.26 |    0.61 | 0.0601 |    1008 B |       42.00 |
