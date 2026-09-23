```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 134.417 ns | 12.321 ns | 0.6753 ns | 13.71 |    0.16 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   9.806 ns |  2.253 ns | 0.1235 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 605.860 ns | 52.352 ns | 2.8696 ns | 61.79 |    0.72 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 610.360 ns | 29.569 ns | 1.6208 ns | 62.25 |    0.70 | 0.0601 |    1008 B |       42.00 |
