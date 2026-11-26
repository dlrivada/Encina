```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.9183 ns | 0.0278 ns | 0.0015 ns |  0.15 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 5.9673 ns | 0.2478 ns | 0.0136 ns |  1.00 |    0.00 | 0.0010 |      24 B |        1.00 |
| FromBytes        | 7.8436 ns | 4.6265 ns | 0.2536 ns |  1.31 |    0.04 | 0.0010 |      24 B |        1.00 |
| ToBytes          | 7.8813 ns | 0.5205 ns | 0.0285 ns |  1.32 |    0.00 | 0.0013 |      32 B |        1.33 |
