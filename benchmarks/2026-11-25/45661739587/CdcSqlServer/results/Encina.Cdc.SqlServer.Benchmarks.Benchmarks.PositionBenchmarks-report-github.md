```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.65GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  1.279 ns | 0.2003 ns | 0.0110 ns |  0.19 |    0.00 |      - |         - |        0.00 |
| CreatePosition   |  6.706 ns | 2.4869 ns | 0.1363 ns |  1.00 |    0.03 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 15.077 ns | 8.7214 ns | 0.4781 ns |  2.25 |    0.07 | 0.0014 |      24 B |        1.00 |
| ToBytes          |  7.050 ns | 1.2915 ns | 0.0708 ns |  1.05 |    0.02 | 0.0019 |      32 B |        1.33 |
