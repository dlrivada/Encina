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
| ComparePositions |  1.314 ns |  0.0181 ns | 0.0010 ns |  0.22 |    0.00 |      - |         - |        0.00 |
| CreatePosition   |  6.087 ns |  2.3076 ns | 0.1265 ns |  1.00 |    0.03 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 13.958 ns | 17.2123 ns | 0.9435 ns |  2.29 |    0.14 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 17.408 ns | 14.5829 ns | 0.7993 ns |  2.86 |    0.12 | 0.0019 |      32 B |        1.33 |
