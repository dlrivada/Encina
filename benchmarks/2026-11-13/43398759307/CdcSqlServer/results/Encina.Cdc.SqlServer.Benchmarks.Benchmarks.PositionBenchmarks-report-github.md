```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.62GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  1.284 ns |  0.2147 ns | 0.0118 ns |  0.21 |    0.00 |      - |         - |        0.00 |
| CreatePosition   |  6.246 ns |  1.9345 ns | 0.1060 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 12.301 ns | 12.5031 ns | 0.6853 ns |  1.97 |    0.10 | 0.0014 |      24 B |        1.00 |
| ToBytes          |  6.601 ns |  1.4658 ns | 0.0803 ns |  1.06 |    0.02 | 0.0019 |      32 B |        1.33 |
