```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 2.81GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.320 ns | 0.0728 ns | 0.0040 ns |  0.19 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 7.118 ns | 3.3524 ns | 0.1838 ns |  1.00 |    0.03 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 8.331 ns | 3.1449 ns | 0.1724 ns |  1.17 |    0.03 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 7.020 ns | 2.9402 ns | 0.1612 ns |  0.99 |    0.03 | 0.0019 |      32 B |        1.33 |
