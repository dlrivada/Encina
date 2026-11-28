```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.15GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| ComparePositions |  1.297 ns | 0.4663 ns | 0.0256 ns |  0.20 |      - |         - |        0.00 |
| CreatePosition   |  6.424 ns | 0.3496 ns | 0.0192 ns |  1.00 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 14.228 ns | 0.6848 ns | 0.0375 ns |  2.21 | 0.0014 |      24 B |        1.00 |
| ToBytes          |  6.602 ns | 0.3736 ns | 0.0205 ns |  1.03 | 0.0019 |      32 B |        1.33 |
