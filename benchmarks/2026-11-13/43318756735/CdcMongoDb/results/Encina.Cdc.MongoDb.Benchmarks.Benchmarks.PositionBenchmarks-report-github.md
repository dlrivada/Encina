```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.23GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 137.943 ns |  22.268 ns | 1.2206 ns | 16.40 |    0.38 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   8.414 ns |   3.939 ns | 0.2159 ns |  1.00 |    0.03 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 609.112 ns | 132.819 ns | 7.2803 ns | 72.42 |    1.75 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 579.506 ns |  66.069 ns | 3.6215 ns | 68.90 |    1.55 | 0.0601 |    1008 B |       42.00 |
