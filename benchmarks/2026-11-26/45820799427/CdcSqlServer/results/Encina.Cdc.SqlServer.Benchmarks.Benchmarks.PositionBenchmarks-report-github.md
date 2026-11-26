```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.11GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  1.283 ns | 0.0568 ns | 0.0031 ns |  0.18 |    0.00 |      - |         - |        0.00 |
| CreatePosition   |  6.938 ns | 0.1407 ns | 0.0077 ns |  1.00 |    0.00 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 12.649 ns | 3.2860 ns | 0.1801 ns |  1.82 |    0.02 | 0.0014 |      24 B |        1.00 |
| ToBytes          |  7.511 ns | 1.0866 ns | 0.0596 ns |  1.08 |    0.01 | 0.0019 |      32 B |        1.33 |
