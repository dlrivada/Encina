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
| ComparePositions |  1.313 ns |  0.3483 ns | 0.0191 ns |  0.22 |    0.00 |      - |         - |        0.00 |
| CreatePosition   |  5.883 ns |  0.1889 ns | 0.0104 ns |  1.00 |    0.00 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 12.757 ns |  1.2328 ns | 0.0676 ns |  2.17 |    0.01 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 15.766 ns | 18.5354 ns | 1.0160 ns |  2.68 |    0.15 | 0.0019 |      32 B |        1.33 |
