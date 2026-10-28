```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.86GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.339 ns | 0.0112 ns | 0.0006 ns |  0.20 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 6.577 ns | 3.5534 ns | 0.1948 ns |  1.00 |    0.04 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 7.064 ns | 0.2496 ns | 0.0137 ns |  1.07 |    0.03 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 7.310 ns | 4.4522 ns | 0.2440 ns |  1.11 |    0.04 | 0.0019 |      32 B |        1.33 |
