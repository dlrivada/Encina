```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  1.285 ns |  0.3362 ns | 0.0184 ns |  0.19 |    0.00 |      - |         - |        0.00 |
| CreatePosition   |  6.713 ns |  0.9742 ns | 0.0534 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 15.481 ns | 11.1584 ns | 0.6116 ns |  2.31 |    0.08 | 0.0014 |      24 B |        1.00 |
| ToBytes          |  7.199 ns |  1.4410 ns | 0.0790 ns |  1.07 |    0.01 | 0.0019 |      32 B |        1.33 |
