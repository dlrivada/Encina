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
| ComparePositions | 133.908 ns | 22.722 ns | 1.2455 ns | 13.91 |    0.75 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   9.653 ns | 11.025 ns | 0.6043 ns |  1.00 |    0.08 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 601.223 ns | 82.959 ns | 4.5473 ns | 62.44 |    3.34 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 606.319 ns | 56.991 ns | 3.1239 ns | 62.97 |    3.36 | 0.0601 |    1008 B |       42.00 |
