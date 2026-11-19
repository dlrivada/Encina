```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.84GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  1.315 ns |  0.0706 ns | 0.0039 ns |  0.22 |    0.00 |      - |         - |        0.00 |
| CreatePosition   |  6.021 ns |  1.8993 ns | 0.1041 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 15.111 ns | 13.2462 ns | 0.7261 ns |  2.51 |    0.11 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 18.162 ns |  3.3000 ns | 0.1809 ns |  3.02 |    0.05 | 0.0019 |      32 B |        1.33 |
