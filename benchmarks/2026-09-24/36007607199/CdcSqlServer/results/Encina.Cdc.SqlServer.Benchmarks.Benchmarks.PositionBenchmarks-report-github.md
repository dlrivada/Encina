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
| ComparePositions | 1.862 ns | 0.0328 ns | 0.0018 ns |  0.34 |    0.03 |      - |         - |        0.00 |
| CreatePosition   | 5.440 ns | 8.4581 ns | 0.4636 ns |  1.01 |    0.11 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 5.823 ns | 2.7394 ns | 0.1502 ns |  1.08 |    0.09 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 6.021 ns | 1.4869 ns | 0.0815 ns |  1.11 |    0.09 | 0.0019 |      32 B |        1.33 |
