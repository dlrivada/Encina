```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  1.299 ns |  0.6008 ns | 0.0329 ns |  0.20 |    0.01 |      - |         - |        0.00 |
| CreatePosition   |  6.348 ns |  1.7381 ns | 0.0953 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 12.699 ns | 11.9836 ns | 0.6569 ns |  2.00 |    0.09 | 0.0014 |      24 B |        1.00 |
| ToBytes          |  6.802 ns |  0.7288 ns | 0.0399 ns |  1.07 |    0.01 | 0.0019 |      32 B |        1.33 |
