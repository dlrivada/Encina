```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.6844 ns | 0.1224 ns | 0.0067 ns |  0.13 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 5.3133 ns | 1.8330 ns | 0.1005 ns |  1.00 |    0.02 | 0.0003 |      24 B |        1.00 |
| FromBytes        | 6.4321 ns | 2.1021 ns | 0.1152 ns |  1.21 |    0.03 | 0.0003 |      24 B |        1.00 |
| ToBytes          | 6.5577 ns | 2.0533 ns | 0.1126 ns |  1.23 |    0.03 | 0.0004 |      32 B |        1.33 |
