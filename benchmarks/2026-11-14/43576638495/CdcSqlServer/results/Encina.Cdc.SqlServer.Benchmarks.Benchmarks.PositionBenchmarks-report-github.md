```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  1.280 ns | 0.0682 ns | 0.0037 ns |  0.20 |    0.00 |      - |         - |        0.00 |
| CreatePosition   |  6.561 ns | 1.0905 ns | 0.0598 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 14.231 ns | 0.7720 ns | 0.0423 ns |  2.17 |    0.02 | 0.0014 |      24 B |        1.00 |
| ToBytes          |  7.099 ns | 1.8726 ns | 0.1026 ns |  1.08 |    0.02 | 0.0019 |      32 B |        1.33 |
