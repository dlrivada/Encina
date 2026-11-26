```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  1.280 ns |  0.0258 ns | 0.0014 ns |  0.19 |    0.00 |      - |         - |        0.00 |
| CreatePosition   |  6.719 ns |  2.3032 ns | 0.1262 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 15.327 ns | 10.0214 ns | 0.5493 ns |  2.28 |    0.08 | 0.0014 |      24 B |        1.00 |
| ToBytes          |  7.037 ns |  4.8545 ns | 0.2661 ns |  1.05 |    0.04 | 0.0019 |      32 B |        1.33 |
