```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.58GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| ComparePositions | 0.8859 ns | 1.4663 ns | 0.0804 ns |  0.13 |      - |         - |        0.00 |
| CreatePosition   | 6.9206 ns | 0.3932 ns | 0.0216 ns |  1.00 | 0.0003 |      24 B |        1.00 |
| FromBytes        | 6.1741 ns | 2.0681 ns | 0.1134 ns |  0.89 | 0.0003 |      24 B |        1.00 |
| ToBytes          | 7.9070 ns | 1.3121 ns | 0.0719 ns |  1.14 | 0.0004 |      32 B |        1.33 |
