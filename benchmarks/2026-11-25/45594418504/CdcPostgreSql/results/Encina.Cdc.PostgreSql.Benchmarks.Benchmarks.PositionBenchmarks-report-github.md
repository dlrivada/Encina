```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.67GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  1.325 ns |  0.1225 ns | 0.0067 ns |  0.21 |    0.00 |      - |         - |        0.00 |
| CreatePosition   |  6.275 ns |  0.6851 ns | 0.0376 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 17.416 ns | 13.3730 ns | 0.7330 ns |  2.78 |    0.10 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 19.092 ns |  7.3162 ns | 0.4010 ns |  3.04 |    0.06 | 0.0019 |      32 B |        1.33 |
