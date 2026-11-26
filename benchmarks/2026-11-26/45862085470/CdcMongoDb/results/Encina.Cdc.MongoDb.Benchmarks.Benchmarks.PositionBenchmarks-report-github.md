```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error      | StdDev     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|-----------:|-----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 104.130 ns |  14.753 ns |  0.8086 ns | 13.89 |    0.32 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   7.503 ns |   3.421 ns |  0.1875 ns |  1.00 |    0.03 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 462.313 ns |  61.447 ns |  3.3681 ns | 61.65 |    1.41 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 456.828 ns | 202.781 ns | 11.1151 ns | 60.92 |    1.85 | 0.0601 |    1008 B |       42.00 |
