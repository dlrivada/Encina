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
| ComparePositions | 0.6836 ns | 0.0511 ns | 0.0028 ns |  0.10 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 7.1470 ns | 0.7616 ns | 0.0417 ns |  1.00 |    0.01 | 0.0003 |      24 B |        1.00 |
| FromBytes        | 6.7558 ns | 4.7450 ns | 0.2601 ns |  0.95 |    0.03 | 0.0003 |      24 B |        1.00 |
| ToBytes          | 6.0893 ns | 1.8541 ns | 0.1016 ns |  0.85 |    0.01 | 0.0004 |      32 B |        1.33 |
