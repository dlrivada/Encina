```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.16GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.4588 ns | 0.2653 ns | 0.0145 ns |  0.13 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 3.5039 ns | 0.9920 ns | 0.0544 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 3.6958 ns | 0.9157 ns | 0.0502 ns |  1.05 |    0.02 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 4.0669 ns | 0.9122 ns | 0.0500 ns |  1.16 |    0.02 | 0.0019 |      32 B |        1.33 |
