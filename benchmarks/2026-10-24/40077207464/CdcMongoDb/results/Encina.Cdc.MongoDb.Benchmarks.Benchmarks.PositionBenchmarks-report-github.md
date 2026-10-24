```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  72.018 ns | 18.2348 ns | 0.9995 ns | 14.60 |    0.19 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   4.933 ns |  0.5536 ns | 0.0303 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 337.566 ns | 18.5927 ns | 1.0191 ns | 68.43 |    0.41 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 327.121 ns | 57.4813 ns | 3.1507 ns | 66.31 |    0.66 | 0.0601 |    1008 B |       42.00 |
