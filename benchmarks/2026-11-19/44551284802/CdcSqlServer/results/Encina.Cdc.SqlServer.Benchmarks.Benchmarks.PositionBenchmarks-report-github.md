```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  1.272 ns |  0.0346 ns | 0.0019 ns |  0.20 |    0.00 |      - |         - |        0.00 |
| CreatePosition   |  6.342 ns |  0.6513 ns | 0.0357 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 15.471 ns | 15.9453 ns | 0.8740 ns |  2.44 |    0.12 | 0.0014 |      24 B |        1.00 |
| ToBytes          |  7.466 ns |  2.8948 ns | 0.1587 ns |  1.18 |    0.02 | 0.0019 |      32 B |        1.33 |
