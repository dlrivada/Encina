```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.48GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  68.627 ns |  7.8550 ns | 0.4306 ns | 16.67 |    0.12 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   4.118 ns |  0.3777 ns | 0.0207 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 322.444 ns | 66.3045 ns | 3.6344 ns | 78.31 |    0.84 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 300.953 ns | 51.5382 ns | 2.8250 ns | 73.09 |    0.67 | 0.0601 |    1008 B |       42.00 |
