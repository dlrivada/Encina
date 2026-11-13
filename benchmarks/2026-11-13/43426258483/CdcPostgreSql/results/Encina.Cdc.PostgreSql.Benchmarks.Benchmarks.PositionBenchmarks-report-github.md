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
| ComparePositions |  1.312 ns |  0.0938 ns | 0.0051 ns |  0.21 |    0.01 |      - |         - |        0.00 |
| CreatePosition   |  6.181 ns |  3.7793 ns | 0.2072 ns |  1.00 |    0.04 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 16.362 ns |  3.6107 ns | 0.1979 ns |  2.65 |    0.08 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 17.435 ns | 11.1657 ns | 0.6120 ns |  2.82 |    0.12 | 0.0019 |      32 B |        1.33 |
