```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.88GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.6909 ns | 0.1881 ns | 0.0103 ns |  0.11 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 6.3849 ns | 1.8119 ns | 0.0993 ns |  1.00 |    0.02 | 0.0003 |      24 B |        1.00 |
| FromBytes        | 6.9340 ns | 6.7505 ns | 0.3700 ns |  1.09 |    0.05 | 0.0003 |      24 B |        1.00 |
| ToBytes          | 6.7936 ns | 3.2915 ns | 0.1804 ns |  1.06 |    0.03 | 0.0004 |      32 B |        1.33 |
