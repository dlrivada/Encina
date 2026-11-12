```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.61GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  1.313 ns |  0.0188 ns | 0.0010 ns |  0.21 |    0.00 |      - |         - |        0.00 |
| CreatePosition   |  6.138 ns |  1.0335 ns | 0.0567 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 16.072 ns | 16.5013 ns | 0.9045 ns |  2.62 |    0.13 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 19.280 ns |  3.4532 ns | 0.1893 ns |  3.14 |    0.04 | 0.0019 |      32 B |        1.33 |
