```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  1.274 ns | 0.0457 ns | 0.0025 ns |  0.19 |    0.00 |      - |         - |        0.00 |
| CreatePosition   |  6.709 ns | 1.9345 ns | 0.1060 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 15.770 ns | 7.9038 ns | 0.4332 ns |  2.35 |    0.06 | 0.0014 |      24 B |        1.00 |
| ToBytes          |  6.932 ns | 1.9674 ns | 0.1078 ns |  1.03 |    0.02 | 0.0019 |      32 B |        1.33 |
