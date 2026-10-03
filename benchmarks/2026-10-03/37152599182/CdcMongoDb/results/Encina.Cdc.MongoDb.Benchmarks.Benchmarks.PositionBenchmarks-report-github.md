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
| ComparePositions | 134.561 ns |  7.145 ns | 0.3916 ns | 16.37 |    0.18 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   8.220 ns |  1.904 ns | 0.1044 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 577.570 ns | 84.689 ns | 4.6421 ns | 70.27 |    0.91 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 542.863 ns | 57.917 ns | 3.1746 ns | 66.05 |    0.79 | 0.0601 |    1008 B |       42.00 |
