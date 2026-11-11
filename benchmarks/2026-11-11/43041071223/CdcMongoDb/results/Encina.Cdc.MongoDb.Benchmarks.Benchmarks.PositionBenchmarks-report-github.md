```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error      | StdDev     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|-----------:|-----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 138.195 ns |   4.162 ns |  0.2282 ns | 15.62 |    0.35 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   8.850 ns |   4.121 ns |  0.2259 ns |  1.00 |    0.03 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 600.062 ns |  62.795 ns |  3.4420 ns | 67.83 |    1.56 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 568.771 ns | 297.543 ns | 16.3093 ns | 64.29 |    2.15 | 0.0601 |    1008 B |       42.00 |
