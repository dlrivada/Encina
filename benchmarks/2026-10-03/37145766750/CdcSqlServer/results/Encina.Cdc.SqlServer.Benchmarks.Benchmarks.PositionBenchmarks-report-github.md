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
| ComparePositions | 1.091 ns | 0.0963 ns | 0.0053 ns |  0.24 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 4.591 ns | 5.0326 ns | 0.2759 ns |  1.00 |    0.07 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 5.787 ns | 2.4153 ns | 0.1324 ns |  1.26 |    0.07 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 5.545 ns | 5.5544 ns | 0.3045 ns |  1.21 |    0.08 | 0.0019 |      32 B |        1.33 |
