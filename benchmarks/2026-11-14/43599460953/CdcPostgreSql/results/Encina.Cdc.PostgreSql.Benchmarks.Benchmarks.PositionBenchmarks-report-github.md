```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.63GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  1.323 ns | 0.3524 ns | 0.0193 ns |  0.21 |    0.00 |      - |         - |        0.00 |
| CreatePosition   |  6.183 ns | 1.3297 ns | 0.0729 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 12.861 ns | 7.0816 ns | 0.3882 ns |  2.08 |    0.06 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 16.843 ns | 6.0255 ns | 0.3303 ns |  2.72 |    0.05 | 0.0019 |      32 B |        1.33 |
