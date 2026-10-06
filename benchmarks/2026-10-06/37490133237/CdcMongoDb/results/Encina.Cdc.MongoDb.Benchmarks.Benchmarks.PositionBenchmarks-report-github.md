```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 134.726 ns |  4.601 ns | 0.2522 ns | 16.78 |    1.66 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   8.097 ns | 16.078 ns | 0.8813 ns |  1.01 |    0.14 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 573.418 ns | 37.318 ns | 2.0455 ns | 71.41 |    7.07 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 529.638 ns | 41.028 ns | 2.2489 ns | 65.96 |    6.53 | 0.0601 |    1008 B |       42.00 |
