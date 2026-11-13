```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.65GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.024 ns | 0.0344 ns | 0.0019 ns |  0.17 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 6.142 ns | 4.3553 ns | 0.2387 ns |  1.00 |    0.05 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 6.340 ns | 8.6447 ns | 0.4738 ns |  1.03 |    0.08 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 6.092 ns | 6.7152 ns | 0.3681 ns |  0.99 |    0.06 | 0.0019 |      32 B |        1.33 |
