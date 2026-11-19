```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.50GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.6988 ns | 0.2494 ns | 0.0137 ns |  0.10 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 6.6611 ns | 0.7153 ns | 0.0392 ns |  1.00 |    0.01 | 0.0003 |      24 B |        1.00 |
| FromBytes        | 7.2403 ns | 0.8967 ns | 0.0491 ns |  1.09 |    0.01 | 0.0003 |      24 B |        1.00 |
| ToBytes          | 6.4095 ns | 5.2426 ns | 0.2874 ns |  0.96 |    0.04 | 0.0004 |      32 B |        1.33 |
