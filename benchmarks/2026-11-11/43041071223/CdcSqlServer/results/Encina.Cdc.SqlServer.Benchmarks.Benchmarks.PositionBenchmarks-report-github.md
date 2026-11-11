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
| ComparePositions |  1.302 ns | 0.4764 ns | 0.0261 ns |  0.21 |    0.00 |      - |         - |        0.00 |
| CreatePosition   |  6.127 ns | 1.6781 ns | 0.0920 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 13.269 ns | 2.9632 ns | 0.1624 ns |  2.17 |    0.04 | 0.0014 |      24 B |        1.00 |
| ToBytes          |  6.361 ns | 0.3800 ns | 0.0208 ns |  1.04 |    0.01 | 0.0019 |      32 B |        1.33 |
