```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.20GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.9839 ns | 0.4540 ns | 0.0249 ns |  0.13 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 7.5116 ns | 3.3217 ns | 0.1821 ns |  1.00 |    0.03 | 0.0003 |      24 B |        1.00 |
| FromBytes        | 8.3646 ns | 0.7795 ns | 0.0427 ns |  1.11 |    0.02 | 0.0003 |      24 B |        1.00 |
| ToBytes          | 8.1070 ns | 3.2299 ns | 0.1770 ns |  1.08 |    0.03 | 0.0004 |      32 B |        1.33 |
