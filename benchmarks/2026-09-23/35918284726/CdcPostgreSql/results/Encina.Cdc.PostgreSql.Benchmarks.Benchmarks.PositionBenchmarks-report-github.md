```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.046 ns | 0.5840 ns | 0.0320 ns |  0.22 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 4.694 ns | 2.4104 ns | 0.1321 ns |  1.00 |    0.03 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 6.486 ns | 6.6259 ns | 0.3632 ns |  1.38 |    0.07 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 5.942 ns | 3.4485 ns | 0.1890 ns |  1.27 |    0.05 | 0.0019 |      32 B |        1.33 |
