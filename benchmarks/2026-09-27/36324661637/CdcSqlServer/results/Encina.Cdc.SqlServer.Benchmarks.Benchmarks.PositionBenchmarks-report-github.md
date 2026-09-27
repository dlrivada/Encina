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
| ComparePositions | 1.208 ns | 0.0160 ns | 0.0009 ns |  0.28 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 4.251 ns | 2.3352 ns | 0.1280 ns |  1.00 |    0.04 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 6.559 ns | 6.2796 ns | 0.3442 ns |  1.54 |    0.08 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 5.123 ns | 2.5616 ns | 0.1404 ns |  1.21 |    0.04 | 0.0019 |      32 B |        1.33 |
