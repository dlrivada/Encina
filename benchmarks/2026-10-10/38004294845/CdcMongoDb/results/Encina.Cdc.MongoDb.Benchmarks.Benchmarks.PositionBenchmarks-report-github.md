```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.10GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 136.246 ns | 11.820 ns | 0.6479 ns | 18.93 |    0.48 | 0.0010 |      96 B |        4.00 |
| CreatePosition   |   7.200 ns |  3.817 ns | 0.2092 ns |  1.00 |    0.04 | 0.0003 |      24 B |        1.00 |
| FromBytes        | 568.262 ns | 40.750 ns | 2.2337 ns | 78.97 |    1.98 | 0.0114 |    1024 B |       42.67 |
| ToBytes          | 542.077 ns | 60.407 ns | 3.3111 ns | 75.33 |    1.91 | 0.0114 |    1008 B |       42.00 |
