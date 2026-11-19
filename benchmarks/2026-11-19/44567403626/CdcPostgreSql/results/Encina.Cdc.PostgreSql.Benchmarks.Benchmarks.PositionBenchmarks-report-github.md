```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.23GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  1.297 ns | 0.3416 ns | 0.0187 ns |  0.22 |    0.00 |      - |         - |        0.00 |
| CreatePosition   |  6.027 ns | 1.2819 ns | 0.0703 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 15.243 ns | 4.8059 ns | 0.2634 ns |  2.53 |    0.05 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 16.775 ns | 6.4373 ns | 0.3528 ns |  2.78 |    0.06 | 0.0019 |      32 B |        1.33 |
