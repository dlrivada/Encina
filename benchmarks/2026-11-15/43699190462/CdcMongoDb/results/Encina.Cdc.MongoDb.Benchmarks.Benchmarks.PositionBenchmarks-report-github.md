```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.55GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error       | StdDev     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|------------:|-----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  68.845 ns |  16.0350 ns |  0.8789 ns | 17.39 |    0.21 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   3.959 ns |   0.4375 ns |  0.0240 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 319.996 ns |  89.7007 ns |  4.9168 ns | 80.83 |    1.16 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 299.292 ns | 186.1628 ns | 10.2042 ns | 75.60 |    2.27 | 0.0601 |    1008 B |       42.00 |
