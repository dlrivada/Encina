```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 139.037 ns |   3.241 ns | 0.1777 ns | 15.95 |    0.25 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   8.721 ns |   2.897 ns | 0.1588 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 641.806 ns | 143.104 ns | 7.8440 ns | 73.61 |    1.40 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 600.242 ns | 123.205 ns | 6.7533 ns | 68.84 |    1.28 | 0.0601 |    1008 B |       42.00 |
