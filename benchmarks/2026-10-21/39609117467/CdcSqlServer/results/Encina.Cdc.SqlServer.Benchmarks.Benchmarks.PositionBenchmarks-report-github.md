```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.062 ns | 0.0262 ns | 0.0014 ns |  0.22 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 4.728 ns | 3.1086 ns | 0.1704 ns |  1.00 |    0.04 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 6.427 ns | 3.8361 ns | 0.2103 ns |  1.36 |    0.06 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 5.302 ns | 0.3682 ns | 0.0202 ns |  1.12 |    0.04 | 0.0019 |      32 B |        1.33 |
