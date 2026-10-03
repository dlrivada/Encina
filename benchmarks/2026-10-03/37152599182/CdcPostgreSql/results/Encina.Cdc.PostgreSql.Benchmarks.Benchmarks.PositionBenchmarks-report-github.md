```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.088 ns |  0.1879 ns | 0.0103 ns |  0.19 |    0.02 |      - |         - |        0.00 |
| CreatePosition   | 5.815 ns | 11.6126 ns | 0.6365 ns |  1.01 |    0.14 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 5.832 ns |  4.3950 ns | 0.2409 ns |  1.01 |    0.11 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 6.272 ns |  2.7450 ns | 0.1505 ns |  1.09 |    0.11 | 0.0019 |      32 B |        1.33 |
