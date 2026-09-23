```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error      | StdDev     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|-----------:|-----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 101.779 ns |   2.338 ns |  0.1282 ns | 13.71 |    0.36 | 0.0011 |      96 B |        4.00 |
| CreatePosition   |   7.428 ns |   4.102 ns |  0.2248 ns |  1.00 |    0.04 | 0.0003 |      24 B |        1.00 |
| FromBytes        | 415.201 ns |  89.036 ns |  4.8803 ns | 55.93 |    1.56 | 0.0119 |    1024 B |       42.67 |
| ToBytes          | 431.633 ns | 433.869 ns | 23.7818 ns | 58.14 |    3.16 | 0.0119 |    1008 B |       42.00 |
