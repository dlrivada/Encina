```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.78GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.9672 ns | 0.2601 ns | 0.0143 ns |  0.14 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 6.9519 ns | 2.8773 ns | 0.1577 ns |  1.00 |    0.03 | 0.0010 |      24 B |        1.00 |
| FromBytes        | 7.6332 ns | 0.2318 ns | 0.0127 ns |  1.10 |    0.02 | 0.0010 |      24 B |        1.00 |
| ToBytes          | 7.5347 ns | 2.0830 ns | 0.1142 ns |  1.08 |    0.03 | 0.0013 |      32 B |        1.33 |
