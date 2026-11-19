```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 107.262 ns |  10.722 ns | 0.5877 ns | 15.81 |    0.25 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   6.788 ns |   2.120 ns | 0.1162 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 453.684 ns |  92.319 ns | 5.0603 ns | 66.85 |    1.19 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 445.315 ns | 151.089 ns | 8.2817 ns | 65.62 |    1.44 | 0.0601 |    1008 B |       42.00 |
