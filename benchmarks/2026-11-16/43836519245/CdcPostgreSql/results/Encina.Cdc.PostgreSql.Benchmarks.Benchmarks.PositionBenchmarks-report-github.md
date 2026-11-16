```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 3.26GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.9267 ns | 0.2624 ns | 0.0144 ns |  0.15 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 6.3665 ns | 1.8465 ns | 0.1012 ns |  1.00 |    0.02 | 0.0010 |      24 B |        1.00 |
| FromBytes        | 7.0662 ns | 0.5675 ns | 0.0311 ns |  1.11 |    0.02 | 0.0010 |      24 B |        1.00 |
| ToBytes          | 9.2529 ns | 1.8500 ns | 0.1014 ns |  1.45 |    0.02 | 0.0013 |      32 B |        1.33 |
