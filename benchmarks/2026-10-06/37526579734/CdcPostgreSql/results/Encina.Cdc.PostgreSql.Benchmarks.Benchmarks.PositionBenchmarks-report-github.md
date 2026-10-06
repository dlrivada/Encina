```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.035 ns | 0.4650 ns | 0.0255 ns |  0.23 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 4.488 ns | 0.6230 ns | 0.0341 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 5.680 ns | 2.2404 ns | 0.1228 ns |  1.27 |    0.03 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 5.725 ns | 0.7245 ns | 0.0397 ns |  1.28 |    0.01 | 0.0019 |      32 B |        1.33 |
