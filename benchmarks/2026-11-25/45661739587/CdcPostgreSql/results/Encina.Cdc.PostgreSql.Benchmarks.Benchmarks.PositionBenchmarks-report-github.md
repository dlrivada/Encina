```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  1.314 ns | 0.0775 ns | 0.0042 ns |  0.20 |    0.00 |      - |         - |        0.00 |
| CreatePosition   |  6.441 ns | 2.0106 ns | 0.1102 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 16.101 ns | 6.0597 ns | 0.3322 ns |  2.50 |    0.06 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 17.034 ns | 2.5454 ns | 0.1395 ns |  2.65 |    0.04 | 0.0019 |      32 B |        1.33 |
